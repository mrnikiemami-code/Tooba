using Tooba.Catalog.Contracts;
using Tooba.Fulfillment.Contracts.History;
using Tooba.Order.Application;
using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.PurchaseVerification;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;
using Tooba.Order.Application.Admin.Completeness.History;
using Tooba.Order.Domain;
using Tooba.Party.Contracts;
using Tooba.Payment.Contracts.Admin;
using Tooba.Returns.Contracts.History;
using Tooba.Settlement.Contracts.History;
using Xunit;

namespace Tooba.Order.Tests.Application;

/// <summary>ترکیب تاریچهٔ عملیاتی از منابع قراردادی و ترتیب قطعی آن.</summary>
public sealed class AdminOrderHistoryCompositionTests
{
    [Fact]
    public async Task Cancelled_order_emits_cancellation_and_inventory_release_kinds()
    {
        var order = OrderTestData.SellerOrder(1000m, 2m);
        order.Cancel();

        var kinds = (await ComposeAsync(OrderTestData.Submit(order))).Select(x => x.Kind).ToList();

        Assert.Contains("order_created", kinds);
        Assert.Contains("order_cancelled", kinds);
        Assert.Contains("inventory_released", kinds);
    }

    [Fact]
    public async Task Restored_order_emits_order_restored_kind()
    {
        var order = OrderTestData.SellerOrder(1000m, 2m);
        order.Cancel();
        order.RestoreFromCancellation(DateTimeOffset.UnixEpoch.AddDays(2));

        var kinds = (await ComposeAsync(OrderTestData.Submit(order))).Select(x => x.Kind).ToList();

        Assert.Contains("order_created", kinds);
        Assert.Contains("order_restored", kinds);
        Assert.DoesNotContain("order_cancelled", kinds);
    }

    [Fact]
    public async Task Drafts_sort_by_occurred_at_descending_then_kind_ordinal()
    {
        var group = OrderTestData.Checkout(status: SellerOrderStatus.Cancelled);

        var drafts = await ComposeAsync(group);

        for (var i = 1; i < drafts.Count; i++)
        {
            Assert.True(drafts[i - 1].OccurredAt >= drafts[i].OccurredAt);
            if (drafts[i - 1].OccurredAt == drafts[i].OccurredAt)
            {
                Assert.True(string.CompareOrdinal(drafts[i - 1].Kind, drafts[i].Kind) <= 0);
            }
        }
    }

