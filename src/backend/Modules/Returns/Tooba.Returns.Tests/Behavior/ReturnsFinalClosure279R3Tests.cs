using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Contracts.Returns;
using Tooba.Order.Contracts.Returns;
using Tooba.Payment.Contracts.Returns;
using Tooba.Returns.Application.ReturnRequests.Commands;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Application.ReturnRequests.Ports;
using Tooba.Returns.Domain.Aggregates;
using Tooba.Returns.Domain.ValueObjects;
using Tooba.Returns.Infrastructure.Directories;
using Tooba.Returns.Infrastructure.Evaluators;
using Tooba.Returns.Infrastructure.Observability;
using Tooba.Returns.Infrastructure.Persistence;
using Tooba.Wallet.Contracts.Dtos;
using Tooba.Wallet.Contracts.Refunds;
using Xunit;

namespace Tooba.Returns.Tests.Behavior;

/// <summary>
/// TB-279-R3 (C) — the two EFFECTFUL owner proofs the R2 review named: a real first refund that restocks the accepted
/// portion (never the rejected one) through the actual <see cref="ReturnDirectory"/>, and the shared payment budget of a
/// reviewed failed-child retry under a real cross-case obligation.
/// <para>
/// The ports here are scripted to SUCCEED and to RECORD their arguments, because a throw-if-called port can only prove
/// a zero-effect path. Nothing leaves the process: there is no real PSP, Wallet, Inventory or database write.
/// </para>
/// </summary>
public sealed class ReturnsFinalClosure279R3Tests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-18T12:00:00Z");

    private static readonly Guid SellerPartyId = Guid.Parse("01a03826-97c5-7000-ad15-c9d141b1f32e");

    private const decimal UnitPrice = 1000m;

    // ------------------------------------------------------------------ C1: the real first refund restocks 2, never 3

    [Fact]
    public async Task C1_the_real_first_refund_restocks_exactly_the_accepted_portion_and_replays_with_zero_effect()
    {
        // requested/received 3 usable, reviewed accepted 2 / rejected received 1: the money is 2 x the stored unit price.
        var fixture = await SeedAsync(requested: 3m, receivedUsable: 3m, acceptedUsable: 2m, rejectedReceivedUsable: 1m);
        await using var harness = fixture.Open();

        var approved = await harness.ApproveAsync();
        Assert.Equal(ReturnRequestStatus.Approved, approved.Status);
        Assert.Equal(UnitPrice * 2m, approved.ApprovedAmount);
        Assert.Empty(harness.Gateway.Calls);
        Assert.Empty(harness.Inventory.Restocked);

        var refunded = await harness.StartFirstRefundAsync(approved.DecisionVersion);
        Assert.Equal(ReturnRequestStatus.Completed, refunded.Status);

        // Exactly ONE durable first attempt carries the reviewed amount and the case-derived identity/key.
        var attempt = Assert.Single(refunded.RefundAttempts);
        Assert.Equal(RefundAttemptStatus.Succeeded, attempt.Status);
        Assert.Equal(UnitPrice * 2m, attempt.Amount);
        Assert.Equal(ReturnFirstRefundIdentity.DeriveAttemptId(fixture.ReturnRequestId), attempt.RefundAttemptId);
        Assert.Equal(ReturnFirstRefundIdentity.DeriveIdempotencyKey(fixture.ReturnRequestId), attempt.IdempotencyKey);
        Assert.Equal("psp-c1-ref", attempt.ProviderReference);

        // The gateway was called ONCE, with the stored amount/key; Inventory received the ACTUAL accepted 2 — never 3.
        var money = Assert.Single(harness.Gateway.Calls);
        Assert.Equal(UnitPrice * 2m, money.Amount);
        Assert.Equal(attempt.IdempotencyKey, money.IdempotencyKey);
        var restock = Assert.Single(harness.Inventory.Restocked);
        Assert.Equal(2m, restock.Quantity);
        Assert.Equal(fixture.ReservationId, restock.ReservationId);

        // Only the accepted portion became saleable; the rejected received unit stays owed custody, so the case is NOT
        // settled by money alone.
        var stored = await harness.StoredItemAsync(fixture.ReturnItemId);
        Assert.Equal(2m, stored.UsableRestockedQuantity);
        Assert.Empty(refunded.Custody);
        Assert.Equal(1m, refunded.Resolution!.Lines.Single().RejectedReceived);
        Assert.NotNull(refunded.Recovery);
        Assert.True(refunded.Recovery!.FinancialComplete);
        Assert.Equal(ReturnRecoveryPhase.PendingDisposition, refunded.Recovery.Phase);

        // A duplicate of the SAME first-refund intent answers its stored attempt with ZERO effect.
        var replay = await harness.StartFirstRefundAsync(approved.DecisionVersion);
        Assert.Single(replay.RefundAttempts);
        Assert.Single(harness.Gateway.Calls);
        Assert.Single(harness.Inventory.Restocked);
        Assert.Equal(ReturnRequestStatus.Completed, replay.Status);
    }

    // ------------------------------------------------------------------ C2: the real retry budget across cases

    [Fact]
    public async Task C2_the_real_failed_child_retry_refuses_a_cross_case_budget_overrun_and_mints_nothing()
    {
        // payment total 100; case A's reviewed failure is 60; another Returns case already reserves 60 on the SAME payment.
        var fixture = await SeedFailedAsync(paymentAmount: 100m, refundAmount: 60m, siblingReservation: 60m);
        await using var harness = fixture.Open();

        var refused = await Assert.ThrowsAsync<ContractOperationException>(
            () => harness.RetryRefundAsync());

        Assert.Equal(Tooba.Returns.Contracts.Errors.ReturnsErrorCodes.RefundBudgetExceeded, refused.Code);
        Assert.Equal(1, await harness.AttemptCountAsync());
        Assert.Empty(harness.Gateway.Calls);
        Assert.Empty(harness.Inventory.Restocked);
        Assert.Equal(ReturnRequestStatus.RefundFailed, (await harness.SnapshotAsync())!.Status);
    }

    [Fact]
    public async Task C2_an_independent_lawful_control_mints_exactly_one_deterministic_child_and_replays_it()
    {
        // The same reviewed failure, but the sibling now reserves only 40: 40 + 60 fits the 100 payment.
        var fixture = await SeedFailedAsync(paymentAmount: 100m, refundAmount: 60m, siblingReservation: 40m);
        await using var harness = fixture.Open();

        var retried = await harness.RetryRefundAsync();
        var child = Assert.Single(
            retried.RefundAttempts,
            x => x.RefundAttemptId != fixture.FailedAttemptId);
        Assert.Equal(
            ReturnRetryIdentity.DeriveAttemptId(fixture.ReturnRequestId, fixture.FailedAttemptId),
            child.RefundAttemptId);
        Assert.Equal(ReturnRetryIdentity.DeriveIdempotencyKey(fixture.FailedAttemptId), child.IdempotencyKey);
        Assert.Equal(60m, child.Amount);

        // ONE gateway call and ONE accepted-usable restock of the reviewed 1 unit at the stored unit price.
        Assert.Single(harness.Gateway.Calls);
        Assert.Equal(60m, harness.Gateway.Calls.Single().Amount);
        Assert.Equal(1m, Assert.Single(harness.Inventory.Restocked).Quantity);

        // The duplicate reviewed intent answers the SAME child and mints/sends nothing again.
        var replay = await harness.RetryRefundAsync();
        Assert.Equal(2, replay.RefundAttempts.Count);
        Assert.Single(harness.Gateway.Calls);
        Assert.Single(harness.Inventory.Restocked);
        Assert.Equal(2, await harness.AttemptCountAsync());
    }

    [Fact]
    public async Task C2_a_missing_payment_authority_refuses_the_retry_with_zero_attempt_and_effect()
    {
        var fixture = await SeedFailedAsync(paymentAmount: 100m, refundAmount: 60m, siblingReservation: 0m);
        await using var harness = fixture.Open(paymentAuthority: false);

        var refused = await Assert.ThrowsAsync<ContractOperationException>(
            () => harness.RetryRefundAsync());

        Assert.Equal(Tooba.Returns.Contracts.Errors.ReturnsErrorCodes.RefundPaymentMissing, refused.Code);
        Assert.Equal(1, await harness.AttemptCountAsync());
        Assert.Empty(harness.Gateway.Calls);
        Assert.Empty(harness.Inventory.Restocked);
    }

    // ------------------------------------------------------------------ fixtures

    private sealed record Fixture(
        string DbName,
        Guid ReturnRequestId,
        Guid ReturnItemId,
        Guid ReservationId,
        Guid FailedAttemptId,
        decimal RefundAmount,
        decimal PaymentAmount,
        Guid PaymentId,
        Guid CheckoutId,
        OrderReturnContextSnapshot Order)
    {
        /// <summary>A fresh owning context on the SAME in-memory database the seed committed.</summary>
        public Harness Open(bool paymentAuthority = true) => new(this, CreateDb(DbName), paymentAuthority);
    }

    private static async Task<Fixture> SeedAsync(
        decimal requested,
        decimal receivedUsable,
        decimal acceptedUsable,
        decimal rejectedReceivedUsable)
    {
        var dbName = Guid.NewGuid().ToString("N");
        var db = CreateDb(dbName);
        var customer = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var returnRequestId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        var request = ReturnRequest.Create(
            returnRequestId,
            () => Guid.NewGuid(),
            orderId,
            Guid.NewGuid(),
            SellerPartyId,
            customer,
            $"tb279r3-c1-{returnRequestId:N}",
            "دلیل مرجوعی",
            "IRR",
            [(lineId, requested, UnitPrice, (Guid?)reservationId)],
            Now,
            RefundDestination.OriginalPayment);
        db.ReturnRequests.Add(request);
        db.ReturnItems.AddRange(request.Items);

        var receipt = ReturnReceipt.Create(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            returnRequestId,
            $"tb279r3-c1-receipt",
            Guid.NewGuid(),
            Now,
            [(request.Items.Single().ReturnItemId, receivedUsable, ReturnItemCondition.Usable)]);
        db.ReturnReceipts.Add(receipt);
        db.ReturnReceiptLines.AddRange(receipt.Lines);

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
            [new OrderReturnLineSnapshot(lineId, requested, UnitPrice, "IRR", reservationId, true, 7, null, null, QuantityDecimalPlacesSnapshot: 0)]);

        return new Fixture(
            dbName,
            returnRequestId,
            request.Items.Single().ReturnItemId,
            reservationId,
            Guid.Empty,
            UnitPrice * requested,
            UnitPrice * requested,
            paymentId,
            request.CheckoutId,
            order);
    }

    /// <summary>
    /// A case carrying a REAL reviewed plan plus a positively bound FAILED attempt (a definite provider refusal), so the
    /// reviewed failed-child retry reaches its shared payment budget instead of an earlier refusal.
    /// </summary>
    private static async Task<Fixture> SeedFailedAsync(
        decimal paymentAmount,
        decimal refundAmount,
        decimal siblingReservation)
    {
        var dbName = Guid.NewGuid().ToString("N");
        var db = CreateDb(dbName);
        var customer = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var returnRequestId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        const decimal requested = 1m;
        var unitPrice = refundAmount;

        var request = ReturnRequest.Create(
            returnRequestId,
            () => Guid.NewGuid(),
            orderId,
            Guid.NewGuid(),
            SellerPartyId,
            customer,
            $"tb279r3-c2-{returnRequestId:N}",
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
            "tb279r3-c2-receipt",
            Guid.NewGuid(),
            Now,
            [(request.Items.Single().ReturnItemId, requested, ReturnItemCondition.Usable)]);
        db.ReturnReceipts.Add(receipt);
        db.ReturnReceiptLines.AddRange(receipt.Lines);

        // A reviewed plan accepting the single received unit freezes exactly `refundAmount`, then a definite provider
        // refusal leaves that reviewed failure current.
        db.ReturnItemResolutions.Add(new ReturnItemResolution
        {
            ReturnRequestId = returnRequestId,
            ReturnItemId = request.Items.Single().ReturnItemId,
            Version = 1,
            AcceptedQuantity = requested,
            RejectedQuantity = 0m,
            AcceptedUsableQuantity = requested,
            AcceptedDamagedQuantity = 0m,
            RejectedReceivedUsableQuantity = 0m,
            RejectedReceivedDamagedQuantity = 0m,
            RejectedUnreceivedQuantity = 0m,
            Reason = "پذیرش کامل واحد دریافتی",
            ActorUserId = Guid.NewGuid(),
            OccurrenceKey = "tb279r3-c2-plan-1",
            RecordedAt = Now,
        });
        db.ReturnDecisions.Add(new ReturnDecisionRecord
        {
            ReturnRequestId = returnRequestId,
            Decision = ReturnRecoveryPolicy.ApprovedDecision,
            Reason = null,
            DecidedByUserId = Guid.NewGuid(),
            DecidedAt = Now,
        });

        request.Approve(paymentId, Now, approvedAmount: refundAmount);
        request.MarkRefundProcessing(Now);
        var failed = request.BeginRefundAttempt(Guid.NewGuid(), paymentId, "tb279r3-c2-first-attempt", Now);
        failed.MarkFailed("PSP_DECLINED", Now);
        request.MarkRefundFailed(Now);
        db.RefundAttempts.Add(failed);

        if (siblingReservation > 0m)
        {
            // Another Returns case of the SAME payment whose Pending obligation must count against the shared budget.
            var sibling = ReturnRequest.Create(
                Guid.NewGuid(),
                () => Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                SellerPartyId,
                Guid.NewGuid(),
                $"tb279r3-c2-sibling-{Guid.NewGuid():N}",
                "پرونده دیگر روی همان پرداخت",
                "IRR",
                [(Guid.NewGuid(), 1m, siblingReservation, (Guid?)Guid.NewGuid())],
                Now,
                RefundDestination.OriginalPayment);
            db.ReturnRequests.Add(sibling);
            db.ReturnItems.AddRange(sibling.Items);
            sibling.Approve(paymentId, Now, approvedAmount: siblingReservation);
            var siblingAttempt = sibling.BeginRefundAttempt(
                Guid.NewGuid(),
                paymentId,
                $"tb279r3-c2-sibling-attempt-{Guid.NewGuid():N}",
                Now);
            db.RefundAttempts.Add(siblingAttempt);
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
            [new OrderReturnLineSnapshot(lineId, requested, unitPrice, "IRR", reservationId, true, 7, null, null, QuantityDecimalPlacesSnapshot: 0)]);

        return new Fixture(
            dbName,
            returnRequestId,
            request.Items.Single().ReturnItemId,
            reservationId,
            failed.RefundAttemptId,
            refundAmount,
            paymentAmount,
            paymentId,
            request.CheckoutId,
            order);
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

    /// <summary>The owning payment answer of the case's own checkout, or NO authority at all for the refusal fact.</summary>
    private sealed class ScriptedPaymentReader(
        Guid paymentId,
        Guid checkoutId,
        decimal amount,
        bool answers) : IPaymentReturnReader
    {
        public Task<PaymentReturnSnapshot?> GetAsync(
            Guid requestedPaymentId,
            Guid actorUserId,
            Guid? buyerPartyId,
            CancellationToken cancellationToken) =>
            Task.FromResult(answers
                ? new PaymentReturnSnapshot(paymentId, checkoutId, amount, "IRR", "Succeeded")
                : null);

        public Task<PaymentReturnSnapshot?> GetLatestForCheckoutAsync(
            Guid requestedCheckoutId,
            Guid actorUserId,
            Guid? buyerPartyId,
            CancellationToken cancellationToken) =>
            Task.FromResult(answers && requestedCheckoutId == checkoutId
                ? new PaymentReturnSnapshot(paymentId, checkoutId, amount, "IRR", "Succeeded")
                : null);
    }

    /// <summary>A gateway scripted to SUCCEED and to record the exact money it was asked to send.</summary>
    private sealed class RecordingGateway : IPaymentRefundGateway
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
            return Task.FromResult(new GatewayRefundResult(true, "psp-c1-ref", null));
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

    /// <summary>An Inventory port scripted to SUCCEED and to record the exact restock argument it received.</summary>
    private sealed class RecordingInventory : IReturnInventoryGateway
    {
        public List<(Guid ReservationId, decimal Quantity, string IdempotencyKey)> Restocked { get; } = [];

        public Task RestockConsumedReservationAsync(
            Guid reservationId,
            decimal quantity,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            Restocked.Add((reservationId, quantity, idempotencyKey));
            return Task.CompletedTask;
        }
    }

    private sealed class Harness : IAsyncDisposable
    {
        private readonly Fixture _fixture;
        private readonly IClock _clock = new FixedClock(Now);
        private readonly ReturnDirectory _directory;

        public Harness(Fixture fixture, ReturnsDbContext db, bool paymentAuthority)
        {
            _fixture = fixture;
            Db = db;
            Gateway = new RecordingGateway();
            Inventory = new RecordingInventory();
            var orderReader = new FixedOrderReader(fixture.Order);
            _directory = new ReturnDirectory(
                db,
                new OpenReturnUseCaseGuard(),
                orderReader,
                new ReturnEligibilityEvaluator(
                    orderReader,
                    new FixedFulfillmentReader(fixture.Order, fixture.Order.Lines.Single().OrderLineId),
                    db,
                    _clock),
                new ScriptedPaymentReader(fixture.PaymentId, fixture.CheckoutId, fixture.PaymentAmount, paymentAuthority),
                Gateway,
                new NoopWallet(),
                Inventory,
                new ReturnsInstrumentation(),
                NullLogger<ReturnDirectory>.Instance,
                _clock,
                new UuidV7IdGenerator());
        }

        public ReturnsDbContext Db { get; }

        public RecordingGateway Gateway { get; }

        public RecordingInventory Inventory { get; }

        public async Task<ReturnSnapshot> ApproveAsync()
        {
            var published = await _directory.PublishResolutionAsync(
                new PublishReturnResolutionCommand(
                    _fixture.ReturnRequestId,
                    Guid.NewGuid(),
                    "tb279r3-resolution-1",
                    0,
                    [
                        new ReturnResolutionLineCommand(
                            _fixture.ReturnItemId,
                            AcceptedUsable: 2m,
                            AcceptedDamaged: 0m,
                            RejectedReceivedUsable: 1m,
                            RejectedReceivedDamaged: 0m,
                            RejectedUnreceived: 0m,
                            "یک واحد مردود و تحت حضانت"),
                    ],
                    SellerPartyId),
                CancellationToken.None);
            return await _directory.ApproveAsync(
                new ApproveReturnCommand(
                    _fixture.ReturnRequestId,
                    Guid.NewGuid(),
                    SellerPartyId,
                    RefundDestination.OriginalPayment)
                {
                    ExpectedDecisionVersion = published.DecisionVersion,
                    ExpectedResolutionVersion = published.Resolution!.Version,
                },
                CancellationToken.None);
        }

        public Task<ReturnSnapshot> StartFirstRefundAsync(int? expectedDecisionVersion) =>
            _directory.StartFirstRefundAsync(
                new StartReturnRefundCommand(
                    _fixture.ReturnRequestId,
                    Guid.NewGuid(),
                    expectedDecisionVersion),
                CancellationToken.None);

        public async Task<ReturnSnapshot> RetryRefundAsync()
        {
            var reviewed = await _directory.GetAsync(_fixture.ReturnRequestId, CancellationToken.None);
            return await _directory.RetryRefundAsync(
                new RetryReturnRefundCommand(_fixture.ReturnRequestId, Guid.NewGuid())
                {
                    ExpectedFailedAttemptId = _fixture.FailedAttemptId,
                    ExpectedDecisionVersion = reviewed?.DecisionVersion ?? 0,
                },
                CancellationToken.None);
        }

        public Task<ReturnSnapshot?> SnapshotAsync() =>
            _directory.GetAsync(_fixture.ReturnRequestId, CancellationToken.None);

        public Task<ReturnItem> StoredItemAsync(Guid returnItemId) =>
            Db.ReturnItems.AsNoTracking().SingleAsync(x => x.ReturnItemId == returnItemId);

        public Task<int> AttemptCountAsync() =>
            Db.RefundAttempts.CountAsync(x => x.ReturnRequestId == _fixture.ReturnRequestId);

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
}
