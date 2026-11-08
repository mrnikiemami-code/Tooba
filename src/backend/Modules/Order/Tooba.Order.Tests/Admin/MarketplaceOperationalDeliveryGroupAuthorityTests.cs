using Tooba.Order.Contracts.Shipping;
using Xunit;

namespace Tooba.Order.Tests.Admin;

/// <summary>TB-253 authority matrix for Marketplace operational delivery grouping.</summary>
public sealed class MarketplaceOperationalDeliveryGroupAuthorityTests
{
    private static readonly Guid Checkout = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid SellerA = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
    private static readonly Guid SellerB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");
    private static readonly Guid OrderA = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1");
    private static readonly Guid OrderB = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc2");
    private static readonly Guid GroupA = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd1");
    private static readonly Guid GroupB = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd2");
    private static readonly Guid GroupC = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd3");
    private static readonly Guid LineA = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee1");
    private static readonly Guid LineB = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee2");
    private static readonly DateOnly Day = new(2026, 11, 10);
    private const long Open = 14L * TimeSpan.TicksPerHour;
    private const long Close = 15L * TimeSpan.TicksPerHour;
    private const string Dest = "تهران\u001fتهران\u001f1234567890\u001fخیابان نمونه";

    [Fact]
    public void Two_marketplace_sellers_same_origin_service_day_window_merge()
    {
        var projected = MarketplaceOperationalDeliveryGroupAuthority.Project(
        [
            Marketplace(GroupA, SellerA, OrderA, LineA, origin: "WH-1"),
            Marketplace(GroupB, SellerB, OrderB, LineB, origin: "WH-1"),
        ]);

        Assert.Single(projected);
        Assert.True(projected[0].IsCrossSellerMarketplace);
        Assert.Equal(2, projected[0].Members.Count);
        Assert.True(projected[0].ConsolidateEligible);
    }

    [Fact]
    public void Seller_responsibility_stays_separate_and_consolidate_refused()
    {
        var projected = MarketplaceOperationalDeliveryGroupAuthority.Project(
        [
            SellerOwned(GroupA, SellerA, OrderA, LineA),
            SellerOwned(GroupB, SellerB, OrderB, LineB),
        ]);

        Assert.Equal(2, projected.Count);
        Assert.All(projected, g => Assert.False(g.IsCrossSellerMarketplace));
        Assert.Throws<MarketplaceOperationalDeliveryGroupException>(() =>
            MarketplaceOperationalDeliveryGroupAuthority.EnsureConsolidateAllowed(
            [
                SellerOwned(GroupA, SellerA, OrderA, LineA),
                SellerOwned(GroupB, SellerB, OrderB, LineB),
            ]));
    }

    [Fact]
    public void Different_origin_method_day_or_hour_stay_separate()
    {
        Assert.Equal(2, MarketplaceOperationalDeliveryGroupAuthority.Project(
        [
            Marketplace(GroupA, SellerA, OrderA, LineA, origin: "WH-1"),
            Marketplace(GroupB, SellerB, OrderB, LineB, origin: "WH-2"),
        ]).Count);

        Assert.Equal(2, MarketplaceOperationalDeliveryGroupAuthority.Project(
        [
            Marketplace(GroupA, SellerA, OrderA, LineA, origin: "WH-1", method: "post"),
            Marketplace(GroupB, SellerB, OrderB, LineB, origin: "WH-1", method: "heavy"),
        ]).Count);

        Assert.Equal(2, MarketplaceOperationalDeliveryGroupAuthority.Project(
        [
            Marketplace(GroupA, SellerA, OrderA, LineA, origin: "WH-1", day: Day),
            Marketplace(GroupB, SellerB, OrderB, LineB, origin: "WH-1", day: Day.AddDays(1)),
        ]).Count);

        Assert.Equal(2, MarketplaceOperationalDeliveryGroupAuthority.Project(
        [
            Marketplace(GroupA, SellerA, OrderA, LineA, origin: "WH-1", open: Open),
            Marketplace(GroupB, SellerB, OrderB, LineB, origin: "WH-1", open: Open + TimeSpan.TicksPerHour),
        ]).Count);
    }

    [Fact]
    public void Missing_origin_keeps_groups_separate_with_owned_reason()
    {
        var projected = MarketplaceOperationalDeliveryGroupAuthority.Project(
        [
            Marketplace(GroupA, SellerA, OrderA, LineA, origin: null),
            Marketplace(GroupB, SellerB, OrderB, LineB, origin: null),
        ]);

        Assert.Equal(2, projected.Count);
        Assert.Contains(projected, g =>
            g.SeparateReasonCode == MarketplaceOperationalDeliveryGroupAuthority.SeparateMissingOrigin);
    }

