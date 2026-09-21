using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Contracts.Returns;
using Tooba.Order.Contracts.Returns;
using Tooba.Payment.Contracts.Returns;
using Tooba.Returns.Application.Models;
using Tooba.Returns.Application.Ports;
using Tooba.Returns.Domain.ValueObjects;
using Tooba.Returns.Infrastructure.Directories;
using Tooba.Returns.Infrastructure.Evaluators;
using Tooba.Returns.Infrastructure.Observability;
using Tooba.Returns.Infrastructure.Persistence;
using Tooba.Wallet.Contracts.Dtos;
using Tooba.Wallet.Contracts.Refunds;
using Xunit;

namespace Tooba.Returns.Tests.Behavior;

public sealed class ReturnsCharacterizationTests
{
    [Fact]
    public async Task Eligibility_allows_delivered_within_window_and_denies_expired()
    {
        var sellerOrderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var deliveredAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        await using var db = CreateDb();
        var clock = new FixedClock(deliveredAt.AddDays(2));
        var allow = new ReturnEligibilityEvaluator(
            new FixedOrderReader(BuildOrder(sellerOrderId, lineId)),
            new FixedFulfillmentReader(BuildFulfillment(sellerOrderId, lineId, deliveredAt)),
            db,
            clock);
        var allowed = await allow.EvaluateAsync(sellerOrderId, CancellationToken.None);
        Assert.True(allowed.Eligible);

        var denyClock = new FixedClock(deliveredAt.AddDays(40));
        var deny = new ReturnEligibilityEvaluator(
            new FixedOrderReader(BuildOrder(sellerOrderId, lineId)),
            new FixedFulfillmentReader(BuildFulfillment(sellerOrderId, lineId, deliveredAt)),
            db,
            denyClock);
        var denied = await deny.EvaluateAsync(sellerOrderId, CancellationToken.None);
        Assert.False(denied.Eligible);
        Assert.Equal(ReturnEligibilityReasonCodes.WindowExpired, denied.ReasonCode);
    }

    [Fact]
    public async Task Create_approve_refund_preserves_amount_currency_and_is_idempotent()
    {
        var sellerOrderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var checkoutId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        await using var db = CreateDb();
        var clock = new SystemUtcClock();
        var ids = new UuidV7IdGenerator();
        var wallets = new RecordingWallet();
        var inventory = new RecordingInventory();
        var refunds = new RecordingRefundGateway();
        var payments = new FixedPaymentReader(new PaymentReturnSnapshot(paymentId, checkoutId, 2000m, "IRR", "Succeeded"));
        var order = BuildOrder(sellerOrderId, lineId, checkoutId, unitPrice: 1000m, qty: 2);
        var eligibility = new ReturnEligibilityEvaluator(
            new FixedOrderReader(order),
            new FixedFulfillmentReader(BuildFulfillment(sellerOrderId, lineId, clock.UtcNow.AddDays(-1))),
            db,
            clock);
        var directory = new ReturnDirectory(
            db,
            new OpenReturnUseCaseGuard(),
            new FixedOrderReader(order),
            eligibility,
            payments,
            refunds,
            wallets,
            inventory,
            new ReturnsInstrumentation(),
            NullLogger<ReturnDirectory>.Instance,
            clock,
            ids);

        var cmd = new CreateReturnCommand(
            sellerOrderId,
            order.PlacedByUserId,
            "idem-1",
            "reason",
            [new ReturnLineCommand(lineId, 2)],
            RefundDestination.OriginalPayment);
        var first = await directory.CreateAsync(cmd, CancellationToken.None);
        var second = await directory.CreateAsync(cmd, CancellationToken.None);
        Assert.Equal(first.ReturnRequestId, second.ReturnRequestId);
        Assert.Equal(2000m, first.RefundAmount);
        Assert.Equal("IRR", first.Currency);

        var approved = await directory.ApproveAsync(
            new ApproveReturnCommand(first.ReturnRequestId, Guid.NewGuid()),
            CancellationToken.None);
        Assert.Equal(ReturnRequestStatus.Completed, approved.Status);
        Assert.Single(refunds.Calls);
        Assert.Equal(paymentId, refunds.Calls[0].PaymentId);
        Assert.Equal(2000m, refunds.Calls[0].Amount);
        Assert.Equal("IRR", refunds.Calls[0].Currency);
        Assert.Empty(wallets.Credits);
    }

