using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Order.Contracts.Settlement;
using Tooba.Payment.Contracts.Settlement;
using Tooba.Settlement.Application.Payouts.Ports;
using Tooba.Settlement.Contracts.Errors;
using Tooba.Settlement.Domain.ValueObjects;
using Tooba.Settlement.Infrastructure.Bridges;
using Tooba.Settlement.Infrastructure.Directories;
using Tooba.Settlement.Infrastructure.Observability;
using Tooba.Settlement.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Settlement.Tests.Behavior;

/// <summary>
/// M4 — حساب‌داری هزینهٔ ارسال per-group: فقط charge مشتری KNOWN ثبت می‌شود، سهم carrier/فروشنده/
/// فروشگاه UNSPECIFIED می‌ماند، هیچ ledger فروشنده یا payout ساخته نمی‌شود و ثبت idempotent است.
/// </summary>
public sealed class ShippingGroupFeeAllocationTests
{
    [Fact]
    public async Task Two_groups_of_one_seller_record_distinct_known_charges_without_any_seller_revenue()
    {
        await using var db = CreateDb();
        var paymentId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var sellerPartyId = Guid.NewGuid();
        var groupA = Guid.NewGuid();
        var groupB = Guid.NewGuid();
        var directory = CreateDirectory(
            db,
            new StaticShippingGroupReader(
                Fee(groupA, sellerOrderId, sellerPartyId, "post", 8_000m),
                Fee(groupB, sellerOrderId, sellerPartyId, "tipax", 5_000m)));

        await directory.RecordShippingGroupFeesAsync(paymentId, [sellerOrderId], CancellationToken.None);

        var rows = await db.ShippingGroupFees.OrderBy(x => x.CustomerChargeAmount).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.Equal([5_000m, 8_000m], rows.Select(x => x.CustomerChargeAmount).ToArray());
        Assert.Equal([groupB, groupA], rows.Select(x => x.ShippingGroupId).ToArray());
        Assert.All(rows, row =>
        {
            Assert.Equal("IRR", row.Currency);
            Assert.Equal(ShippingGroupFeeAllocationState.PendingAllocation, row.AllocationState);
            Assert.Equal(ShippingGroupFeeRecordKind.Charge, row.RecordKind);
            Assert.True(row.IsPendingAllocation);
            Assert.Null(row.AllocationPolicyVersion);
            Assert.Null(row.AllocationPolicyReference);
            Assert.Null(row.CarrierPayableAmount);
            Assert.Null(row.SellerShareAmount);
            Assert.Null(row.MarketplaceShareAmount);
            Assert.Null(row.ShippingDiscountAmount);
            Assert.Null(row.ShippingTaxAmount);
            Assert.Null(row.FunderAdjustmentAmount);
            Assert.Null(row.FunderReference);
        });
        Assert.Equal(2, rows.Select(x => x.IdempotencyKey).Distinct(StringComparer.Ordinal).Count());

        // No seller balance, no merchandise ledger row and no payout may be produced by a shipping fee.
        Assert.Empty(await db.SettlementAccounts.ToListAsync());
        Assert.Empty(await db.SettlementEntries.ToListAsync());
        Assert.Empty(await db.PayoutRequests.ToListAsync());
    }

    [Fact]
    public async Task Same_method_different_sellers_stay_separate_and_never_merge()
    {
        await using var db = CreateDb();
        var paymentId = Guid.NewGuid();
        var sellerOrderA = Guid.NewGuid();
        var sellerOrderB = Guid.NewGuid();
        var sellerA = Guid.NewGuid();
        var sellerB = Guid.NewGuid();
        var directory = CreateDirectory(
            db,
            new StaticShippingGroupReader(
                Fee(Guid.NewGuid(), sellerOrderA, sellerA, "post", 8_000m),
                Fee(Guid.NewGuid(), sellerOrderB, sellerB, "post", 3_000m)));

        await directory.RecordShippingGroupFeesAsync(paymentId, [sellerOrderA, sellerOrderB], CancellationToken.None);

        var rows = await db.ShippingGroupFees.ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.Equal(
            new[] { sellerA, sellerB }.OrderBy(x => x).ToArray(),
            rows.Select(x => x.SellerPartyId).OrderBy(x => x).ToArray());
        Assert.Equal(2, rows.Select(x => x.SellerOrderId).Distinct().Count());
        Assert.Equal(2, rows.Select(x => x.CheckoutId).Distinct().Count());
        Assert.Empty(await db.SettlementEntries.ToListAsync());
    }

