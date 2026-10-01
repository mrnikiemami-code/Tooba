using Tooba.Catalog.Contracts;
using Tooba.Offer.Contracts.Ports;
using Tooba.Order.Contracts.Admin;

namespace Tooba.Host.Admin.Panel;

/// <summary>
/// ترکیب باریک Host برای داشبورد Admin.
/// GET/POST sellers به Party.Endpoints منتقل شده‌اند.
/// تمام خواندن ماژول‌های کسب‌وکار فقط از طریق Contracts انجام می‌شود.
/// </summary>
public sealed class AdminPanelComposer
{
    private readonly ICatalogAdminProductCountGateway _catalogProducts;
    private readonly IOfferQueryGateway _offers;
    private readonly IAdminOrderDashboardMetricsPort _orderMetrics;

    /// <summary>
    /// ترکیب‌گر Host را فقط با مرزهای Contracts ماژول‌ها می‌سازد.
    /// </summary>
    public AdminPanelComposer(
        ICatalogAdminProductCountGateway catalogProducts,
        IOfferQueryGateway offers,
        IAdminOrderDashboardMetricsPort orderMetrics)
    {
        _catalogProducts = catalogProducts;
        _offers = offers;
        _orderMetrics = orderMetrics;
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
}