    [Fact]
    public async Task Wallet_destination_credits_wallet_without_psp_refund()
    {
        var sellerOrderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var checkoutId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        await using var db = CreateDb();
        var clock = new SystemUtcClock();
        var ids = new UuidV7IdGenerator();
        var wallets = new RecordingWallet();
        var inventory = new RecordingInventory();
        var refunds = new RecordingRefundGateway();
        var order = BuildOrder(sellerOrderId, lineId, checkoutId, unitPrice: 500m, qty: 1, reservationId: Guid.NewGuid());
        var directory = new ReturnDirectory(
            db,
            new OpenReturnUseCaseGuard(),
            new FixedOrderReader(order),
            new ReturnEligibilityEvaluator(
                new FixedOrderReader(order),
                new FixedFulfillmentReader(BuildFulfillment(sellerOrderId, lineId, clock.UtcNow.AddDays(-1))),
                db,
                clock),
            new FixedPaymentReader(new PaymentReturnSnapshot(paymentId, checkoutId, 500m, "IRR", "Succeeded")),
            refunds,
            wallets,
            inventory,
            new ReturnsInstrumentation(),
            NullLogger<ReturnDirectory>.Instance,
            clock,
            ids);

        var created = await directory.CreateAsync(
            new CreateReturnCommand(
                sellerOrderId,
                order.PlacedByUserId,
                "idem-wallet",
                null,
                [new ReturnLineCommand(lineId, 1)],
                RefundDestination.Wallet),
            CancellationToken.None);
        var approved = await directory.ApproveAsync(
            new ApproveReturnCommand(created.ReturnRequestId, Guid.NewGuid()),
            CancellationToken.None);
        Assert.Equal(ReturnRequestStatus.Completed, approved.Status);
        Assert.Empty(refunds.Calls);
        Assert.Single(wallets.Credits);
        Assert.Single(inventory.Restocks);
    }

    private static OrderReturnContextSnapshot BuildOrder(
        Guid sellerOrderId,
        Guid lineId,
        Guid? checkoutId = null,
        decimal unitPrice = 1000m,
        decimal qty = 2,
        Guid? reservationId = null) =>
        new(
            sellerOrderId,
            checkoutId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            true,
            "IRR",
            [new OrderReturnLineSnapshot(lineId, qty, unitPrice, "IRR", reservationId, true, 7)]);

    private static FulfillmentReturnEligibilitySnapshot BuildFulfillment(
        Guid sellerOrderId,
        Guid lineId,
        DateTimeOffset deliveredAt) =>
        new(
            sellerOrderId,
            new Dictionary<Guid, decimal> { [lineId] = 2 },
            deliveredAt,
            new Dictionary<Guid, DateTimeOffset> { [lineId] = deliveredAt },
            [new LineDeliverySlice(lineId, 2, deliveredAt)]);

    private static ReturnsDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ReturnsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ReturnsDbContext(options);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }

    private sealed class FixedOrderReader(OrderReturnContextSnapshot snapshot) : IOrderReturnReader
    {
        public Task<OrderReturnContextSnapshot?> GetReturnContextAsync(Guid sellerOrderId, CancellationToken cancellationToken) =>
            Task.FromResult<OrderReturnContextSnapshot?>(snapshot.SellerOrderId == sellerOrderId ? snapshot : null);
    }

    private sealed class FixedFulfillmentReader(FulfillmentReturnEligibilitySnapshot snapshot) : IFulfillmentReturnReader
    {
        public Task<FulfillmentReturnEligibilitySnapshot?> GetEligibilityAsync(Guid sellerOrderId, CancellationToken cancellationToken) =>
            Task.FromResult<FulfillmentReturnEligibilitySnapshot?>(snapshot.SellerOrderId == sellerOrderId ? snapshot : null);

        public Task<IReadOnlyDictionary<Guid, DateTimeOffset?>> GetLastDeliveredAtBySellerOrderIdsAsync(
            IReadOnlyList<Guid> sellerOrderIds,
            CancellationToken cancellationToken)
        {
            IReadOnlyDictionary<Guid, DateTimeOffset?> map = sellerOrderIds
                .Where(id => id == snapshot.SellerOrderId)
                .ToDictionary(id => id, _ => snapshot.LastDeliveredAt);
            return Task.FromResult(map);
        }
    }

    private sealed class FixedPaymentReader(PaymentReturnSnapshot snapshot) : IPaymentReturnReader
    {
        public Task<PaymentReturnSnapshot?> GetAsync(Guid paymentId, Guid actorUserId, Guid? buyerPartyId, CancellationToken cancellationToken) =>
            Task.FromResult<PaymentReturnSnapshot?>(snapshot);

        public Task<PaymentReturnSnapshot?> GetLatestForCheckoutAsync(Guid checkoutId, Guid actorUserId, Guid? buyerPartyId, CancellationToken cancellationToken) =>
            Task.FromResult<PaymentReturnSnapshot?>(snapshot);
    }

    private sealed class RecordingRefundGateway : IPaymentRefundGateway
    {
        public List<(Guid PaymentId, decimal Amount, string Currency)> Calls { get; } = [];

        public Task<GatewayRefundResult> RefundAsync(
            Guid paymentId,
            decimal amount,
            string currency,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            Calls.Add((paymentId, amount, currency));
            return Task.FromResult(new GatewayRefundResult(true, "ref-1", null));
        }
    }

    private sealed class RecordingWallet : IWalletRefundCreditPort
    {
        public List<(Guid UserId, decimal Amount, string Currency)> Credits { get; } = [];

        public Task<WalletRefundCreditResultDto> CreditRefundAsync(
            Guid customerActorUserId,
            decimal amount,
            string currency,
            Guid returnRequestId,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            Credits.Add((customerActorUserId, amount, currency));
            return Task.FromResult(new WalletRefundCreditResultDto(amount, false));
        }
    }

    private sealed class RecordingInventory : IReturnInventoryGateway
    {
        public List<(Guid ReservationId, decimal Qty)> Restocks { get; } = [];

        public Task RestockConsumedReservationAsync(
            Guid reservationId,
            decimal quantity,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            Restocks.Add((reservationId, quantity));
            return Task.CompletedTask;
        }
    }
}