    [Fact]
    public async Task Recording_and_reversing_are_idempotent_and_create_no_duplicate_row()
    {
        await using var db = CreateDb();
        var paymentId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var sellerPartyId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var directory = CreateDirectory(
            db,
            new StaticShippingGroupReader(Fee(groupId, sellerOrderId, sellerPartyId, "post", 8_000m)));

        await directory.RecordShippingGroupFeesAsync(paymentId, [sellerOrderId], CancellationToken.None);
        await directory.RecordShippingGroupFeesAsync(paymentId, [sellerOrderId], CancellationToken.None);
        await directory.ReverseShippingGroupFeeAsync(paymentId, groupId, CancellationToken.None);
        await directory.ReverseShippingGroupFeeAsync(paymentId, groupId, CancellationToken.None);

        var rows = await db.ShippingGroupFees.ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.Single(rows, x => x.RecordKind == ShippingGroupFeeRecordKind.Charge);
        Assert.Single(rows, x => x.RecordKind == ShippingGroupFeeRecordKind.Reversal);
        Assert.Empty(await db.SettlementEntries.ToListAsync());
    }

    [Fact]
    public async Task Legacy_ungrouped_order_keeps_the_store_shipping_sentinel_route_untouched()
    {
        await using var db = CreateDb();
        var directory = CreateDirectory(db, new StaticShippingGroupReader());

        await directory.RecordShippingGroupFeesAsync(Guid.NewGuid(), [Guid.NewGuid()], CancellationToken.None);

        Assert.Empty(await db.ShippingGroupFees.ToListAsync());
        Assert.Empty(await db.SettlementEntries.ToListAsync());
        Assert.Empty(await db.PaymentInbox.ToListAsync());
    }

    [Fact]
    public async Task Declared_charge_that_does_not_equal_the_paid_group_allocation_fails_closed()
    {
        await using var db = CreateDb();
        var paymentId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var sellerPartyId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var bridge = new SettlementShippingGroupBridge(
            new StaticOrderGroupReader(
                new OrderShippingGroupSettlementSnapshot(
                    groupId, Guid.NewGuid(), sellerOrderId, sellerPartyId, "post", null, null, "Confirmed", 8_000m, "IRR")),
            new StaticPaymentSettlementReader(
                new PaymentSettlementShippingGroupAllocationSnapshot(groupId, 12_000m, "IRR")));
        var directory = CreateDirectory(db, bridge);

        var error = await Assert.ThrowsAsync<ContractOperationException>(() =>
            directory.RecordShippingGroupFeesAsync(paymentId, [sellerOrderId], CancellationToken.None));

        Assert.Equal(SettlementErrorCodes.ShippingGroupChargeMismatch, error.Code);
        Assert.Empty(await db.ShippingGroupFees.ToListAsync());
        Assert.Empty(await db.SettlementEntries.ToListAsync());
    }

    [Fact]
    public async Task Group_without_any_paid_allocation_is_never_recorded_as_a_known_charge()
    {
        await using var db = CreateDb();
        var sellerOrderId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var bridge = new SettlementShippingGroupBridge(
            new StaticOrderGroupReader(
                new OrderShippingGroupSettlementSnapshot(
                    groupId, Guid.NewGuid(), sellerOrderId, Guid.NewGuid(), "post", null, null, "Confirmed", 8_000m, "IRR")),
            new StaticPaymentSettlementReader());
        var directory = CreateDirectory(db, bridge);

        var error = await Assert.ThrowsAsync<ContractOperationException>(() =>
            directory.RecordShippingGroupFeesAsync(Guid.NewGuid(), [sellerOrderId], CancellationToken.None));

        Assert.Equal(SettlementErrorCodes.ShippingGroupChargeMismatch, error.Code);
        Assert.Empty(await db.ShippingGroupFees.ToListAsync());
    }

