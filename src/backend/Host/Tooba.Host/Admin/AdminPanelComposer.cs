using Tooba.BuildingBlocks.Grid;
using Tooba.Catalog.Contracts;
using Tooba.Host.Grid;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Order.Contracts.Admin;
using Tooba.Party.Contracts;

namespace Tooba.Host.Admin;

/// <summary>
/// ترکیب باریک Host برای سطوح cross-module مدیر (داشبورد / فروشندگان).
/// مسیرهای Order-owned (orders list، customers) در Order.Endpoints هستند.
/// تمام خواندن ماژول‌های کسب‌وکار فقط از طریق Contracts انجام می‌شود.
/// </summary>
public sealed class AdminPanelComposer
{
    private readonly ICatalogAdminProductCountGateway _catalogProducts;
    private readonly IOfferQueryGateway _offers;
    private readonly IPartyAdminSellerReadGateway _parties;
    private readonly IAdminOrderDashboardMetricsPort _orderMetrics;
    private readonly IAdminSellerOrderCountPort _sellerOrderCounts;
    private readonly AdminSellersGridQueryEngine _sellersGrid;

    /// <summary>
    /// ترکیب‌گر Host را فقط با مرزهای Contracts ماژول‌ها می‌سازد.
    /// </summary>
    public AdminPanelComposer(
        ICatalogAdminProductCountGateway catalogProducts,
        IOfferQueryGateway offers,
        IPartyAdminSellerReadGateway parties,
        IAdminOrderDashboardMetricsPort orderMetrics,
        IAdminSellerOrderCountPort sellerOrderCounts,
        AdminSellersGridQueryEngine sellersGrid)
    {
        _catalogProducts = catalogProducts;
        _offers = offers;
        _parties = parties;
        _orderMetrics = orderMetrics;
        _sellerOrderCounts = sellerOrderCounts;
        _sellersGrid = sellersGrid;
    }

    /// <summary>
    /// خلاصهٔ واقعی داشبورد را بدون نمودار یا درآمد ساختگی برمی‌گرداند.
    /// شمارنده‌های Order از مرز Contracts Order می‌آیند؛ Catalog/Offer در Host ترکیب می‌شوند.
    /// </summary>
    public async Task<AdminDashboardSummary> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var publishedProducts = await _catalogProducts.CountPublishedProductsAsync(cancellationToken);
        var activeOffers = await _offers.CountActiveOffersAsync(cancellationToken);
        var sellerIds = await _offers.ListDistinctSellerPartyIdsAsync(cancellationToken);
        var orderMetrics = await _orderMetrics.GetMetricsAsync(cancellationToken);

        return new AdminDashboardSummary(
            publishedProducts,
            activeOffers,
            orderMetrics.OpenOrders,
            orderMetrics.PaidOrders,
            orderMetrics.PendingOrders,
            sellerIds.Count,
            orderMetrics.Customers);
    }

    /// <summary>
    /// فروشندگان دارای Offer را با وضعیت Party و شمارنده‌های مستقل فهرست می‌کند.
    /// وضعیت Party از مرز Contracts Party و شمارش سفارش از مرز Contracts Order است.
    /// </summary>
    public async Task<IReadOnlyList<AdminSellerListItem>> ListSellersAsync(CancellationToken cancellationToken)
    {
        var offerRows = await _offers.ListSellerStatusRowsAsync(cancellationToken);
        var sellerIds = offerRows.Select(x => x.SellerPartyId).Distinct().ToList();
        var parties = await _parties.GetStatusProjectionsAsync(sellerIds, cancellationToken);
        var orderMap = await _sellerOrderCounts.GetCountsBySellerAsync(sellerIds, cancellationToken);
        return parties.Select(party => new AdminSellerListItem(
            party.PartyId,
            party.DisplayName,
            party.Status,
            offerRows.Count(x => x.SellerPartyId == party.PartyId && x.Status == OfferStatus.Active),
            orderMap.GetValueOrDefault(party.PartyId))).ToList();
    }

    /// <summary>صفحه‌بندی server-side گرید فروشندگان Admin (مرز Contracts Party؛ Order metric از Contracts Order).</summary>
    public Task<GridPageResponse<AdminSellerListItem>> QuerySellersGridAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = AdminListGridPolicies.Sellers.Normalize(request);
        return _sellersGrid.QueryAsync(q, cancellationToken);
    }
}
