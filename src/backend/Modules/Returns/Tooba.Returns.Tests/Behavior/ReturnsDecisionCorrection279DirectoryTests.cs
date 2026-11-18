using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Contracts.Returns;
using Tooba.Order.Contracts.Returns;
using Tooba.Payment.Contracts.Returns;
using Tooba.Returns.Application.Composition;
using Tooba.Returns.Application.ReturnRequests.Commands;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Application.ReturnRequests.Ports;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.Aggregates;
using Tooba.Returns.Domain.ValueObjects;
using Tooba.Returns.Infrastructure.Directories;
using Tooba.Returns.Infrastructure.Evaluators;
using Tooba.Returns.Infrastructure.Observability;
using Tooba.Returns.Infrastructure.Persistence;
using Tooba.Wallet.Contracts.Refunds;
using Xunit;

namespace Tooba.Returns.Tests.Behavior;

/// <summary>
/// TB-279 — the owning directory behind the audited correction, the reviewed per-item resolution and the physical
/// custody handback. Every fact here drives the REAL <see cref="ReturnDirectory"/> over its own stored rows, so the
/// append-only revision history, the version fence, the frozen approved amount and the custody remainder are proven at
/// the write boundary instead of only as pure policy arithmetic. No provider, gateway or Inventory effect is stubbed
/// into success: money/stock ports are scripted and their call count is asserted.
/// </summary>
public sealed class ReturnsDecisionCorrection279DirectoryTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-18T09:00:00Z");

    private static readonly Guid SellerPartyId = Guid.Parse("01a03826-97c5-7000-ad15-c9d141b1f32e");

    private const decimal Requested = 2m;

    private const decimal UnitPrice = 1000m;

    // ------------------------------------------------------------------ A: audited correction

    [Fact]
    public async Task A_reopen_appends_one_reviewed_revision_and_keeps_the_corrected_decision_visible()
    {
        await using var harness = await SeedAsync(Stage.ApprovedNoMoney);

        var snapshot = await harness.ReopenAsync("reopen-1", "تصمیم اولیه نادرست بود", expectedVersion: 0);

        // The case is back in review, the FIRST decision row is untouched audit history and the correction is one
        // appended revision with its own reviewed actor/reason/occurrence.
        Assert.Equal(ReturnRequestStatus.Requested, snapshot.Status);
        Assert.Null(snapshot.EffectiveDecision);
        Assert.Equal(ReturnDecisionRevision.ApproveDecision, snapshot.RecordedDecision);
        Assert.Equal(1, snapshot.DecisionVersion);
        var revision = Assert.Single(snapshot.DecisionHistory);
        Assert.Equal(1, revision.Version);
        Assert.Equal(ReturnDecisionRevision.ReopenDecision, revision.Decision);
        Assert.Equal("تصمیم اولیه نادرست بود", revision.Reason);

        // No money, no stock, no custody and no release: a correction is a decision fact only.
        Assert.Equal(1, await harness.RevisionCountAsync());
        Assert.Equal(0, await harness.AttemptCountAsync());
        Assert.Equal(0, harness.Gateway.Calls);
        Assert.Empty(snapshot.Custody);
        Assert.True(snapshot.Work!.CanDecide, System.Text.Json.JsonSerializer.Serialize(snapshot.Work));
        Assert.False(snapshot.Work.CanStartRefund);
    }

    [Fact]
    public async Task A_same_occurrence_replays_and_a_changed_reason_conflicts()
    {
        await using var harness = await SeedAsync(Stage.ApprovedNoMoney);

        await harness.ReopenAsync("reopen-1", "دلیل بازبینی");
        await harness.ReopenAsync("reopen-1", "دلیل بازبینی");

        Assert.Equal(1, await harness.RevisionCountAsync());

        var conflict = await Assert.ThrowsAsync<ContractOperationException>(
            () => harness.ReopenAsync("reopen-1", "دلیل دیگر"));
        Assert.Equal(ReturnsErrorCodes.CorrectionOccurrenceConflict, conflict.Code);
        Assert.Equal(1, await harness.RevisionCountAsync());
    }

    [Fact]
    public async Task A_stale_reviewed_version_is_refused_before_any_row()
    {
        await using var harness = await SeedAsync(Stage.ApprovedNoMoney);

        var stale = await Assert.ThrowsAsync<ContractOperationException>(
            () => harness.ReopenAsync("reopen-1", "دلیل بازبینی", expectedVersion: 7));

        Assert.Equal(ReturnsErrorCodes.CorrectionVersionStale, stale.Code);
        Assert.Equal(0, await harness.RevisionCountAsync());
        Assert.Equal(ReturnRequestStatus.Approved, (await harness.SnapshotAsync()).Status);
    }

    [Fact]
    public async Task A_recorded_money_attempt_blocks_the_correction()
    {
        await using var harness = await SeedAsync(Stage.ApprovedPendingAttempt);

        var blocked = await Assert.ThrowsAsync<ContractOperationException>(
            () => harness.ReopenAsync("reopen-1", "دلیل بازبینی", expectedVersion: 0));

        Assert.Equal(ReturnsErrorCodes.CorrectionFinancialEffectRecorded, blocked.Code);
        Assert.Equal(0, await harness.RevisionCountAsync());
        Assert.Equal(ReturnRequestStatus.Approved, (await harness.SnapshotAsync()).Status);
    }

    [Fact]
    public async Task A_post_correction_decision_binds_the_current_revision_and_a_stale_one_cannot()
    {
        await using var harness = await SeedAsync(Stage.ApprovedNoMoney);
        await harness.ReopenAsync("reopen-1", "دلیل بازبینی", expectedVersion: 0);

        // The review that produced version 0 is stale now: it may not approve the current case.
        var stale = await Assert.ThrowsAsync<ContractOperationException>(
            () => harness.ApproveAsync(expectedVersion: 0));
        Assert.Equal(ReturnsErrorCodes.CorrectionVersionStale, stale.Code);
        Assert.Equal(ReturnRequestStatus.Requested, (await harness.SnapshotAsync()).Status);

        // The review of the CURRENT revision (1) approves, and the effective decision/version move together.
        var approved = await harness.ApproveAsync(expectedVersion: 1);
        Assert.Equal(ReturnRequestStatus.Approved, approved.Status);
        Assert.Equal(ReturnDecisionRevision.ApproveDecision, approved.EffectiveDecision);
        Assert.Equal(2, approved.DecisionVersion);
        Assert.Equal(0, harness.Gateway.Calls);
    }

    [Fact]
    public async Task A_rejected_case_re_acquires_its_own_released_capacity()
    {
        // A rejected legacy case has no allocation rows: reopening binds its own quantity to the still-open delivered
        // slice under the seller-order serialization, and its OWN occupancy never blocks that re-acquisition.
        await using var harness = await SeedAsync(Stage.RejectedNoMoney);

        var reopened = await harness.ReopenAsync("reopen-rejected-1", "رد با شواهد نادرست", expectedVersion: 0);

        Assert.Equal(ReturnRequestStatus.Requested, reopened.Status);
        Assert.Equal(1, await harness.RevisionCountAsync());
        Assert.Equal(1, await harness.AllocationCountAsync());
        Assert.Equal(0, harness.Gateway.Calls);
        Assert.True(reopened.Work!.CanDecide);
    }

    [Fact]
    public async Task A_rejected_capacity_that_a_sibling_case_already_took_is_a_typed_conflict()
    {
        // The same seller order line is already occupied by another active claim: the capacity was genuinely released
        // by the rejection, so the correction can never steal or overrun it.
        await using var harness = await SeedAsync(Stage.RejectedNoMoney, rivalClaim: true);

        var conflict = await Assert.ThrowsAsync<ContractOperationException>(
            () => harness.ReopenAsync("reopen-rejected-1", "رد با شواهد نادرست", expectedVersion: 0));

        Assert.Equal(ReturnsErrorCodes.CorrectionCapacityConflict, conflict.Code);
        Assert.Equal(0, await harness.RevisionCountAsync());
        Assert.Equal(0, await harness.AllocationCountAsync());
        Assert.Equal(ReturnRequestStatus.Rejected, (await harness.SnapshotAsync()).Status);
    }

    // ------------------------------------------------------------------ B: reviewed per-item resolution

    [Fact]
    public async Task B_a_partial_plan_freezes_only_the_accepted_money_before_the_first_attempt()
    {
        // 2 requested, 1 usable + 1 damaged received: the operator accepts the usable unit and rejects the damaged one.
        await using var harness = await SeedAsync(Stage.RequestedWithMixedReceipt);

        var published = await harness.PublishResolutionAsync(
            "resolution-1",
            expectedVersion: 0,
            (1m, 1m, "واحد آسیب‌دیده قابل بازگشت وجه نیست."));

        Assert.NotNull(published.Resolution);
        Assert.True(published.Resolution!.IsComplete);
        Assert.Equal(1m, published.Resolution.Lines.Single().Accepted);
        Assert.Equal(1m, published.Resolution.Lines.Single().Rejected);
        Assert.Equal(UnitPrice, published.Resolution.ApprovedAmount);
        // Nothing is frozen yet: the plan is a review fact, not money.
        Assert.Null(published.ApprovedAmount);
        Assert.Equal(Requested * UnitPrice, published.RefundAmount);

        var approved = await harness.ApproveAsync(expectedVersion: 0);

        // The approved money is the accepted quantity at the STORED unit price — never the original 2-unit claim.
        Assert.Equal(UnitPrice, approved.ApprovedAmount);
        Assert.Equal(Requested * UnitPrice, approved.RefundAmount);
        Assert.True(approved.Resolution!.FromReviewedPlan);
        Assert.Equal(0, harness.Gateway.Calls);

        // The frozen amount is the persisted case fact, exactly once.
        await using var db = CreateDb(harness.DbName);
        var stored = await db.ReturnRequests.AsNoTracking().SingleAsync();
        Assert.Equal(UnitPrice, stored.ApprovedAmount);
    }

    [Fact]
    public async Task B_a_plan_that_accepts_more_than_was_received_is_refused_with_no_row()
    {
        // 2 requested but only 1 physically received: the untouched unit cannot be named "accepted".
        await using var harness = await SeedAsync(Stage.RequestedWithPartialReceipt);

        var refused = await Assert.ThrowsAsync<ContractOperationException>(
            () => harness.PublishResolutionAsync(
                "resolution-1",
                expectedVersion: 0,
                (2m, 0m, "پذیرش کامل")));

        Assert.Equal(ReturnsErrorCodes.ResolutionReceivedExceeded, refused.Code);
        Assert.Equal(0, await harness.ResolutionCountAsync());
    }

    [Fact]
    public async Task B_a_plan_that_leaves_part_of_the_claim_unaccounted_is_refused()
    {
        await using var harness = await SeedAsync(Stage.RequestedWithPartialReceipt);

        var refused = await Assert.ThrowsAsync<ContractOperationException>(
            () => harness.PublishResolutionAsync(
                "resolution-1",
                expectedVersion: 0,
                (1m, 0m, "پذیرش یک واحد")));

        Assert.Equal(ReturnsErrorCodes.ResolutionIncomplete, refused.Code);
        Assert.Equal(0, await harness.ResolutionCountAsync());
    }

    // ------------------------------------------------------------------ C: custody of rejected received goods

    [Fact]
    public async Task C_custody_hands_back_only_the_rejected_remainder_and_never_claims_settlement()
    {
        await using var harness = await SeedAsync(Stage.RequestedWithMixedReceipt);
        await harness.PublishResolutionAsync(
            "resolution-1",
            expectedVersion: 0,
            (1m, 1m, "واحد آسیب‌دیده قابل بازگشت وجه نیست."));
        await harness.ApproveAsync(expectedVersion: 0);

        var over = await Assert.ThrowsAsync<ContractOperationException>(
            () => harness.CustodyAsync("custody-over", quantity: 2m, "رسید تحویل حضوری ۱"));
        Assert.Equal(ReturnsErrorCodes.CustodyQuantityInvalid, over.Code);

        var handed = await harness.CustodyAsync("custody-1", quantity: 1m, "رسید تحویل حضوری ۱");
        var custody = Assert.Single(handed.Custody!);
        Assert.Equal(1m, custody.Quantity);
        Assert.Equal(ReturnItemCondition.Damaged, custody.Condition);
        Assert.Equal("رسید تحویل حضوری ۱", custody.ProofReference);
        Assert.Equal(handed.RequestedByUserId, custody.RecipientUserId);
        Assert.Equal(0, harness.Gateway.Calls);
        Assert.Equal(0, harness.Inventory.Calls);

        // An exact replay is the same fact; another unit is over-handback, and the missing restock of the ACCEPTED
        // usable unit still keeps the case physically incomplete.
        await harness.CustodyAsync("custody-1", quantity: 1m, "رسید تحویل حضوری ۱");
        Assert.Equal(1, await harness.CustodyCountAsync());
        var again = await Assert.ThrowsAsync<ContractOperationException>(
            () => harness.CustodyAsync("custody-2", quantity: 1m, "رسید تحویل حضوری ۲"));
        Assert.True(again.Code is ReturnsErrorCodes.CustodyQuantityInvalid or ReturnsErrorCodes.CustodyOverHanded);
        Assert.NotEqual(ReturnRecoveryPhase.Settled, handed.Recovery!.Phase);
        Assert.True(handed.Recovery.PhysicalComplete is false);
    }

    // ------------------------------------------------------------------ fixtures

    private enum Stage
    {
        ApprovedNoMoney,
        ApprovedPendingAttempt,
        RejectedNoMoney,
        RequestedWithMixedReceipt,
        RequestedWithPartialReceipt,
    }

    private sealed class Harness : IAsyncDisposable
    {
        private readonly ReturnDirectory _directory;
        private readonly Guid _requestedByUserId;

        public Harness(
            string dbName,
            ReturnsDbContext db,
            Guid returnRequestId,
            Guid returnItemId,
            Guid orderLineId,
            decimal unitPrice,
            OrderReturnContextSnapshot order,
            ReturnDecisionRecord? decision)
        {
            DbName = dbName;
            Db = db;
            ReturnRequestId = returnRequestId;
            ReturnItemId = returnItemId;
            _requestedByUserId = order.PlacedByUserId;
            Gateway = new CountingGateway();
            Inventory = new CountingInventory();
            var clock = new FixedClock(Now);
            var orderReader = new FixedOrderReader(order);
            _directory = new ReturnDirectory(
                db,
                new OpenReturnUseCaseGuard(),
                orderReader,
                new ReturnEligibilityEvaluator(
                    orderReader,
                    new FixedFulfillmentReader(order, orderLineId),
                    db,
                    clock),
                new FixedPaymentReader(unitPrice * Requested),
                Gateway,
                new NoopWallet(),
                Inventory,
                new ReturnsInstrumentation(),
                NullLogger<ReturnDirectory>.Instance,
                clock,
                new UuidV7IdGenerator());
            _ = decision;
        }

        public string DbName { get; }

        public ReturnsDbContext Db { get; }

        public Guid ReturnRequestId { get; }

        public Guid ReturnItemId { get; }

        public CountingGateway Gateway { get; }

        public CountingInventory Inventory { get; }

        public Task<ReturnSnapshot> ReopenAsync(string occurrenceKey, string reason, int? expectedVersion = null) =>
            _directory.ReopenForReviewAsync(
                new ReopenReturnForReviewCommand(
                    ReturnRequestId,
                    Guid.NewGuid(),
                    occurrenceKey,
                    reason,
                    expectedVersion,
                    SellerPartyId),
                CancellationToken.None);

        public Task<ReturnSnapshot> PublishResolutionAsync(
            string occurrenceKey,
            int expectedVersion,
            params (decimal Accepted, decimal Rejected, string Reason)[] lines) =>
            _directory.PublishResolutionAsync(
                new PublishReturnResolutionCommand(
                    ReturnRequestId,
                    Guid.NewGuid(),
                    occurrenceKey,
                    expectedVersion,
                    lines
                        .Select(x => new ReturnResolutionLineCommand(
                            ReturnItemId,
                            AcceptedUsable: x.Accepted,
                            AcceptedDamaged: 0m,
                            RejectedReceivedUsable: 0m,
                            RejectedReceivedDamaged: x.Rejected,
                            RejectedUnreceived: 0m,
                            x.Reason))
                        .ToList(),
                    SellerPartyId),
                CancellationToken.None);

        public Task<ReturnSnapshot> ApproveAsync(int? expectedVersion = null) =>
            _directory.ApproveAsync(
                new ApproveReturnCommand(ReturnRequestId, Guid.NewGuid(), SellerPartyId)
                {
                    ExpectedDecisionVersion = expectedVersion,
                },
                CancellationToken.None);

        public Task<ReturnSnapshot> CustodyAsync(string occurrenceKey, decimal quantity, string proof) =>
            _directory.RecordCustodyAsync(
                new RecordReturnCustodyCommand(
                    ReturnRequestId,
                    Guid.NewGuid(),
                    occurrenceKey,
                    ReturnItemId,
                    quantity,
                    ReturnItemCondition.Damaged,
                    proof,
                    ReturnCustodyProofKindParser.OperatorAttestation,
                    SellerPartyId),
                CancellationToken.None);

        public async Task<ReturnSnapshot> SnapshotAsync() =>
            (await _directory.GetAsync(ReturnRequestId, CancellationToken.None))!;

        public Task<int> RevisionCountAsync() =>
            Db.ReturnDecisionRevisions.CountAsync(x => x.ReturnRequestId == ReturnRequestId);

        public Task<int> AttemptCountAsync() =>
            Db.RefundAttempts.CountAsync(x => x.ReturnRequestId == ReturnRequestId);

        public Task<int> ReceiptCountAsync() =>
            Db.ReturnReceipts.CountAsync(x => x.ReturnRequestId == ReturnRequestId);

        public Task<int> ResolutionCountAsync() =>
            Db.ReturnItemResolutions.CountAsync(x => x.ReturnRequestId == ReturnRequestId);

        public Task<int> CustodyCountAsync() =>
            Db.ReturnItemCustodies.CountAsync(x => x.ReturnRequestId == ReturnRequestId);

        public Task<int> AllocationCountAsync() =>
            Db.ReturnDeliveryAllocations.CountAsync(x => x.ReturnRequestId == ReturnRequestId);

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    private static async Task<Harness> SeedAsync(Stage stage, bool rivalClaim = false)
    {
        var dbName = Guid.NewGuid().ToString("N");
        var db = CreateDb(dbName);
        var customer = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var returnRequestId = Guid.NewGuid();

        var request = ReturnRequest.Create(
            returnRequestId,
            () => Guid.NewGuid(),
            orderId,
            Guid.NewGuid(),
            SellerPartyId,
            customer,
            $"tb279-{returnRequestId:N}",
            "دلیل مرجوعی",
            "IRR",
            [(lineId, Requested, UnitPrice, (Guid?)reservationId)],
            Now,
            RefundDestination.OriginalPayment);
        db.ReturnRequests.Add(request);
        db.ReturnItems.AddRange(request.Items);

        if (rivalClaim)
        {
            // A sibling active claim on the SAME seller order line. It predates per-delivery allocation, so it occupies
            // its quantity conservatively — exactly the competitor a rejected case must win or lose against.
            var rival = ReturnRequest.Create(
                Guid.NewGuid(),
                () => Guid.NewGuid(),
                orderId,
                Guid.NewGuid(),
                SellerPartyId,
                customer,
                $"tb279-rival-{Guid.NewGuid():N}",
                "درخواست رقیب",
                "IRR",
                [(lineId, Requested, UnitPrice, (Guid?)null)],
                Now,
                RefundDestination.OriginalPayment);
            db.ReturnRequests.Add(rival);
            db.ReturnItems.AddRange(rival.Items);
        }

        ReturnDecisionRecord? decision = null;
        if (stage is Stage.ApprovedNoMoney or Stage.ApprovedPendingAttempt or Stage.RejectedNoMoney)
        {
            // The recorded FIRST decision (version 0) of a decided case is immutable baseline history.
            var rejected = stage == Stage.RejectedNoMoney;
            decision = new ReturnDecisionRecord
            {
                ReturnRequestId = returnRequestId,
                Decision = rejected ? ReturnRecoveryPolicy.RejectedDecision : ReturnRecoveryPolicy.ApprovedDecision,
                Reason = rejected ? "کالا مطابق سیاست مرجوعی نیست." : null,
                DecidedByUserId = Guid.NewGuid(),
                DecidedAt = Now,
            };
            db.ReturnDecisions.Add(decision);
            if (rejected)
            {
                request.Reject(Now);
            }
            else
            {
                request.Approve(Guid.NewGuid(), Now);
            }
        }

        var receivedUsable = stage switch
        {
            Stage.RequestedWithMixedReceipt => 1m,
            Stage.RequestedWithPartialReceipt => 1m,
            _ => Requested,
        };
        var receivedDamaged = stage == Stage.RequestedWithMixedReceipt ? 1m : 0m;
        var receipt = ReturnReceipt.Create(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            returnRequestId,
            $"tb279-delivery-1-{stage}",
            Guid.NewGuid(),
            Now,
            receivedDamaged > 0m
                ?
                [
                    (request.Items.Single().ReturnItemId, receivedUsable, ReturnItemCondition.Usable),
                    (request.Items.Single().ReturnItemId, receivedDamaged, ReturnItemCondition.Damaged),
                ]
                : [(request.Items.Single().ReturnItemId, receivedUsable, ReturnItemCondition.Usable)]);
        db.ReturnReceipts.Add(receipt);
        db.ReturnReceiptLines.AddRange(receipt.Lines);

        if (stage == Stage.ApprovedPendingAttempt)
        {
            // A recorded money attempt while the case is still in its decided state: ANY attempt is a real financial
            // effect the correction may not erase.
            var paymentId = Guid.NewGuid();
            var pending = request.BeginRefundAttempt(
                Guid.NewGuid(), paymentId, $"refund-{returnRequestId:N}", Now);
            db.RefundAttempts.Add(pending);
        }

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        await db.DisposeAsync();

        var order = new OrderReturnContextSnapshot(
            orderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            customer,
            true,
            "IRR",
            [new OrderReturnLineSnapshot(lineId, Requested, UnitPrice, "IRR", reservationId, true, 7, null, null, QuantityDecimalPlacesSnapshot: 0)]);

        return new Harness(
            dbName,
            CreateDb(dbName),
            returnRequestId,
            request.Items.Single().ReturnItemId,
            lineId,
            UnitPrice,
            order,
            decision);
    }

    private static ReturnsDbContext CreateDb(string name) => new(
        new DbContextOptionsBuilder<ReturnsDbContext>()
            .UseInMemoryDatabase(name)
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
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

    /// <summary>A provider port that counts calls and REFUSES to move money: these facts must never reach it.</summary>
    private sealed class CountingGateway : IPaymentRefundGateway
    {
        public int Calls { get; private set; }

        public Task<GatewayRefundResult> RefundAsync(
            Guid paymentId,
            decimal amount,
            string currency,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            Calls++;
            throw new InvalidOperationException("tb279: no refund may be sent by a correction/resolution/custody fact");
        }
    }

    private sealed class NoopWallet : IWalletRefundCreditPort
    {
        public Task<WalletRefundCreditResultDto> CreditRefundAsync(
            Guid customerActorUserId,
            decimal amount,
            string currency,
            Guid returnRequestId,
            string idempotencyKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(new WalletRefundCreditResultDto(amount, false));
    }

    /// <summary>An Inventory port that counts calls and REFUSES to restock: no correction/resolution/custody restocks.</summary>
    private sealed class CountingInventory : IReturnInventoryGateway
    {
        public int Calls { get; private set; }

        public Task RestockConsumedReservationAsync(
            Guid reservationId,
            decimal quantity,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            Calls++;
            throw new InvalidOperationException("tb279: no restock may be sent by a correction/resolution/custody fact");
        }
    }
}

