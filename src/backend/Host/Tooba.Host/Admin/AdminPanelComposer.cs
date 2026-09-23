using MediatR;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Grid;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Grid;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Order.Application.Admin.Dashboard.Queries.GetAdminOrderDashboardMetrics;
using Tooba.Order.Application.Admin.Sellers.Ports;
using Tooba.Party.Infrastructure.Persistence;

namespace Tooba.Host.Admin;

/// <summary>
/// ترکیب باریک Host برای سطوح cross-module مدیر (داشبورد / فروشندگان).
/// مسیرهای Order-owned (orders list، customers) در Order.Endpoints هستند.
/// </summary>
public sealed class AdminPanelComposer
{
    private readonly CatalogDbContext _catalog;
    private readonly IOfferQueryGateway _offers;
    private readonly PartyDbContext _parties;
    private readonly ISender _sender;
    private readonly ISellerOrderCountReader _sellerOrderCounts;
    private readonly AdminSellersGridQueryEngine _sellersGrid;

    /// <summary>
    /// ترکیب‌گر Host را با contextهای غیر-Order و مرزهای Order Application می‌سازد.
    /// </summary>
    public AdminPanelComposer(
        CatalogDbContext catalog,
        IOfferQueryGateway offers,
        PartyDbContext parties,
        ISender sender,
        ISellerOrderCountReader sellerOrderCounts,
        AdminSellersGridQueryEngine sellersGrid)
    {
        _catalog = catalog;
        _offers = offers;
        _parties = parties;
        _sender = sender;
        _sellerOrderCounts = sellerOrderCounts;
        _sellersGrid = sellersGrid;
    }

    /// <summary>
    /// خلاصهٔ واقعی داشبورد را بدون نمودار یا درآمد ساختگی برمی‌گرداند.
    /// شمارنده‌های Order از CQRS Order می‌آیند؛ Catalog/Offer در Host ترکیب می‌شوند.
    /// </summary>
    public async Task<AdminDashboardSummary> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var publishedProducts = await _catalog.Products.AsNoTracking()
            .CountAsync(x => x.Status == CatalogPublicationStatus.Published, cancellationToken);
        var activeOffers = await _offers.CountActiveOffersAsync(cancellationToken);
        var sellerIds = await _offers.ListDistinctSellerPartyIdsAsync(cancellationToken);
        var orderMetrics = (await _sender.Send(new GetAdminOrderDashboardMetricsQuery(), cancellationToken)).Value;

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
    /// شمارش سفارش از مرز Order Application است.
    /// </summary>
    public async Task<IReadOnlyList<AdminSellerListItem>> ListSellersAsync(CancellationToken cancellationToken)
    {
        var offerRows = await _offers.ListSellerStatusRowsAsync(cancellationToken);
        var sellerIds = offerRows.Select(x => x.SellerPartyId).Distinct().ToList();
        var parties = await _parties.Parties.AsNoTracking()
            .Where(x => sellerIds.Contains(x.PartyId))
            .Select(x => new { x.PartyId, x.DisplayName, x.Status })
            .ToListAsync(cancellationToken);
        var orderMap = await _sellerOrderCounts.GetCountsBySellerAsync(sellerIds, cancellationToken);
        return parties.Select(party => new AdminSellerListItem(
            party.PartyId,
            party.DisplayName,
            party.Status.ToString(),
            offerRows.Count(x => x.SellerPartyId == party.PartyId && x.Status == OfferStatus.Active),
            orderMap.GetValueOrDefault(party.PartyId))).ToList();
    }

    /// <summary>صفحه‌بندی server-side گرید فروشندگان Admin (DB-native؛ Order metric از مرز Order).</summary>
    public Task<GridPageResponse<AdminSellerListItem>> QuerySellersGridAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = AdminListGridPolicies.Sellers.Normalize(request);
        return _sellersGrid.QueryAsync(q, cancellationToken);
    }
}
