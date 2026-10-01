using Tooba.BuildingBlocks.Grid;
using Tooba.Catalog.Contracts;
using Tooba.Offer.Contracts.Ports;
using Tooba.Order.Contracts.Admin;
using Tooba.Party.Contracts;

namespace Tooba.Host.Admin.Panel;

/// <summary>
/// ترکیب باریک Host برای سطوح cross-module مدیر (داشبورد / گرید فروشندگان).
/// GET /v1/admin/sellers به Party.Endpoints منتقل شده است.
/// تمام خواندن ماژول‌های کسب‌وکار فقط از طریق Contracts انجام می‌شود.
/// </summary>
public sealed class AdminPanelComposer
{
    private readonly ICatalogAdminProductCountGateway _catalogProducts;
    private readonly IOfferQueryGateway _offers;
    private readonly IAdminOrderDashboardMetricsPort _orderMetrics;
    private readonly IAdminSellersGridPort _sellersGrid;

    /// <summary>
    /// ترکیب‌گر Host را فقط با مرزهای Contracts ماژول‌ها می‌سازد.
    /// </summary>
    public AdminPanelComposer(
        ICatalogAdminProductCountGateway catalogProducts,
        IOfferQueryGateway offers,
        IAdminOrderDashboardMetricsPort orderMetrics,
        IAdminSellersGridPort sellersGrid)
    {
        _catalogProducts = catalogProducts;
        _offers = offers;
        _orderMetrics = orderMetrics;
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

    /// <summary>صفحه‌بندی server-side گرید فروشندگان Admin (مرز Contracts Party؛ Order metric از Contracts Order).</summary>
    public Task<GridPageResponse<AdminSellerListItem>> QuerySellersGridAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken) =>
        _sellersGrid.QueryAsync(request, cancellationToken);
}