    [Fact]
    public async Task Free_shipping_group_has_no_customer_fee_and_stays_out_of_settlement()
    {
        await using var db = CreateDb();
        var sellerOrderId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var bridge = new SettlementShippingGroupBridge(
            new StaticOrderGroupReader(
                new OrderShippingGroupSettlementSnapshot(
                    groupId, Guid.NewGuid(), sellerOrderId, Guid.NewGuid(), "post", null, null, "Confirmed", 0m, "IRR")),
            new StaticPaymentSettlementReader());
        var directory = CreateDirectory(db, bridge);

        await directory.RecordShippingGroupFeesAsync(Guid.NewGuid(), [sellerOrderId], CancellationToken.None);

        Assert.Empty(await db.ShippingGroupFees.ToListAsync());
        Assert.Empty(await db.SettlementEntries.ToListAsync());
    }

    [Fact]
    public async Task Pending_allocation_cannot_become_seller_revenue_or_a_payout()
    {
        await using var db = CreateDb();
        var paymentId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var sellerPartyId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var directory = CreateDirectory(
            db,
            new StaticShippingGroupReader(Fee(groupId, sellerOrderId, sellerPartyId, "post", 8_000m)));

        await directory.RecordShippingGroupFeesAsync(paymentId, [sellerOrderId], CancellationToken.None);
        var row = await db.ShippingGroupFees.SingleAsync();

        var payoutError = Assert.Throws<ContractOperationException>(row.EnsurePayoutAllocationAvailable);
        Assert.Equal(SettlementErrorCodes.ShippingGroupAllocationPolicyMissing, payoutError.Code);
        var revenueError = Assert.Throws<ContractOperationException>(row.EnsureNotSellerRevenue);
        Assert.Equal(SettlementErrorCodes.ShippingGroupNotSellerRevenue, revenueError.Code);

        Assert.Empty(await db.SettlementEntries.ToListAsync());
        Assert.Empty(await db.PayoutRequests.ToListAsync());
    }

    [Fact]
    public async Task Reversal_is_scoped_to_one_group_and_never_rewrites_the_original_row()
    {
        await using var db = CreateDb();
        var paymentId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var sellerPartyId = Guid.NewGuid();
        var groupA = Guid.NewGuid();
        var groupB = Guid.NewGuid();
        var directory = CreateDirectory(
            db,
            new StaticShippingGroupReader(
                Fee(groupA, sellerOrderId, sellerPartyId, "post", 8_000m),
                Fee(groupB, sellerOrderId, sellerPartyId, "tipax", 5_000m)));

        await directory.RecordShippingGroupFeesAsync(paymentId, [sellerOrderId], CancellationToken.None);
        await directory.ReverseShippingGroupFeeAsync(paymentId, groupA, CancellationToken.None);

        var rows = await db.ShippingGroupFees.ToListAsync();
        Assert.Equal(3, rows.Count);
        var original = rows.Single(x => x.ShippingGroupId == groupA && x.RecordKind == ShippingGroupFeeRecordKind.Charge);
        Assert.Equal(8_000m, original.CustomerChargeAmount);
        var reversal = rows.Single(x => x.RecordKind == ShippingGroupFeeRecordKind.Reversal);
        Assert.Equal(original.ShippingGroupFeeId, reversal.ReversalOfShippingGroupFeeId);
        Assert.Equal(groupA, reversal.ShippingGroupId);
        Assert.False(rows.Any(x => x.RecordKind == ShippingGroupFeeRecordKind.Reversal && x.ShippingGroupId == groupB));
        Assert.Empty(await db.SettlementEntries.ToListAsync());
    }

