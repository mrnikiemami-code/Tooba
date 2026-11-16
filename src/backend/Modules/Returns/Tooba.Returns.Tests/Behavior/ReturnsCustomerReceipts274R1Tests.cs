using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Contracts.Returns;
using Tooba.Order.Contracts.Returns;
using Tooba.Payment.Contracts.Returns;
using Tooba.Returns.Application.ReturnRequests.Commands;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Application.ReturnRequests.Ports;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Contracts.Operations;
using Tooba.Returns.Domain.Aggregates;
using Tooba.Returns.Domain.ValueObjects;
using Tooba.Returns.Endpoints.Admin;
using Tooba.Returns.Infrastructure.Directories;
using Tooba.Returns.Infrastructure.Evaluators;
using Tooba.Returns.Infrastructure.Observability;
using Tooba.Returns.Infrastructure.Persistence;
using Tooba.Wallet.Contracts.Dtos;
using Tooba.Wallet.Contracts.Refunds;
using Xunit;
using ReturnSnapshot = Tooba.Returns.Application.ReturnRequests.Models.ReturnSnapshot;

namespace Tooba.Returns.Tests.Behavior;

/// <summary>
/// TB-274-R1 — the bounded repair of 274's explicit-inspection, truthful-stock, fail-closed-evidence and scoped
/// decision-collision requirements.
/// <para>
/// The facts here are exactly the ones the Architect named as missing: inspection is explicit at every reachable
/// shape; a malformed/borrowed stored receipt row is never read as coverage and never accepts more rows; owning seller
/// scope is enforced INSIDE the owning directory before a decision; an inspected usable quantity without an owning
/// stock binding is a typed pending blocker instead of a claimed completion; and only the decision table's own primary
/// key arbitrates a collision, with scoped tracking recovery that keeps the caller's work.
/// </para>
/// </summary>
public sealed class ReturnsCustomerReceipts274R1Tests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-16T10:00:00Z");

    private static readonly Guid SellerPartyId = Guid.Parse("01a03826-97c5-7000-ad15-c9d141b1f32e");

    private static readonly Guid OtherSellerPartyId = Guid.Parse("01a03827-1111-7000-ad15-c9d141b1f32f");

    // ------------------------------------------------------------------ A: inspection is explicit

    [Fact]
    public void A_inspection_has_no_optional_default_on_any_reachable_shape()
    {
        // A record parameter with a default value is OPTIONAL to the compiler, which is exactly the omission 274-R1
        // forbids: a caller could leave the inspection out and still construct the line.
        foreach (var type in new[] { typeof(CustomerReceiptLine), typeof(CustomerReceiptLineCommand), typeof(AdminReturnReceiptLine) })
        {
            var condition = Assert.Single(
                type.GetConstructors().SelectMany(x => x.GetParameters()),
                x => string.Equals(x.Name, "Condition", StringComparison.Ordinal));

            Assert.False(condition.IsOptional, $"{type.Name}.Condition must be required");
            Assert.False(condition.HasDefaultValue, $"{type.Name}.Condition must not carry a default");
        }
    }

    // ------------------------------------------------------------------ B: fail-closed stored evidence

    [Fact]
    public void B_malformed_stored_rows_never_become_coverage_or_usable_quantity()
    {
        var item = Guid.NewGuid();

        // A zero/negative stored quantity, and an inspection value outside the two defined ones, are contradictions.
        foreach (var quantity in new[] { 0m, -1m })
        {
            var summary = ReturnReceivingSummary.Describe([(item, 2m)], [(item, quantity, ReturnItemCondition.Usable)]);
            Assert.False(summary.IsCoherent);
            Assert.False(summary.IsComplete);
            Assert.Equal(0m, summary.Usable);
            Assert.Equal(0m, Assert.Single(summary.Lines).Received);
        }

        var undefined = ReturnReceivingSummary.Describe([(item, 2m)], [(item, 2m, (ReturnItemCondition)7)]);
        Assert.False(undefined.IsCoherent);
        Assert.False(undefined.IsComplete);
        Assert.Equal(0m, undefined.Usable);
        Assert.Equal(0m, undefined.Damaged);

        // The same row read through a non-representable quantity is refused too, never rounded into coverage.
        var imprecise = ReturnReceivingSummary.Describe([(item, 2m)], [(item, 1.0000001m, ReturnItemCondition.Usable)]);
        Assert.False(imprecise.IsCoherent);
        Assert.Equal(0m, imprecise.Usable);
    }

    [Fact]
    public void B_a_line_answering_for_another_cases_receipt_header_is_never_borrowed()
    {
        var item = Guid.NewGuid();
        var ownReceipt = Guid.NewGuid();
        var foreignReceipt = Guid.NewGuid();

        var summary = ReturnReceivingSummary.Describe(
            [(item, 2m)],
            [
                (ownReceipt, item, 1m, ReturnItemCondition.Usable),
                (foreignReceipt, item, 1m, ReturnItemCondition.Usable),
            ],
            [ownReceipt]);

        // The borrowed row is a contradiction: it never counts, and the case can never read as complete.
        Assert.False(summary.IsCoherent);
        Assert.False(summary.IsComplete);
        Assert.Equal(1m, Assert.Single(summary.Lines).Received);
    }

    [Fact]
    public async Task B_a_new_occurrence_is_refused_when_the_stored_evidence_already_contradicts_itself()
    {
        await using var order = await SeedAsync(requested: 2m);
        await order.RecordAsync("delivery-1", (order.ReturnItemId, 1m, ReturnItemCondition.Usable));

        // A contradictory stored row (more units than the customer was asked to return) that only the store could
        // hold: the write boundary itself would never produce it.
        var storedLine = await order.Db.ReturnReceiptLines.AsTracking().SingleAsync();
        order.Db.Entry(storedLine).Property(x => x.Quantity).CurrentValue = 5m;
        await order.Db.SaveChangesAsync();
        order.ResetRequestScope();

        Assert.False((await order.Snapshot())!.Receiving!.IsCoherent);

        // Adding MORE lines can never "repair" the disagreement, so a new occurrence is refused with zero new rows.
        var refused = await Assert.ThrowsAsync<ContractOperationException>(() => order.RecordAsync(
            "delivery-2", (order.ReturnItemId, 1m, ReturnItemCondition.Damaged)));
        Assert.Equal(ReturnsErrorCodes.RecoveryEvidenceConflict, refused.Code);
        Assert.Equal(1, await order.ReceiptCount());
        Assert.Equal(1, await order.ReceiptLineCount());
    }

    [Fact]
    public async Task B_the_directory_refuses_a_receipt_with_no_authenticated_actor()
    {
        await using var order = await SeedAsync(requested: 1m);

        var refused = await Assert.ThrowsAsync<ContractOperationException>(() => order.RecordAsAsync(
            actorUserId: Guid.Empty,
            "delivery-1",
            (order.ReturnItemId, 1m, ReturnItemCondition.Usable)));

        Assert.Equal(ReturnsErrorCodes.ReceiptActorRequired, refused.Code);
        Assert.Equal(0, await order.ReceiptCount());
        Assert.Equal(0, await order.ReceiptLineCount());
    }

    [Fact]
    public async Task B_a_foreign_seller_scope_can_neither_approve_nor_reject_another_case()
    {
        await using var order = await SeedAsync(requested: 1m);
        await order.RecordAsync("delivery-1", (order.ReturnItemId, 1m, ReturnItemCondition.Usable));

        var approve = await Assert.ThrowsAsync<ContractOperationException>(() =>
            order.ApproveAsAsync(OtherSellerPartyId));
        Assert.Equal(ReturnsErrorCodes.Missing, approve.Code);

        var reject = await Assert.ThrowsAsync<ContractOperationException>(() =>
            order.RejectAsAsync(OtherSellerPartyId, "دلیل بیرونی"));
        Assert.Equal(ReturnsErrorCodes.Missing, reject.Code);

        // Zero decision, zero attempt, zero money: the refusal runs BEFORE the durable decision is recorded.
        Assert.Equal(0, await order.DecisionCount());
        Assert.Equal(0, await order.AttemptCount());
        Assert.Empty(order.Gateway.Calls);
        Assert.Equal(ReturnRequestStatus.Requested, (await order.Snapshot())!.Status);

        // The OWNING scope is still fully authorized on the same case.
        var rejected = await order.RejectAsAsync(SellerPartyId, "دلیل درست");
        Assert.Equal(ReturnRequestStatus.Rejected, rejected.Status);
        Assert.Equal(1, await order.DecisionCount());
        Assert.Empty(order.Gateway.Calls);
    }

    // ------------------------------------------------------------------ C: never claim stock completion without Inventory

    [Fact]
    public async Task C_inspected_usable_without_an_owning_stock_binding_is_a_typed_pending_blocker()
    {
        // A delivered-customer case whose line carries NO inventory reservation at all.
        await using var order = await SeedAsync(requested: 2m, reservationId: null);
        await order.RecordAsync("delivery-1", (order.ReturnItemId, 2m, ReturnItemCondition.Usable));

        var approved = await order.ApproveAsync();
        Assert.Equal(ReturnRequestStatus.Completed, approved.Status);

        // The money really was confirmed, and NOTHING was restocked or declared saleable without Inventory.
        Assert.Single(order.Gateway.Calls);
        Assert.Empty(order.Inventory.Restocked);
        Assert.Equal(ReturnItemDisposition.None, (await order.Items()).Single().Disposition);
        Assert.Equal(1, await order.AttemptCount());

        // The READ reports the honest blocker, with no executable kind and the money preserved.
        var read = (await order.Snapshot())!.Recovery!;
        Assert.Equal(ReturnRecoveryPhase.Blocked, read.Phase);
        Assert.Equal(ReturnsErrorCodes.RecoveryStockBindingRequired, read.BlockerCode);
        Assert.Null(read.RecoveryKind);
        Assert.True(read.FinancialComplete);
        Assert.False(read.PhysicalComplete);

        // The ACTION refuses with the SAME typed reason: no Inventory call, no new attempt, no invented completion.
        var refused = await Assert.ThrowsAsync<ContractOperationException>(
            () => order.RecoverAsync(ReturnRecoveryKind.SettlePhysicalDisposition));
        Assert.Equal(ReturnsErrorCodes.RecoveryStockBindingRequired, refused.Code);
        Assert.Single(order.Gateway.Calls);
        Assert.Empty(order.Inventory.Restocked);
        Assert.Equal(1, await order.AttemptCount());
        Assert.Equal(ReturnItemDisposition.None, (await order.Items()).Single().Disposition);
    }

    [Fact]
    public async Task C_a_damaged_only_receipt_without_a_binding_completes_without_any_inventory_call()
    {
        // A fully damaged line needs no stock binding: nothing saleable may ever be restocked, so its physical phase
        // is a real recorded non-saleable disposition rather than a pending stock blocker.
        await using var order = await SeedAsync(requested: 1m, reservationId: null);
        await order.RecordAsync("delivery-1", (order.ReturnItemId, 1m, ReturnItemCondition.Damaged));

        await order.ApproveAsync();

        Assert.Empty(order.Inventory.Restocked);
        Assert.Equal(ReturnItemDisposition.DamagedNonSaleable, (await order.Items()).Single().Disposition);
        Assert.Equal(ReturnRecoveryPhase.Settled, (await order.Snapshot())!.Recovery!.Phase);
    }

    [Fact]
    public async Task C_a_mixed_line_restocks_only_the_inspected_usable_portion()
    {
        // ONE requested line inspected as a mixed portion: 1 usable + 1 damaged of 2 requested units.
        await using var order = await SeedAsync(requested: 2m, reservationId: Guid.NewGuid());
        await order.RecordAsync(
            "delivery-1",
            (order.ReturnItemId, 1m, ReturnItemCondition.Usable),
            (order.ReturnItemId, 1m, ReturnItemCondition.Damaged));

        await order.ApproveAsync();

        // ONLY the inspected usable portion may reach saleable Inventory; the damaged portion never does.
        var restock = Assert.Single(order.Inventory.Restocked);
        Assert.Equal(1m, restock.Quantity);

        // The read states the EXACT restocked portion, so neither the requested quantity nor the line's single
        // condition/disposition field can announce that the whole requested amount went back to saleable stock.
        var line = Assert.Single((await order.Snapshot())!.Items);
        Assert.Equal(ReturnItemDisposition.SaleableRestock, line.Disposition);
        Assert.Equal(1m, line.SaleableRestockedQuantity);
        Assert.Equal(2m, line.Quantity);
    }

    [Fact]
    public async Task C_a_rejected_case_that_received_goods_reports_the_remaining_physical_resolution()
    {
        await using var order = await SeedAsync(requested: 1m, reservationId: Guid.NewGuid());
        await order.RecordAsync("delivery-1", (order.ReturnItemId, 1m, ReturnItemCondition.Usable));

        var rejected = await order.RejectAsAsync(SellerPartyId, "مشتری انصراف داد");
        Assert.Equal(ReturnRequestStatus.Rejected, rejected.Status);

        // The recorded receipt history stays visible...
        Assert.True(rejected.Receiving!.IsComplete);
        Assert.Equal(1m, rejected.Receiving.Usable);
        Assert.Equal(1, await order.ReceiptCount());

        // ...and the remaining physical work is an honest, non-executable blocker: nothing is restocked, refunded or
        // shipped back automatically.
        var read = rejected.Recovery!;
        Assert.Equal(ReturnRecoveryPhase.Blocked, read.Phase);
        Assert.Equal(ReturnsErrorCodes.RecoveryPhysicalResolutionRequired, read.BlockerCode);
        Assert.Null(read.RecoveryKind);
        Assert.False(read.FinancialComplete);
        Assert.Empty(order.Inventory.Restocked);
        Assert.Empty(order.Gateway.Calls);
        Assert.Equal(0, await order.AttemptCount());
        Assert.Equal(ReturnItemDisposition.None, (await order.Items()).Single().Disposition);
    }

    [Fact]
    public async Task C_a_still_requested_case_with_a_partial_receipt_is_not_reported_as_blocked()
    {
        // The honest remaining-work blocker belongs to a case that was DECIDED AWAY. A still-open case that has only
        // received part of its parcel is normal in-progress work, not an owed physical resolution.
        await using var order = await SeedAsync(requested: 2m, reservationId: Guid.NewGuid());
        await order.RecordAsync("delivery-1", (order.ReturnItemId, 1m, ReturnItemCondition.Usable));

        var read = (await order.Snapshot())!;
        Assert.Equal(ReturnRequestStatus.Requested, read.Status);
        Assert.True(read.Receiving!.IsCoherent);
        Assert.False(read.Receiving.IsComplete);
        Assert.Equal(ReturnRecoveryPhase.None, read.Recovery!.Phase);
        Assert.Null(read.Recovery.BlockerCode);
        Assert.Null(read.Recovery.RecoveryKind);
    }

    // ------------------------------------------------------------------ D: scoped decision-collision recovery

    [Fact]
    public async Task D_the_owning_decision_primary_key_arbitrates_the_collision_and_keeps_unrelated_tracked_work()
    {
        await using var order = await SeedAsync(requested: 1m);

        // The winning decision of the concurrent writer, written to the same store only when our insert is refused.
        order.Collision.ArmWinner(new ReturnDecisionRecord
        {
            ReturnRequestId = order.ReturnRequestId,
            Decision = ReturnRecoveryPolicy.RejectedDecision,
            Reason = "دلیل برنده",
            DecidedByUserId = Guid.NewGuid(),
            DecidedAt = Now,
        });

        // An unrelated caller-owned tracked change that must survive the refusal untouched.
        var trackedItem = await order.Db.ReturnItems.AsTracking().SingleAsync();
        order.Db.Entry(trackedItem).Property(x => x.Currency).CurrentValue = "USD";
        Assert.Equal(EntityState.Modified, order.Db.Entry(trackedItem).State);

        var winner = await order.RejectAsAsync(SellerPartyId, "دلیل برنده");

        // The stored winner answers the exact same intent: no rewrite, no second row, no effect.
        Assert.Equal(ReturnRequestStatus.Rejected, winner.Status);
        Assert.Equal("دلیل برنده", winner.DecisionReason);
        Assert.Equal(1, await order.DecisionCount());
        Assert.Empty(order.Gateway.Calls);
        Assert.Equal(0, await order.AttemptCount());

        // The caller's own tracked entity kept its INSTANCE and VALUE — no global tracker clear happened. (The case
        // write that follows the decision legitimately persists it, so its state settles to Unchanged afterwards;
        // the durable value below is the stronger fact.)
        Assert.Same(trackedItem, order.Db.Entry(trackedItem).Entity);
        Assert.Equal("USD", trackedItem.Currency);
        Assert.Equal("USD", Assert.Single(await order.Items()).Currency);

        // The failed attempt's own addition was detached, so a later real save cannot replay it.
        Assert.DoesNotContain(
            order.Db.ChangeTracker.Entries<ReturnDecisionRecord>(),
            x => x.State == EntityState.Added);
    }

    [Fact]
    public async Task D_a_winner_with_a_different_reason_is_a_typed_conflict_with_no_mutation()
    {
        await using var order = await SeedAsync(requested: 1m);

        order.Collision.ArmWinner(new ReturnDecisionRecord
        {
            ReturnRequestId = order.ReturnRequestId,
            Decision = ReturnRecoveryPolicy.RejectedDecision,
            Reason = "دلیل برنده",
            DecidedByUserId = Guid.NewGuid(),
            DecidedAt = Now,
        });

        var conflict = await Assert.ThrowsAsync<ContractOperationException>(
            () => order.RejectAsAsync(SellerPartyId, "دلیل دیگر"));

        Assert.Equal(ReturnsErrorCodes.DecisionReasonConflict, conflict.Code);
        Assert.Equal("دلیل برنده", Assert.Single(await order.Decisions()).Reason);
        Assert.Equal(1, await order.DecisionCount());
        Assert.Empty(order.Gateway.Calls);
    }

    [Fact]
    public async Task D_a_failure_on_another_constraint_is_never_read_as_a_decision_collision()
    {
        // A lookalike unique violation on ANOTHER constraint, the right constraint name on the wrong TABLE, and the
        // right table/constraint in the wrong SCHEMA. Each case runs on its own context because a refused insert that
        // is NOT our primary key leaves the provider failure to the caller, exactly as production does.
        var faults = new (string Constraint, string Table, string Schema)[]
        {
            ("ix_return_requests_idempotency_key", "return_requests", ReturnsDbContext.Schema),
            ("pk_return_request_decisions", "return_requests", ReturnsDbContext.Schema),
            ("pk_return_request_decisions", "return_request_decisions", "public"),
        };

        foreach (var fault in faults)
        {
            await using var order = await SeedAsync(requested: 1m);
            order.Collision.ArmFault(fault.Constraint, fault.Table, fault.Schema);

            var propagated = await Assert.ThrowsAsync<DbUpdateException>(
                () => order.RejectAsAsync(SellerPartyId, "دلیل"));
            var postgres = Assert.IsType<PostgresException>(propagated.InnerException);
            Assert.Equal("23505", postgres.SqlState);
            Assert.Equal(0, await order.DecisionCount());

            // No refund, no attempt, no claim release and no case transition rode on the misinterpretation.
            Assert.Empty(order.Gateway.Calls);
            Assert.Equal(0, await order.AttemptCount());
            var stored = await order.Snapshot();
            Assert.NotNull(stored);
            Assert.Equal(ReturnRequestStatus.Requested, stored!.Status);
            Assert.Null(stored.DecisionReason);
        }
    }

    // ------------------------------------------------------------------ fixtures

    private sealed class Harness : IAsyncDisposable
    {
        private readonly ReturnsDbContext _db;
        private readonly ReturnDirectory _directory;

        public Harness(
            ReturnsDbContext db,
            ReturnDirectory directory,
            Guid returnRequestId,
            Guid returnItemId,
            ScriptedGateway gateway,
            ScriptedWallet wallet,
            ScriptedInventory inventory,
            DecisionCollisionInterceptor collision)
        {
            _db = db;
            _directory = directory;
            ReturnRequestId = returnRequestId;
            ReturnItemId = returnItemId;
            Gateway = gateway;
            Wallet = wallet;
            Inventory = inventory;
            Collision = collision;
        }

        public ReturnsDbContext Db => _db;

        public Guid ReturnRequestId { get; }

        public Guid ReturnItemId { get; }

        public ScriptedGateway Gateway { get; }

        public ScriptedWallet Wallet { get; }

        public ScriptedInventory Inventory { get; }

        public DecisionCollisionInterceptor Collision { get; }

        public void ResetRequestScope() => _db.ChangeTracker.Clear();

        public Task<ReturnSnapshot> RecordAsync(
            string occurrenceKey,
            params (Guid ReturnItemId, decimal Quantity, ReturnItemCondition Condition)[] lines) =>
            RecordAsAsync(Guid.NewGuid(), occurrenceKey, lines);

        public Task<ReturnSnapshot> RecordAsAsync(
            Guid actorUserId,
            string occurrenceKey,
            params (Guid ReturnItemId, decimal Quantity, ReturnItemCondition Condition)[] lines) =>
            _directory.RecordReceiptAsync(
                new RecordReturnReceiptCommand(
                    ReturnRequestId,
                    actorUserId,
                    occurrenceKey,
                    lines.Select(x => new CustomerReceiptLine(x.ReturnItemId, x.Quantity, x.Condition)).ToList()),
                CancellationToken.None);

        public Task<ReturnSnapshot> ApproveAsync() => ApproveAsAsync(SellerPartyId);

        public Task<ReturnSnapshot> ApproveAsAsync(Guid sellerPartyId) =>
            _directory.ApproveAsync(
                new ApproveReturnCommand(ReturnRequestId, Guid.NewGuid(), sellerPartyId),
                CancellationToken.None);

        public Task<ReturnSnapshot> RejectAsAsync(Guid sellerPartyId, string? reason) =>
            _directory.RejectAsync(
                new RejectReturnCommand(ReturnRequestId, Guid.NewGuid(), sellerPartyId, reason),
                CancellationToken.None);

        public Task<ReturnSnapshot> RecoverAsync(ReturnRecoveryKind kind, Guid? expectedAttemptId = null) =>
            _directory.RecoverAsync(
                new RecoverReturnCommand(ReturnRequestId, Guid.NewGuid(), kind, expectedAttemptId),
                CancellationToken.None);

        public Task<ReturnSnapshot?> Snapshot() => _directory.GetAsync(ReturnRequestId, CancellationToken.None);

        public Task<int> AttemptCount() =>
            _db.RefundAttempts.CountAsync(x => x.ReturnRequestId == ReturnRequestId);

        public Task<int> DecisionCount() =>
            _db.ReturnDecisions.CountAsync(x => x.ReturnRequestId == ReturnRequestId);

        public Task<int> ReceiptCount() =>
            _db.ReturnReceipts.CountAsync(x => x.ReturnRequestId == ReturnRequestId);

        public Task<int> ReceiptLineCount() =>
            _db.ReturnReceiptLines.CountAsync(x => x.ReturnRequestId == ReturnRequestId);

        public async Task<IReadOnlyList<ReturnDecisionRecord>> Decisions() =>
            await _db.ReturnDecisions.AsNoTracking()
                .Where(x => x.ReturnRequestId == ReturnRequestId)
                .ToListAsync();

        public async Task<IReadOnlyList<ReturnItem>> Items() =>
            await _db.ReturnItems.AsNoTracking()
                .Where(x => x.ReturnRequestId == ReturnRequestId)
                .ToListAsync();

        public ValueTask DisposeAsync() => _db.DisposeAsync();
    }

    private static async Task<Harness> SeedAsync(decimal requested, Guid? reservationId = null)
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var collision = new DecisionCollisionInterceptor(databaseName);
        var db = CreateDb(databaseName, collision);
        var clock = new MutableClock(Now);
        var customer = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var returnRequestId = Guid.NewGuid();

        var request = ReturnRequest.Create(
            returnRequestId,
            () => Guid.NewGuid(),
            orderId,
            Guid.NewGuid(),
            SellerPartyId,
            customer,
            $"tb274r1-{returnRequestId:N}",
            "دلیل مرجوعی",
            "IRR",
            [(lineId, requested, 1000m, reservationId)],
            Now,
            RefundDestination.OriginalPayment);
        db.ReturnRequests.Add(request);
        db.ReturnItems.AddRange(request.Items);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var gateway = new ScriptedGateway();
        var wallet = new ScriptedWallet();
        var inventory = new ScriptedInventory();
        var order = BuildOrder(orderId, customer, lineId, requested, 1000m, reservationId);
        var orderReader = new FixedOrderReader(order);
        var directory = new ReturnDirectory(
            db,
            new OpenReturnUseCaseGuard(),
            orderReader,
            new ReturnEligibilityEvaluator(
                orderReader,
                new FixedFulfillmentReader(order, lineId),
                db,
                clock),
            new FixedPaymentReader(requested * 1000m),
            gateway,
            wallet,
            inventory,
            new ReturnsInstrumentation(),
            NullLogger<ReturnDirectory>.Instance,
            clock,
            new UuidV7IdGenerator());

        return new Harness(
            db,
            directory,
            returnRequestId,
            request.Items.Single().ReturnItemId,
            gateway,
            wallet,
            inventory,
            collision);
    }

    private static OrderReturnContextSnapshot BuildOrder(
        Guid sellerOrderId,
        Guid customerUserId,
        Guid lineId,
        decimal quantity,
        decimal unitPrice,
        Guid? reservationId) =>
        new(
            sellerOrderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            customerUserId,
            true,
            "IRR",
            [new OrderReturnLineSnapshot(lineId, quantity, unitPrice, "IRR", reservationId, true, 7)]);

    private static ReturnsDbContext CreateDb(string databaseName, DecisionCollisionInterceptor collision)
    {
        var options = new DbContextOptionsBuilder<ReturnsDbContext>()
            .UseInMemoryDatabase(databaseName)
            .AddInterceptors(collision)
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ReturnsDbContext(options);
    }

    /// <summary>
    /// Controlled, clearly-labelled fault injection for the decision collision branch. No real race is simulated:
    /// the interceptor reproduces the EXACT provider failure shape (<c>23505</c> on a named constraint of a named
    /// table/schema) and, when armed with a winner, persists that winner through a second context on the same
    /// in-memory store so the owning re-read has a real row to answer.
    /// </summary>
    private sealed class DecisionCollisionInterceptor(string databaseName) : SaveChangesInterceptor
    {
        private ReturnDecisionRecord? _winner;
        private string? _constraint;
        private string _table = "return_request_decisions";
        private string _schema = ReturnsDbContext.Schema;

        public void ArmWinner(ReturnDecisionRecord winner)
        {
            _winner = winner;
            _constraint = "pk_return_request_decisions";
            _table = "return_request_decisions";
            _schema = ReturnsDbContext.Schema;
        }

        public void ArmFault(string constraint, string table, string schema = ReturnsDbContext.Schema)
        {
            _winner = null;
            _constraint = constraint;
            _table = table;
            _schema = schema;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var context = eventData.Context;
            if (context is null
                || _constraint is null
                || !context.ChangeTracker.Entries<ReturnDecisionRecord>().Any(x => x.State == EntityState.Added))
            {
                return result;
            }

            var constraint = _constraint;
            var table = _table;
            var schema = _schema;
            var winner = _winner;
            _winner = null;
            _constraint = null;

            if (winner is not null)
            {
                var options = new DbContextOptionsBuilder<ReturnsDbContext>()
                    .UseInMemoryDatabase(databaseName)
                    .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                    .Options;
                await using var other = new ReturnsDbContext(options);
                other.ReturnDecisions.Add(new ReturnDecisionRecord
                {
                    ReturnRequestId = winner.ReturnRequestId,
                    Decision = winner.Decision,
                    Reason = winner.Reason,
                    DecidedByUserId = winner.DecidedByUserId,
                    DecidedAt = winner.DecidedAt,
                });
                await other.SaveChangesAsync(cancellationToken);
            }

            throw new DbUpdateException(
                "tb274r1: injected decision insert failure",
                new PostgresException(
                    messageText: "duplicate key value violates unique constraint",
                    severity: "ERROR",
                    invariantSeverity: "ERROR",
                    sqlState: "23505",
                    detail: "Key (return_request_id) already exists.",
                    schemaName: schema,
                    tableName: table,
                    constraintName: constraint));
        }
    }

    private sealed class MutableClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class FixedOrderReader(OrderReturnContextSnapshot snapshot) : IOrderReturnReader
    {
        public Task<OrderReturnContextSnapshot?> GetReturnContextAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken) =>
            Task.FromResult<OrderReturnContextSnapshot?>(
                snapshot.SellerOrderId == sellerOrderId ? snapshot : null);
    }

    private sealed class FixedFulfillmentReader(OrderReturnContextSnapshot order, Guid lineId)
        : IFulfillmentReturnReader
    {
        public Task<FulfillmentReturnEligibilitySnapshot?> GetEligibilityAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken) =>
            Task.FromResult<FulfillmentReturnEligibilitySnapshot?>(
                new FulfillmentReturnEligibilitySnapshot(
                    sellerOrderId,
                    new Dictionary<Guid, decimal> { [lineId] = order.Lines.Single().Quantity },
                    Now,
                    new Dictionary<Guid, DateTimeOffset> { [lineId] = Now },
                    [new LineDeliverySlice(lineId, order.Lines.Single().Quantity, Now)]));

        public Task<IReadOnlyDictionary<Guid, DateTimeOffset?>> GetLastDeliveredAtBySellerOrderIdsAsync(
            IReadOnlyList<Guid> sellerOrderIds,
            CancellationToken cancellationToken)
        {
            IReadOnlyDictionary<Guid, DateTimeOffset?> map = sellerOrderIds
                .Where(x => x == order.SellerOrderId)
                .ToDictionary(x => x, _ => (DateTimeOffset?)Now);
            return Task.FromResult(map);
        }
    }

    private sealed class FixedPaymentReader(decimal amount) : IPaymentReturnReader
    {
        private readonly Guid _paymentId = Guid.NewGuid();

        public Task<PaymentReturnSnapshot?> GetAsync(
            Guid paymentId,
            Guid actorUserId,
            Guid? buyerPartyId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PaymentReturnSnapshot?>(
                new PaymentReturnSnapshot(
                    paymentId == Guid.Empty ? _paymentId : paymentId,
                    Guid.NewGuid(),
                    amount,
                    "IRR",
                    "Succeeded"));

        public Task<PaymentReturnSnapshot?> GetLatestForCheckoutAsync(
            Guid checkoutId,
            Guid actorUserId,
            Guid? buyerPartyId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PaymentReturnSnapshot?>(
                new PaymentReturnSnapshot(_paymentId, checkoutId, amount, "IRR", "Succeeded"));
    }

    private sealed class ScriptedGateway : IPaymentRefundGateway
    {
        public List<(Guid PaymentId, decimal Amount)> Calls { get; } = [];

        public Task<GatewayRefundResult> RefundAsync(
            Guid paymentId,
            decimal amount,
            string currency,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            Calls.Add((paymentId, amount));
            return Task.FromResult(new GatewayRefundResult(true, $"ref-274r1-{Calls.Count}", null));
        }
    }

    private sealed class ScriptedWallet : IWalletRefundCreditPort
    {
        public List<Guid> Calls { get; } = [];

        public Task<WalletRefundCreditResultDto> CreditRefundAsync(
            Guid customerActorUserId,
            decimal amount,
            string currency,
            Guid returnRequestId,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            Calls.Add(returnRequestId);
            return Task.FromResult(new WalletRefundCreditResultDto(amount, false));
        }
    }

    private sealed class ScriptedInventory : IReturnInventoryGateway
    {
        public List<(Guid ReservationId, decimal Quantity, string IdempotencyKey)> Restocked { get; } = [];

        public Task RestockConsumedReservationAsync(
            Guid reservationId,
            decimal quantity,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            if (!Restocked.Any(x => string.Equals(x.IdempotencyKey, idempotencyKey, StringComparison.Ordinal)))
            {
                Restocked.Add((reservationId, quantity, idempotencyKey));
            }

            return Task.CompletedTask;
        }
    }
}