    [Fact]
    public async Task Fulfillment_returns_and_settlement_contracts_all_contribute_kinds()
    {
        var order = OrderTestData.SellerOrder(1000m, 2m);
        var group = OrderTestData.Submit(order);
        var lineId = order.Lines[0].LineId;
        var fulfillment = new FakeFulfillmentHistory
        {
            Fulfillments =
            [
                new FulfillmentHistoryRecord(
                    Guid.NewGuid(),
                    order.SellerOrderId,
                    order.SellerPartyId,
                    FulfillmentHistoryStatuses.Delivered,
                    DateTimeOffset.UnixEpoch.AddHours(1),
                    DateTimeOffset.UnixEpoch.AddHours(2),
                    [new FulfillmentHistoryItem(lineId, 2m, 2m)],
                    [
                        new FulfillmentHistoryShipment(
                            Guid.NewGuid(),
                            FulfillmentHistoryStatuses.Delivered,
                            "پست",
                            "پست پیشتاز",
                            "TRK-2",
                            "TRK-1",
                            DateTimeOffset.UnixEpoch.AddHours(3),
                            DateTimeOffset.UnixEpoch.AddHours(4),
                            DateTimeOffset.UnixEpoch.AddHours(5),
                            [new FulfillmentHistoryShipmentLine(lineId, 2m)]),
                    ]),
            ],
            Packages =
            [
                new ConsolidatedPackageHistoryRecord(
                    Guid.NewGuid(),
                    "PKG-1",
                    Guid.NewGuid(),
                    DateTimeOffset.UnixEpoch.AddHours(6),
                    DateTimeOffset.UnixEpoch.AddHours(7),
                    DateTimeOffset.UnixEpoch.AddHours(8),
                    DateTimeOffset.UnixEpoch.AddHours(9),
                    [new ConsolidatedPackageHistoryMember(Guid.NewGuid(), order.SellerPartyId, DateTimeOffset.UnixEpoch.AddHours(6))]),
            ],
        };
        var returns = new FakeReturnHistory
        {
            Records =
            [
                new ReturnHistoryRecord(
                    Guid.NewGuid(),
                    order.SellerOrderId,
                    Guid.NewGuid(),
                    ReturnHistoryStatuses.Completed,
                    DateTimeOffset.UnixEpoch.AddHours(10),
                    DateTimeOffset.UnixEpoch.AddHours(11),
                    [new ReturnHistoryItem(lineId, 1m)],
                    [
                        new ReturnHistoryRefundAttempt(
                            ReturnHistoryRefundAttemptStatuses.Succeeded,
                            1000m,
                            "IRR",
                            DateTimeOffset.UnixEpoch.AddHours(11),
                            DateTimeOffset.UnixEpoch.AddHours(12)),
                    ]),
            ],
        };
        var settlement = new FakeSettlementHistory
        {
            Entries =
            [
                new SettlementHistoryEntry(
                    order.SellerOrderId,
                    "order_paid",
                    SettlementHistoryEntryTypes.Credit,
                    DateTimeOffset.UnixEpoch.AddHours(13)),
                new SettlementHistoryEntry(
                    order.SellerOrderId,
                    SettlementHistorySourceTypes.Refund,
                    SettlementHistoryEntryTypes.Debit,
                    DateTimeOffset.UnixEpoch.AddHours(14)),
            ],
        };

        var kinds = (await ComposeAsync(group, fulfillment, returns, settlement)).Select(x => x.Kind).ToList();

        Assert.Contains("fulfillment_processing", kinds);
        Assert.Contains("fulfillment_packed", kinds);
        Assert.Contains("shipment_created", kinds);
        Assert.Contains("tracking_assigned", kinds);
        Assert.Contains("tracking_corrected", kinds);
        Assert.Contains("shipment_dispatched", kinds);
        Assert.Contains("shipment_delivered", kinds);
        Assert.Contains("consolidated_package_created", kinds);
        Assert.Contains("consolidated_package_member_added", kinds);
        Assert.Contains("consolidated_package_cancelled", kinds);
        Assert.Contains("consolidated_package_members_released", kinds);
        Assert.Contains("consolidated_package_dispatched", kinds);
        Assert.Contains("consolidated_package_delivered", kinds);
        Assert.Contains("return_requested", kinds);
        Assert.Contains("return_approved", kinds);
        Assert.Contains("refund_completed", kinds);
        Assert.Contains("settlement_accrual", kinds);
        Assert.Contains("settlement_adjustment", kinds);
    }

    [Fact]
    public async Task Payment_status_maps_to_one_payment_kind_plus_creation()
    {
        var group = OrderTestData.Checkout();
        var payment = new PaymentAdminOperationalSnapshot(
            Guid.NewGuid(),
            group.CheckoutId,
            "Succeeded",
            2000m,
            "IRR",
            "wallet",
            null,
            "TX-1",
            DateTimeOffset.UnixEpoch.AddMinutes(1),
            DateTimeOffset.UnixEpoch.AddMinutes(2),
            DateTimeOffset.UnixEpoch.AddMinutes(3),
            null,
            false,
            false,
            false);

        var kinds = (await ComposeAsync(group, payment: payment)).Select(x => x.Kind).ToList();

        Assert.Contains("payment_created", kinds);
        Assert.Contains("payment_succeeded", kinds);
        Assert.DoesNotContain("payment_failed", kinds);
    }

    [Fact]
    public async Task Inventory_recovery_notes_map_to_dedicated_kinds()
    {
        var group = OrderTestData.Checkout();
        var notes = new List<CheckoutOperationalNoteSnapshot>
        {
            Note(group.CheckoutId, $"{AdminOrderInventoryRecoveryNotePrefixes.Requested} admin", 20),
            Note(group.CheckoutId, $"{AdminOrderInventoryRecoveryNotePrefixes.Succeeded} class=B", 21),
            Note(group.CheckoutId, $"{AdminOrderInventoryRecoveryNotePrefixes.Failed} class=A", 22),
            Note(group.CheckoutId, $"{AdminOrderInventoryRecoveryNotePrefixes.Manual} ambiguous", 23),
            Note(group.CheckoutId, "یادداشت ساده", 24),
        };

        var kinds = (await ComposeAsync(group, notes: notes)).Select(x => x.Kind).ToList();

        Assert.Contains("inventory_recovery_requested", kinds);
        Assert.Contains("inventory_recovery_succeeded", kinds);
        Assert.Contains("inventory_recovery_failed_insufficient", kinds);
        Assert.Contains("inventory_recovery_manual_review", kinds);
        Assert.Contains("operational_note", kinds);
    }