    private static SettlementShippingGroupFeeSnapshot Fee(
        Guid shippingGroupId,
        Guid sellerOrderId,
        Guid sellerPartyId,
        string methodCode,
        decimal charge) =>
        new(
            shippingGroupId,
            Guid.NewGuid(),
            sellerOrderId,
            sellerPartyId,
            methodCode,
            null,
            null,
            "Confirmed",
            charge,
            charge,
            "IRR");

    private static SettlementDirectory CreateDirectory(
        SettlementDbContext db,
        ISettlementShippingGroupReader shippingGroups) =>
        new(
            db,
            new OpenGuard(),
            new NullOrderReader(),
            new NullPaymentReader(),
            new NullReturnsReader(),
            shippingGroups,
            new NullPayoutGateway(),
            new SettlementInstrumentation(),
            new SystemUtcClock(),
            new UuidV7IdGenerator());

    private static SettlementDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<SettlementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new SettlementDbContext(options);
    }

    private sealed class StaticShippingGroupReader(params SettlementShippingGroupFeeSnapshot[] rows)
        : ISettlementShippingGroupReader
    {
        public Task<IReadOnlyList<SettlementShippingGroupFeeSnapshot>> GetForPaymentAsync(
            Guid paymentId,
            IReadOnlyList<Guid> sellerOrderIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SettlementShippingGroupFeeSnapshot>>(rows);
    }

    private sealed class StaticOrderGroupReader(params OrderShippingGroupSettlementSnapshot[] rows)
        : IOrderShippingGroupSettlementReader
    {
        public Task<IReadOnlyList<OrderShippingGroupSettlementSnapshot>> GetForSellerOrdersAsync(
            IReadOnlyList<Guid> sellerOrderIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<OrderShippingGroupSettlementSnapshot>>(rows);
    }

    private sealed class StaticPaymentSettlementReader(params PaymentSettlementShippingGroupAllocationSnapshot[] rows)
        : IPaymentSettlementReader
    {
        public Task<PaymentSettlementSnapshot?> GetAsync(Guid paymentId, CancellationToken cancellationToken) =>
            Task.FromResult<PaymentSettlementSnapshot?>(null);

        public Task<IReadOnlyList<PaymentSettlementAllocationSnapshot>> GetAllocationsAsync(
            Guid paymentId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PaymentSettlementAllocationSnapshot>>([]);

        public Task<IReadOnlyList<PaymentSettlementShippingGroupAllocationSnapshot>> GetShippingGroupAllocationsAsync(
            Guid paymentId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PaymentSettlementShippingGroupAllocationSnapshot>>(rows);
    }

    private sealed class OpenGuard : ISettlementUseCaseGuard
    {
        public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class NullOrderReader : ISettlementOrderReader
    {
        public Task<SettlementOrderSnapshot?> GetAsync(Guid sellerOrderId, CancellationToken cancellationToken) =>
            Task.FromResult<SettlementOrderSnapshot?>(null);
    }

    private sealed class NullPaymentReader : ISettlementPaymentReader
    {
        public Task<SettlementPaymentSnapshot?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken) =>
            Task.FromResult<SettlementPaymentSnapshot?>(null);

        public Task<IReadOnlyList<SettlementPaymentAllocationSnapshot>> GetAllocationsAsync(
            Guid paymentId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SettlementPaymentAllocationSnapshot>>([]);
    }

    private sealed class NullReturnsReader : ISettlementReturnsReader
    {
        public Task<SettlementRefundSnapshot?> GetAsync(Guid returnRequestId, CancellationToken cancellationToken) =>
            Task.FromResult<SettlementRefundSnapshot?>(null);
    }

    private sealed class NullPayoutGateway : IPayoutGateway
    {
        public string ProviderCode => "null";

        public Task<GatewayPayoutResult> PayoutAsync(
            Guid payoutRequestId,
            Guid sellerPartyId,
            decimal amount,
            string currency,
            string idempotencyKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(new GatewayPayoutResult(false, null, "not_used"));
    }
}