    [Fact]
    public void Store_owner_is_not_auto_promoted_to_marketplace()
    {
        var projected = MarketplaceOperationalDeliveryGroupAuthority.Project(
        [
            StoreOwned(GroupA, SellerA, OrderA, LineA, origin: "WH-1"),
            StoreOwned(GroupB, SellerB, OrderB, LineB, origin: "WH-1"),
        ]);

        Assert.Equal(2, projected.Count);
        Assert.All(projected, g => Assert.False(g.IsCrossSellerMarketplace));
        Assert.All(projected, g =>
            Assert.Equal(MarketplaceOperationalDeliveryGroupAuthority.SeparateStoreResponsibility, g.SeparateReasonCode));
    }

    [Fact]
    public void Same_seller_different_commitments_remain_split()
    {
        var projected = MarketplaceOperationalDeliveryGroupAuthority.Project(
        [
            Marketplace(GroupA, SellerA, OrderA, LineA, origin: "WH-1", method: "post", day: Day),
            Marketplace(GroupC, SellerA, OrderA, LineB, origin: "WH-1", method: "post", day: Day.AddDays(2)),
        ]);

        Assert.Equal(2, projected.Count);
    }

    [Fact]
    public void Empty_origin_values_never_compare_equal()
    {
        Assert.False(MarketplaceOperationalDeliveryGroupAuthority.AreMarketplaceCompatible(
            Marketplace(GroupA, SellerA, OrderA, LineA, origin: "   "),
            Marketplace(GroupB, SellerB, OrderB, LineB, origin: "   "),
            out var code));
        Assert.Equal(MarketplaceOperationalDeliveryGroupAuthority.SeparateMissingOrigin, code);
    }

    [Fact]
    public void CarrierApi_cross_seller_consolidate_is_refused()
    {
        var a = Marketplace(GroupA, SellerA, OrderA, LineA, origin: "WH-1", transport: "CarrierApi", account: Guid.NewGuid());
        var b = Marketplace(GroupB, SellerB, OrderB, LineB, origin: "WH-1", transport: "CarrierApi", account: a.CarrierAccountId);
        var projected = MarketplaceOperationalDeliveryGroupAuthority.Project([a, b]);
        Assert.Single(projected);
        Assert.False(projected[0].ConsolidateEligible);
        Assert.Throws<MarketplaceOperationalDeliveryGroupException>(() =>
            MarketplaceOperationalDeliveryGroupAuthority.EnsureConsolidateAllowed([a, b]));
    }

    private static MarketplaceOperationalDeliveryGroupAuthority.GroupFact Marketplace(
        Guid groupId,
        Guid sellerPartyId,
        Guid sellerOrderId,
        Guid lineId,
        string? origin,
        string method = "post",
        DateOnly? day = null,
        long? open = null,
        string transport = "Manual",
        Guid? account = null) =>
        MakeFact(
            groupId,
            sellerPartyId,
            sellerOrderId,
            lineId,
            MarketplaceOperationalDeliveryGroupAuthority.OwnerKindMarketplace,
            method,
            day ?? Day,
            open ?? Open,
            transport,
            origin,
            ownerTenant: null,
            ownerSeller: null,
            account: account);

    private static MarketplaceOperationalDeliveryGroupAuthority.GroupFact SellerOwned(
        Guid groupId,
        Guid sellerPartyId,
        Guid sellerOrderId,
        Guid lineId) =>
        MakeFact(
            groupId,
            sellerPartyId,
            sellerOrderId,
            lineId,
            MarketplaceOperationalDeliveryGroupAuthority.OwnerKindSeller,
            "post",
            Day,
            Open,
            "Manual",
            "WH-1",
            ownerTenant: null,
            ownerSeller: sellerPartyId);

    private static MarketplaceOperationalDeliveryGroupAuthority.GroupFact StoreOwned(
        Guid groupId,
        Guid sellerPartyId,
        Guid sellerOrderId,
        Guid lineId,
        string origin) =>
        MakeFact(
            groupId,
            sellerPartyId,
            sellerOrderId,
            lineId,
            MarketplaceOperationalDeliveryGroupAuthority.OwnerKindStore,
            "post",
            Day,
            Open,
            "Manual",
            origin,
            ownerTenant: "store-alpha",
            ownerSeller: null);

    private static MarketplaceOperationalDeliveryGroupAuthority.GroupFact MakeFact(
        Guid groupId,
        Guid sellerPartyId,
        Guid sellerOrderId,
        Guid lineId,
        int ownerKind,
        string method,
        DateOnly day,
        long open,
        string transport,
        string? origin,
        string? ownerTenant,
        Guid? ownerSeller,
        Guid? account = null) =>
        new(
            groupId,
            Checkout,
            sellerOrderId,
            sellerPartyId,
            method,
            null,
            null,
            method == "post" ? "پیشتاز" : method,
            "IRR",
            125000m,
            ownerKind,
            ownerTenant,
            ownerSeller,
            day,
            open,
            Close,
            "Asia/Tehran",
            transport,
            origin,
            null,
            account,
            account is null ? null : "tipax",
            account is null ? null : "standard",
            account is null ? null : 1,
            Dest,
            [lineId],
            true,
            null);
}
