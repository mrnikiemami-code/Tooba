using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Payment.Application.Models;
using Tooba.Payment.Application.Ports;
using Tooba.Payment.Contracts.Checkout;
using Tooba.Payment.Domain.Aggregates;
using Tooba.Payment.Domain.ValueObjects;
using Tooba.Payment.Infrastructure.Directories;
using Tooba.Payment.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Payment.Tests.Behavior;

/// <summary>
/// TB-SHIPPING-ONE-LEVEL-TABS-128 — نمای پرداخت چندگروهی: هر گروه ارسال پایدار سفارش یک تخصیص
/// StoreShippingGroup مستقل با هدف ShippingGroupId می‌گیرد، مجموع دقیقاً یک‌بار محاسبه می‌شود و
/// سفارش قدیمی تک‌روشی همان باکت sentinel قبلی را نگه می‌دارد.
/// </summary>
public sealed class PaymentShippingGroupAllocationTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-16T09:00:00Z");

    [Fact]
    public async Task One_seller_two_methods_creates_two_group_shipping_allocations()
    {
        await using var db = CreateDb();
        var checkoutId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var groupA = Guid.NewGuid();
        var groupB = Guid.NewGuid();
        var reader = new Reader(
            checkoutId,
            sellerOrderId,
            100_000m,
            [
                new PayableShippingGroupSnapshot(groupA, sellerOrderId, 25_000m, "IRR"),
                new PayableShippingGroupSnapshot(groupB, sellerOrderId, 10_000m, "IRR"),
            ]);

        var result = await CreateDirectory(db, new Registry(), reader).InitiateAsync(
            new InitiatePaymentCommand(checkoutId, Guid.NewGuid(), null, "key-128a", "fake"),
            CancellationToken.None);

        Assert.Equal(135_000m, result.Amount);
        var allocations = await db.Allocations.Where(x => x.PaymentId == result.PaymentId).ToListAsync();
        Assert.Equal(3, allocations.Count);
        var shipping = allocations
            .Where(x => x.TargetKind == PaymentAllocationTargetKind.StoreShippingGroup)
            .ToDictionary(x => x.SellerOrderId, x => x.AllocatedAmount);
        Assert.Equal(25_000m, shipping[groupA]);
        Assert.Equal(10_000m, shipping[groupB]);
        Assert.DoesNotContain(allocations, x => x.SellerOrderId == PaymentAllocation.StoreShippingTargetId);
    }

    [Fact]
    public async Task Two_sellers_same_method_creates_separate_seller_scoped_group_allocations()
    {
        await using var db = CreateDb();
        var checkoutId = Guid.NewGuid();
        var sellerA = Guid.NewGuid();
        var sellerB = Guid.NewGuid();
        var groupA = Guid.NewGuid();
        var groupB = Guid.NewGuid();
        var reader = new Reader(
            checkoutId,
            [(sellerA, 50_000m), (sellerB, 70_000m)],
            [
                new PayableShippingGroupSnapshot(groupA, sellerA, 10_000m, "IRR"),
                new PayableShippingGroupSnapshot(groupB, sellerB, 12_000m, "IRR"),
            ]);

        var result = await CreateDirectory(db, new Registry(), reader).InitiateAsync(
            new InitiatePaymentCommand(checkoutId, Guid.NewGuid(), null, "key-128b", "fake"),
            CancellationToken.None);

        Assert.Equal(142_000m, result.Amount);
        var allocations = await db.Allocations.Where(x => x.PaymentId == result.PaymentId).ToListAsync();
        Assert.Equal(22_000m, allocations
            .Where(x => x.TargetKind == PaymentAllocationTargetKind.StoreShippingGroup)
            .Sum(x => x.AllocatedAmount));
        Assert.Equal(
            10_000m,
            allocations.Single(x => x.TargetKind == PaymentAllocationTargetKind.StoreShippingGroup
                && x.SellerOrderId == groupA).AllocatedAmount);
        Assert.Equal(
            12_000m,
            allocations.Single(x => x.TargetKind == PaymentAllocationTargetKind.StoreShippingGroup
                && x.SellerOrderId == groupB).AllocatedAmount);
    }

    [Fact]
    public async Task Legacy_single_method_order_keeps_one_sentinel_store_shipping_allocation()
    {
        await using var db = CreateDb();
        var checkoutId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        // No ShippingGroups => the accepted order predates M2 and only carries the scalar amount.
        var reader = new Reader(checkoutId, sellerOrderId, 100_000m, null, shippingAmount: 8_000m);

        var result = await CreateDirectory(db, new Registry(), reader).InitiateAsync(
            new InitiatePaymentCommand(checkoutId, Guid.NewGuid(), null, "key-128c", "fake"),
            CancellationToken.None);

        Assert.Equal(108_000m, result.Amount);
        var allocations = await db.Allocations.Where(x => x.PaymentId == result.PaymentId).ToListAsync();
        var shipping = Assert.Single(
            allocations,
            x => x.TargetKind == PaymentAllocationTargetKind.StoreShipping);
        Assert.Equal(PaymentAllocation.StoreShippingTargetId, shipping.SellerOrderId);
        Assert.Equal(8_000m, shipping.AllocatedAmount);
        Assert.Equal(2, allocations.Count);
    }

    [Fact]
    public async Task Repeating_the_same_idempotency_key_does_not_add_shipping_allocations()
    {
        await using var db = CreateDb();
        var checkoutId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var groupA = Guid.NewGuid();
        var groupB = Guid.NewGuid();
        var reader = new Reader(
            checkoutId,
            sellerOrderId,
            100_000m,
            [
                new PayableShippingGroupSnapshot(groupA, sellerOrderId, 25_000m, "IRR"),
                new PayableShippingGroupSnapshot(groupB, sellerOrderId, 10_000m, "IRR"),
            ]);

        var directory = CreateDirectory(db, new Registry(), reader);
        var first = await directory.InitiateAsync(
            new InitiatePaymentCommand(checkoutId, Guid.NewGuid(), null, "key-128d", "fake"),
            CancellationToken.None);
        var second = await directory.InitiateAsync(
            new InitiatePaymentCommand(checkoutId, Guid.NewGuid(), null, "key-128d", "fake"),
            CancellationToken.None);

        Assert.Equal(first.PaymentId, second.PaymentId);
        Assert.Equal(3, await db.Allocations.CountAsync(x => x.PaymentId == first.PaymentId));
        Assert.Equal(1, await db.Payments.CountAsync(x => x.CheckoutId == checkoutId));
    }

    [Fact]
    public async Task Duplicate_group_identity_fails_closed_before_any_payment_row()
    {
        await using var db = CreateDb();
        var checkoutId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var group = Guid.NewGuid();
        var reader = new Reader(
            checkoutId,
            sellerOrderId,
            100_000m,
            [
                new PayableShippingGroupSnapshot(group, sellerOrderId, 10_000m, "IRR"),
                new PayableShippingGroupSnapshot(group, sellerOrderId, 10_000m, "IRR"),
            ]);

        var fault = await Assert.ThrowsAsync<ContractOperationException>(() =>
            CreateDirectory(db, new Registry(), reader).InitiateAsync(
                new InitiatePaymentCommand(checkoutId, Guid.NewGuid(), null, "key-128e", "fake"),
                CancellationToken.None));

        Assert.Equal("payment.shipping_group.duplicate", fault.Code);
        Assert.Equal(0, await db.Payments.CountAsync());
        Assert.Equal(0, await db.Allocations.CountAsync());
    }

    [Fact]
    public async Task Group_sum_mismatch_fails_closed_instead_of_double_charging_shipping()
    {
        await using var db = CreateDb();
        var checkoutId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var reader = new Reader(
            checkoutId,
            sellerOrderId,
            100_000m,
            [
                new PayableShippingGroupSnapshot(Guid.NewGuid(), sellerOrderId, 10_000m, "IRR"),
                new PayableShippingGroupSnapshot(Guid.NewGuid(), sellerOrderId, 10_000m, "IRR"),
            ],
            shippingAmount: 30_000m);

        var fault = await Assert.ThrowsAsync<ContractOperationException>(() =>
            CreateDirectory(db, new Registry(), reader).InitiateAsync(
                new InitiatePaymentCommand(checkoutId, Guid.NewGuid(), null, "key-128f", "fake"),
                CancellationToken.None));

        Assert.Equal("payment.shipping_group.sum_mismatch", fault.Code);
        Assert.Equal(0, await db.Payments.CountAsync());
    }

    [Fact]
    public async Task Empty_group_identity_fails_closed()
    {
        await using var db = CreateDb();
        var checkoutId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var reader = new Reader(
            checkoutId,
            sellerOrderId,
            100_000m,
            [new PayableShippingGroupSnapshot(Guid.Empty, sellerOrderId, 10_000m, "IRR")],
            shippingAmount: 10_000m);

        var fault = await Assert.ThrowsAsync<ContractOperationException>(() =>
            CreateDirectory(db, new Registry(), reader).InitiateAsync(
                new InitiatePaymentCommand(checkoutId, Guid.NewGuid(), null, "key-128g", "fake"),
                CancellationToken.None));

        Assert.Equal("payment.shipping_group.identity_required", fault.Code);
        Assert.Equal(0, await db.Payments.CountAsync());
    }

    [Fact]
    public void Group_allocations_are_never_mistaken_for_seller_merchandise()
    {
        var sellerOrderId = Guid.NewGuid();
        var allocation = PaymentAllocation.CreateStoreShippingGroup(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            5_000m,
            "IRR");

        Assert.False(allocation.IsSellerOrder);
        Assert.True(allocation.IsStoreShipping);
        Assert.NotEqual(sellerOrderId, allocation.SellerOrderId);
        Assert.Throws<ContractOperationException>(() => PaymentAllocation.CreateStoreShippingGroup(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentAllocation.StoreShippingTargetId,
            5_000m,
            "IRR"));
    }

    private static PaymentDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase("payment-shipping-group-128-" + Guid.NewGuid().ToString("N"))
            .Options);

    private static PaymentDirectory CreateDirectory(
        PaymentDbContext db,
        IPaymentGatewayRegistry registry,
        IPayableCheckoutReader reader) =>
        new(
            db,
            new OpenPaymentUseCaseGuard(),
            reader,
            registry,
            new PaymentGatewayActorContext(),
            new SystemUtcClock(),
            new UuidV7IdGenerator());

    private sealed class Reader : IPayableCheckoutReader
    {
        private readonly Guid _checkoutId;
        private readonly IReadOnlyList<PayableSellerOrderSnapshot> _sellerOrders;
        private readonly decimal _shippingAmount;
        private readonly IReadOnlyList<PayableShippingGroupSnapshot>? _groups;

        public Reader(
            Guid checkoutId,
            Guid sellerOrderId,
            decimal merchandise,
            IReadOnlyList<PayableShippingGroupSnapshot>? groups,
            decimal shippingAmount = 0m)
            : this(checkoutId, [(sellerOrderId, merchandise)], groups, shippingAmount)
        {
        }

        public Reader(
            Guid checkoutId,
            IReadOnlyList<(Guid SellerOrderId, decimal Amount)> sellerOrders,
            IReadOnlyList<PayableShippingGroupSnapshot>? groups,
            decimal shippingAmount = 0m)
        {
            _checkoutId = checkoutId;
            _sellerOrders = sellerOrders
                .Select(x => new PayableSellerOrderSnapshot(x.SellerOrderId, x.Amount, "IRR"))
                .ToArray();
            _groups = groups;
            _shippingAmount = shippingAmount == 0m && groups is { Count: > 0 }
                ? groups.Sum(x => x.Amount)
                : shippingAmount;
        }

        public Task<PayableCheckoutSnapshot?> GetPayableAsync(
            Guid checkoutId,
            Guid actorUserId,
            Guid? buyerPartyId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PayableCheckoutSnapshot?>(new PayableCheckoutSnapshot(
                _checkoutId,
                OrderPaymentMode.OnlinePurchase,
                "IRR",
                _sellerOrders,
                _shippingAmount,
                _groups));
    }

    private sealed class Registry : IPaymentGatewayRegistry
    {
        private readonly Gateway _gateway = new();

        public IPaymentGateway Resolve(string providerCode) =>
            string.Equals(providerCode.Trim(), "fake", StringComparison.OrdinalIgnoreCase)
                ? _gateway
                : throw new InvalidOperationException("payment.gateway.unknown");
    }

    private sealed class Gateway : IPaymentGateway
    {
        public string ProviderCode => "fake";

        public Task<GatewayInitiation> InitiateAsync(
            Guid paymentId,
            decimal amount,
            string currency,
            CancellationToken cancellationToken) =>
            Task.FromResult(new GatewayInitiation(
                "ref-" + paymentId.ToString("N"),
                "/payment/fake?ref=" + paymentId.ToString("N"),
                Now.AddMinutes(15)));

        public Task<GatewayVerification> VerifyAsync(
            string providerRequestReference,
            bool callbackClaimsSuccess,
            CancellationToken cancellationToken) =>
            Task.FromResult(new GatewayVerification(true, "txn-" + providerRequestReference, null));
    }
}
