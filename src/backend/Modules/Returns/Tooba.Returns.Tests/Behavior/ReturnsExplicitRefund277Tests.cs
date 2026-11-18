using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Fulfillment.Contracts.Returns;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Contracts.Returns;
using Tooba.Payment.Contracts.Returns;
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
using Tooba.Returns.Infrastructure.Queries;
using Tooba.Wallet.Contracts.Dtos;
using Tooba.Wallet.Contracts.Refunds;
using Xunit;

namespace Tooba.Returns.Tests.Behavior;

/// <summary>
/// TB-277 — approval and money are separate facts, and the receiving surface is bounded by the ORDERING UNIT.
/// <para>
/// Approval records ONLY the durable decision: it starts no refund attempt, sends nothing to a gateway or the wallet,
/// and restocks nothing. The first refund is then its OWN deliberate intent whose identity is derived from the case, so
/// a duplicate click, a lost response or a remount answers the SAME stored attempt instead of minting a second one.
/// </para>
/// </summary>
public sealed class ReturnsExplicitRefund277Tests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-17T09:00:00Z");

    private static Guid SellerPartyId { get; } = Guid.Parse("01a03826-97c5-7000-ad15-c9d141b1f32e");

    // ------------------------------------------------------------------ A: approval is a decision, never money

    [Fact]
    public async Task Approval_records_the_decision_only_and_sends_no_money_and_no_stock()
    {
        var fixture = await SeedAsync();
        await using var harness = fixture.Open();

        var approved = await harness.ApproveAsync();

        // The case carries the recorded decision and the Approved status…
        Assert.Equal(ReturnRequestStatus.Approved, approved.Status);
        Assert.Equal(ReturnRecoveryPolicy.ApprovedDecision, approved.RecordedDecision);
        // …and NOTHING financial or physical happened as a side effect.
        Assert.Empty(approved.RefundAttempts);
        Assert.Equal(0, await harness.AttemptCount());
        Assert.Empty(harness.Gateway.Calls);
        Assert.Equal(0, harness.Inventory.RestockCalls);
        Assert.Empty(harness.Wallet.Calls);

        var stored = await harness.Snapshot();
        Assert.NotNull(stored);
        Assert.Empty(stored!.RefundAttempts);
        Assert.Equal(ReturnRequestStatus.Approved, stored.Status);
    }

    [Fact]
    public async Task Approved_without_an_attempt_describes_the_first_refund_as_active_operator_work()
    {
        var fixture = await SeedAsync();
        await using var harness = fixture.Open();
        await harness.ApproveAsync();

        var work = (await harness.Snapshot())!.Work;
        Assert.NotNull(work);
        Assert.Equal(ReturnWorkCategory.Refund, work!.Category);
        Assert.True(work.CanStartRefund);
        // It is neither an unknown-outcome resume nor a definite-failure retry.
        Assert.False(work.CanResumeRefund);
        Assert.False(work.CanRetryRefund);
        Assert.False(work.CanSettlePhysical);
        Assert.True(work.NeedsOperator);
        Assert.True(work.IsOpen);
        // TB-279 appends the audited-correction verb: this approval reached no money/stock fact, so it stays
        // reviewable, and the first-refund verb is unchanged beside it.
        Assert.Equal(
            [ReturnWorkActionCodes.ReopenForReview, ReturnWorkActionCodes.StartRefund],
            work.ActionCodes);
        Assert.Null(work.PendingRefundAttemptId);
        Assert.Null(work.FailedRefundAttemptId);
    }

    // ------------------------------------------------------------------ A/B: the explicit first refund

    [Fact]
    public async Task The_first_refund_is_one_deterministic_attempt_and_a_second_call_replays_it_with_zero_effect()
    {
        var fixture = await SeedAsync();
        await using var harness = fixture.Open();
        await harness.ApproveAsync();

        var started = await harness.StartFirstRefundAsync();

        var expectedAttemptId = ReturnFirstRefundIdentity.DeriveAttemptId(fixture.ReturnRequestId);
        var expectedKey = ReturnFirstRefundIdentity.DeriveIdempotencyKey(fixture.ReturnRequestId);
        var attempt = Assert.Single(started.RefundAttempts);
        Assert.Equal(expectedAttemptId, attempt.RefundAttemptId);
        Assert.Equal(expectedKey, attempt.IdempotencyKey);
        Assert.Equal(RefundAttemptStatus.Succeeded, attempt.Status);
        // Exactly ONE provider call, with the case's own key — no second key, no counter.
        var call = Assert.Single(harness.Gateway.Calls);
        Assert.Equal(expectedKey, call.IdempotencyKey);
        Assert.Equal(fixture.Requested * fixture.UnitPrice, call.Amount);

        // The same reviewed intent again answers the STORED attempt: no new row and no second send.
        await harness.StartFirstRefundAsync();
        Assert.Equal(1, await harness.AttemptCount());
        Assert.Single(harness.Gateway.Calls);

        // Once the money is confirmed, the first-refund verb is no longer offered.
        var work = (await harness.Snapshot())!.Work;
        Assert.False(work!.CanStartRefund);
        Assert.DoesNotContain(ReturnWorkActionCodes.StartRefund, work.ActionCodes);
    }

    [Fact]
    public async Task An_unapproved_case_can_never_start_its_first_refund()
    {
        var fixture = await SeedAsync(approveOnSeed: false);
        await using var harness = fixture.Open();

        var refusal = await Assert.ThrowsAsync<ContractOperationException>(
            () => harness.StartFirstRefundAsync());

        Assert.Equal(ReturnsErrorCodes.FirstRefundNotLawful, refusal.Code);
        Assert.Equal(0, await harness.AttemptCount());
        Assert.Empty(harness.Gateway.Calls);
    }

    [Fact]
    public async Task A_pending_first_attempt_is_answered_from_storage_and_never_re_sent()
    {
        var fixture = await SeedAsync(approveOnSeed: true, pendingFirstAttempt: true);
        await using var harness = fixture.Open();

        var answered = await harness.StartFirstRefundAsync();

        var attempt = Assert.Single(answered.RefundAttempts);
        Assert.Equal(RefundAttemptStatus.Pending, attempt.Status);
        Assert.Equal(ReturnFirstRefundIdentity.DeriveAttemptId(fixture.ReturnRequestId), attempt.RefundAttemptId);
        // The stored unknown outcome is not re-sent by THIS intent; that is the explicit resume verb's job.
        Assert.Empty(harness.Gateway.Calls);
        Assert.Equal(1, await harness.AttemptCount());
        Assert.True((await harness.Snapshot())!.Work!.CanResumeRefund);
    }

    // ------------------------------------------------------------------ B: unit-aware quantity bounds reach the detail

    [Fact]
    public async Task The_admin_detail_publishes_the_units_own_precision_and_never_invents_one()
    {
        var fixture = await SeedAsync();
        await using var harness = fixture.Open();
        var variantId = Guid.NewGuid();
        var engine = new AdminReturnDetailQueryEngine(
            harness.Directory,
            new FixedGridEnrichment(fixture.OrderLineId, variantId, "g", quantityDecimalPlaces: 7),
            new FixedCatalogTitles(variantId, "لنوو اولترابوک مدل ۲"));

        var detail = await engine.QueryAsync(fixture.ReturnRequestId, CancellationToken.None);

        var item = Assert.Single(detail!.Items);
        // The owning line answered 7 decimals; the persisted scale caps it at 6 rather than inventing a bigger precision.
        Assert.Equal(6, item.UnitDecimalPlaces);
        Assert.Equal("g", item.UnitLabel);
        Assert.Equal("لنوو اولترابوک مدل ۲", item.ProductLabel);
    }

    [Fact]
    public async Task An_unanswered_owning_line_leaves_the_unit_precision_null()
    {
        var fixture = await SeedAsync();
        await using var harness = fixture.Open();
        var engine = new AdminReturnDetailQueryEngine(
            harness.Directory,
            new FixedGridEnrichment(Guid.NewGuid(), Guid.NewGuid(), "pcs", quantityDecimalPlaces: 0),
            new FixedCatalogTitles(null, null));

        var detail = await engine.QueryAsync(fixture.ReturnRequestId, CancellationToken.None);

        var item = Assert.Single(detail!.Items);
        // "Did not answer" is honest absence — never a default of whole pieces.
        Assert.Null(item.UnitDecimalPlaces);
        Assert.Null(item.UnitLabel);
        Assert.Null(item.ProductLabel);
    }

    // ------------------------------------------------------------------ harness

    private sealed class Harness : IAsyncDisposable
    {
        private readonly ReturnDirectory _directory;

        public Harness(Fixture fixture, ReturnsDbContext db)
        {
            Fixture = fixture;
            Db = db;
            Gateway = new ScriptedGateway();
            Wallet = new ScriptedWallet();
            Inventory = new ScriptedInventory();
            var clock = new MutableClock(Now);
            var orderReader = new FixedOrderReader(fixture.OrderSnapshot);
            _directory = new ReturnDirectory(
                db,
                new OpenReturnUseCaseGuard(),
                orderReader,
                new ReturnEligibilityEvaluator(
                    orderReader,
                    new FixedFulfillmentReader(fixture.OrderSnapshot, fixture.OrderLineId),
                    db,
                    clock),
                new FixedPaymentReader(fixture.Requested * fixture.UnitPrice, fixture.CheckoutId),
                Gateway,
                Wallet,
                Inventory,
                new ReturnsInstrumentation(),
                NullLogger<ReturnDirectory>.Instance,
                clock,
                new UuidV7IdGenerator());
        }

        public Fixture Fixture { get; }

        public IReturnDirectory Directory => _directory;

        public ReturnsDbContext Db { get; }

        public ScriptedGateway Gateway { get; }

        public ScriptedWallet Wallet { get; }

        public ScriptedInventory Inventory { get; }

        public Task<ReturnSnapshot> ApproveAsync() =>
            _directory.ApproveAsync(
                new ApproveReturnCommand(
                    Fixture.ReturnRequestId,
                    Guid.NewGuid(),
                    SellerPartyId,
                    RefundDestination.OriginalPayment),
                CancellationToken.None);

        public async Task<ReturnSnapshot> StartFirstRefundAsync()
        {
            // TB-279-R1 (F4): every NEW money command names the revision the operator reviewed.
            var reviewed = await _directory.GetAsync(Fixture.ReturnRequestId, CancellationToken.None);
            return await _directory.StartFirstRefundAsync(
                new StartReturnRefundCommand(
                    Fixture.ReturnRequestId,
                    Guid.NewGuid(),
                    reviewed?.DecisionVersion ?? 0),
                CancellationToken.None);
        }

        public Task<ReturnSnapshot?> Snapshot() => _directory.GetAsync(Fixture.ReturnRequestId, CancellationToken.None);

        public Task<int> AttemptCount() =>
            Db.RefundAttempts.CountAsync(x => x.ReturnRequestId == Fixture.ReturnRequestId);

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    private sealed record Fixture(
        string DbName,
        Guid ReturnRequestId,
        Guid ReturnItemId,
        Guid OrderLineId,
        decimal Requested,
        decimal UnitPrice,
        OrderReturnContextSnapshot OrderSnapshot,
        Guid CheckoutId)
    {
        public Harness Open() => new(this, CreateDb(DbName));
    }

    /// <summary>
    /// One delivered-customer case whose goods were fully received and inspected (2 of 2 usable), so approval and the
    /// first refund are both lawful on their own stored evidence.
    /// </summary>
    private static async Task<Fixture> SeedAsync(bool approveOnSeed = false, bool pendingFirstAttempt = false)
    {
        var dbName = Guid.NewGuid().ToString("N");
        var db = CreateDb(dbName);
        var customer = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var returnRequestId = Guid.NewGuid();
        const decimal requested = 2m;
        const decimal unitPrice = 1000m;

        var request = ReturnRequest.Create(
            returnRequestId,
            () => Guid.NewGuid(),
            orderId,
            Guid.NewGuid(),
            SellerPartyId,
            customer,
            $"tb277-{returnRequestId:N}",
            "دلیل مرجوعی",
            "IRR",
            [(lineId, requested, unitPrice, (Guid?)reservationId)],
            Now,
            RefundDestination.OriginalPayment);
        db.ReturnRequests.Add(request);
        db.ReturnItems.AddRange(request.Items);

        var receipt = ReturnReceipt.Create(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            returnRequestId,
            $"tb277-delivery-1-{returnRequestId:N}",
            Guid.NewGuid(),
            Now,
            [(request.Items.Single().ReturnItemId, requested, ReturnItemCondition.Usable)]);
        db.ReturnReceipts.Add(receipt);
        db.ReturnReceiptLines.AddRange(receipt.Lines);

        if (approveOnSeed)
        {
            var paymentId = Guid.NewGuid();
            db.ReturnDecisions.Add(new ReturnDecisionRecord
            {
                ReturnRequestId = returnRequestId,
                Decision = ReturnRecoveryPolicy.ApprovedDecision,
                Reason = null,
                DecidedByUserId = Guid.NewGuid(),
                DecidedAt = Now,
            });
            request.Approve(paymentId, Now);
            if (pendingFirstAttempt)
            {
                request.MarkRefundProcessing(Now);
                var pending = request.BeginRefundAttempt(
                    ReturnFirstRefundIdentity.DeriveAttemptId(returnRequestId),
                    paymentId,
                    ReturnFirstRefundIdentity.DeriveIdempotencyKey(returnRequestId),
                    Now);
                db.RefundAttempts.Add(pending);
            }
        }

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        await db.DisposeAsync();

        var orderSnapshot = new OrderReturnContextSnapshot(
            orderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            customer,
            true,
            "IRR",
            [new OrderReturnLineSnapshot(lineId, requested, unitPrice, "IRR", reservationId, true, 7, null, null, QuantityDecimalPlacesSnapshot: 0)]);

        return new Fixture(dbName, returnRequestId, request.Items.Single().ReturnItemId, lineId, requested, unitPrice, orderSnapshot, request.CheckoutId);
    }

    private static ReturnsDbContext CreateDb(string name) => new(
        new DbContextOptionsBuilder<ReturnsDbContext>()
            .UseInMemoryDatabase(name)
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

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

    private sealed class FixedPaymentReader(decimal amount, Guid owningCheckoutId) : IPaymentReturnReader
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
                    owningCheckoutId,
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
        public List<(Guid PaymentId, decimal Amount, string Currency, string IdempotencyKey)> Calls { get; } = [];

        public Task<GatewayRefundResult> RefundAsync(
            Guid paymentId,
            decimal amount,
            string currency,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            Calls.Add((paymentId, amount, currency, idempotencyKey));
            return Task.FromResult(new GatewayRefundResult(true, $"ref-277-{Calls.Count}", null));
        }
    }

    private sealed class ScriptedWallet : IWalletRefundCreditPort
    {
        public List<(decimal Amount, string IdempotencyKey)> Calls { get; } = [];

        public Task<WalletRefundCreditResultDto> CreditRefundAsync(
            Guid customerActorUserId,
            decimal amount,
            string currency,
            Guid returnRequestId,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            Calls.Add((amount, idempotencyKey));
            return Task.FromResult(new WalletRefundCreditResultDto(amount, false));
        }
    }

    private sealed class ScriptedInventory : IReturnInventoryGateway
    {
        public int RestockCalls { get; private set; }

        public Task RestockConsumedReservationAsync(
            Guid reservationId,
            decimal quantity,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            RestockCalls += 1;
            return Task.CompletedTask;
        }
    }

    /// <summary>Only the line read the admin detail needs; every other grid read is not part of this surface.</summary>
    private sealed class FixedGridEnrichment(Guid lineId, Guid variantId, string? unitCode, int quantityDecimalPlaces)
        : IOrderGridEnrichmentReader
    {
        public Task<IReadOnlyDictionary<Guid, OrderGridLineSnapshot>> GetLinesAsync(
            IReadOnlyList<Guid> orderLineIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, OrderGridLineSnapshot>>(
                orderLineIds.Contains(lineId) && variantId != Guid.Empty
                    ? new Dictionary<Guid, OrderGridLineSnapshot>
                    {
                        [lineId] = new(lineId, variantId, unitCode, quantityDecimalPlaces, null, true, 0),
                    }
                    : new Dictionary<Guid, OrderGridLineSnapshot>());

        public Task<IReadOnlyDictionary<Guid, string>> GetOrderNumbersAsync(
            IReadOnlyList<Guid> sellerOrderIds, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Guid>> SearchSellerOrderIdsByOrderNumberAsync(
            string term, int take, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<Guid>> FilterSellerOrderIdsByOrderNumberAsync(
            string? op, string? value, IReadOnlyList<string>? values, int take, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyDictionary<Guid, string>> GetRecipientNamesAsync(
            IReadOnlyList<Guid> checkoutIds, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<Guid>> SearchCheckoutIdsByRecipientAsync(
            string term, int take, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<Guid>> FilterCheckoutIdsByRecipientAsync(
            string? op, string? value, IReadOnlyList<string>? values, int take, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FixedCatalogTitles(Guid? variantId, string? title) : ICatalogVariantLookup
    {
        public Task<IReadOnlyDictionary<Guid, string>> GetVariantTitlesAsync(
            IReadOnlyList<Guid> variantIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(
                variantId is not null && title is not null && variantIds.Contains(variantId.Value)
                    ? new Dictionary<Guid, string> { [variantId.Value] = title }
                    : new Dictionary<Guid, string>());

        public Task<CatalogVariantLookupResult?> FindVariantAsync(
            Guid variantId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyDictionary<Guid, Guid?>> GetPrimaryCategoryIdsByVariantIdsAsync(
            IReadOnlyList<Guid> variantIds, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<Guid>> GetPublishedVariantIdsAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