    private static CheckoutOperationalNoteSnapshot Note(Guid checkoutId, string body, int hour) =>
        new(Guid.NewGuid(), checkoutId, body, Guid.Empty, DateTimeOffset.UnixEpoch.AddHours(hour), false);

    private static Task<IReadOnlyList<AdminOrderHistoryDraft>> ComposeAsync(
        CheckoutGroup group,
        IFulfillmentHistoryReader? fulfillment = null,
        IReturnHistoryReader? returns = null,
        ISettlementHistoryReader? settlement = null,
        PaymentAdminOperationalSnapshot? payment = null,
        IReadOnlyList<CheckoutOperationalNoteSnapshot>? notes = null) =>
        AdminOrderHistoryComposer.ComposeAsync(
            group,
            notes ?? [],
            payment,
            fulfillment ?? new FakeFulfillmentHistory(),
            returns ?? new FakeReturnHistory(),
            settlement ?? new FakeSettlementHistory(),
            new FakePartyLookup(),
            new FakeCatalogVariantLookup(),
            CancellationToken.None);

    private sealed class FakeFulfillmentHistory : IFulfillmentHistoryReader
    {
        public IReadOnlyList<FulfillmentHistoryRecord> Fulfillments { get; set; } = [];

        public IReadOnlyList<ConsolidatedPackageHistoryRecord> Packages { get; set; } = [];

        public Task<IReadOnlyList<FulfillmentHistoryRecord>> ListFulfillmentsForCheckoutAsync(
            Guid checkoutId,
            CancellationToken cancellationToken) => Task.FromResult(Fulfillments);

        public Task<IReadOnlyList<ConsolidatedPackageHistoryRecord>> ListConsolidatedPackagesForCheckoutAsync(
            Guid checkoutId,
            CancellationToken cancellationToken) => Task.FromResult(Packages);
    }

    private sealed class FakeReturnHistory : IReturnHistoryReader
    {
        public IReadOnlyList<ReturnHistoryRecord> Records { get; set; } = [];

        public Task<IReadOnlyList<ReturnHistoryRecord>> ListBySellerOrderIdsAsync(
            IReadOnlyList<Guid> sellerOrderIds,
            CancellationToken cancellationToken) => Task.FromResult(Records);
    }

    private sealed class FakeSettlementHistory : ISettlementHistoryReader
    {
        public IReadOnlyList<SettlementHistoryEntry> Entries { get; set; } = [];

        public Task<IReadOnlyList<SettlementHistoryEntry>> ListBySellerOrderIdsAsync(
            IReadOnlyList<Guid> sellerOrderIds,
            CancellationToken cancellationToken) => Task.FromResult(Entries);
    }

    private sealed class FakePartyLookup : IPartyLookup
    {
        public Task<PartyLookupResult?> FindByIdAsync(Guid partyId, CancellationToken cancellationToken) =>
            Task.FromResult<PartyLookupResult?>(new PartyLookupResult(partyId, "Organization", "فروشگاه آلفا"));

        public Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
            IReadOnlyList<Guid> partyIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(
                partyIds.ToDictionary(x => x, _ => "فروشگاه آلفا"));

        public Task<IReadOnlyList<Guid>> SearchIdsByDisplayNameAsync(
            string term,
            int take,
            CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<IReadOnlyList<Guid>> FilterIdsByDisplayNameAsync(
            string? op,
            string? value,
            IReadOnlyList<string>? values,
            int take,
            CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Guid>>([]);
    }

    private sealed class FakeCatalogVariantLookup : ICatalogVariantLookup
    {
        public Task<CatalogVariantLookupResult?> FindVariantAsync(Guid variantId, CancellationToken cancellationToken) =>
            Task.FromResult<CatalogVariantLookupResult?>(new CatalogVariantLookupResult(variantId, Guid.NewGuid()));

        public Task<IReadOnlyDictionary<Guid, Guid?>> GetPrimaryCategoryIdsByVariantIdsAsync(
            IReadOnlyList<Guid> variantIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, Guid?>>(new Dictionary<Guid, Guid?>());

        public Task<IReadOnlyDictionary<Guid, string>> GetVariantTitlesAsync(
            IReadOnlyList<Guid> variantIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string>>(
                variantIds.ToDictionary(x => x, _ => "کالای آزمایشی"));
    }
}
