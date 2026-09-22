using Tooba.Order.Application.Storefront.Services;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Grid;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Grid;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Order.Application.Admin.OrdersGrid;
using Tooba.Order.Application.Admin.OrdersGrid.Models;
using Tooba.Offer.Contracts.Ports;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Party.Infrastructure.Persistence;

namespace Tooba.Host.Admin;

/// <summary>
/// read model باریک مدیر را با پرس‌وجوی مستقل هر DbContext و ترکیب در حافظه می‌سازد.
/// جزئیات سفارش (GET /orders/{id}) در Order.Endpoints / Admin/Detail است.
/// </summary>
public sealed class AdminPanelComposer
{
    private readonly CatalogDbContext _catalog;
    private readonly IOfferQueryGateway _offers;
    private readonly OrderDbContext _orders;
    private readonly PartyDbContext _parties;
    private readonly Tooba.Returns.Contracts.Operations.IReturnAdminOperations _returnOperations;
    private readonly AdminSellersGridQueryEngine _sellersGrid;
    private readonly AdminCustomersGridQueryEngine _customersGrid;

    /// <summary>
    /// ترکیب‌گر Host را با contextهای مستقل ماژول‌ها می‌سازد.
    /// </summary>
    public AdminPanelComposer(
        CatalogDbContext catalog,
        IOfferQueryGateway offers,
        OrderDbContext orders,
        PartyDbContext parties,
        Tooba.Returns.Contracts.Operations.IReturnAdminOperations returnOperations)
    {
        _returnOperations = returnOperations;
        _catalog = catalog;
        _offers = offers;
        _orders = orders;
        _parties = parties;
        _sellersGrid = new AdminSellersGridQueryEngine(offers, parties, orders);
        _customersGrid = new AdminCustomersGridQueryEngine(orders);
    }

    /// <summary>
    /// خلاصهٔ واقعی داشبورد را بدون نمودار یا درآمد ساختگی برمی‌گرداند.
    /// </summary>
    public async Task<AdminDashboardSummary> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var publishedProducts = await _catalog.Products.AsNoTracking()
            .CountAsync(x => x.Status == CatalogPublicationStatus.Published, cancellationToken);
        var activeOffers = await _offers.CountActiveOffersAsync(cancellationToken);
        var statuses = await _orders.SellerOrders.AsNoTracking()
            .Select(x => x.Status)
            .ToListAsync(cancellationToken);
        var sellerIds = await _offers.ListDistinctSellerPartyIdsAsync(cancellationToken);
        var customers = await _orders.Checkouts.AsNoTracking()
            .Select(x => x.PlacedByUserId)
            .Distinct()
            .CountAsync(cancellationToken);
        var paid = statuses.Count(x => x == SellerOrderStatus.Paid);
        var pending = statuses.Count(x => x is SellerOrderStatus.PendingPayment or SellerOrderStatus.Submitted);
        var open = statuses.Count(x => x is not SellerOrderStatus.Paid and not SellerOrderStatus.Cancelled);
        return new AdminDashboardSummary(
            publishedProducts,
            activeOffers,
            open,
            paid,
            pending,
            sellerIds.Count,
            customers);
    }

    /// <summary>
    /// Checkoutها را به ردیف‌های سفارش مدیر با snapshot مشتری و مبلغ تبدیل می‌کند.
    /// </summary>
    public async Task<IReadOnlyList<AdminOrderListItem>> ListOrdersAsync(CancellationToken cancellationToken)
    {
        var groups = await LoadOrderGroupsAsync(cancellationToken);
        var sellerIds = groups.SelectMany(g => g.SellerOrders.Select(o => o.SellerPartyId)).Distinct().ToList();
        var sellerNames = await LoadSellerDisplayNamesAsync(sellerIds, cancellationToken);
        var items = new List<AdminOrderListItem>(groups.Count);
        foreach (var group in groups)
        {
            items.Add(await MapOrderListItemAsync(group, sellerNames, cancellationToken));
        }

        return items;
    }

    /// <summary>
    /// فروشندگان دارای Offer را با وضعیت Party و شمارنده‌های مستقل فهرست می‌کند.
    /// </summary>
    public async Task<IReadOnlyList<AdminSellerListItem>> ListSellersAsync(CancellationToken cancellationToken)
    {
        var offerRows = await _offers.ListSellerStatusRowsAsync(cancellationToken);
        var sellerIds = offerRows.Select(x => x.SellerPartyId).Distinct().ToList();
        var parties = await _parties.Parties.AsNoTracking()
            .Where(x => sellerIds.Contains(x.PartyId))
            .Select(x => new { x.PartyId, x.DisplayName, x.Status })
            .ToListAsync(cancellationToken);
        var orderCounts = await _orders.SellerOrders.AsNoTracking()
            .Where(x => sellerIds.Contains(x.SellerPartyId))
            .GroupBy(x => x.SellerPartyId)
            .Select(x => new { SellerPartyId = x.Key, Count = x.Count() })
            .ToListAsync(cancellationToken);
        var orderMap = orderCounts.ToDictionary(x => x.SellerPartyId, x => x.Count);
        return parties.Select(party => new AdminSellerListItem(
            party.PartyId,
            party.DisplayName,
            party.Status.ToString(),
            offerRows.Count(x => x.SellerPartyId == party.PartyId && x.Status == OfferStatus.Active),
            orderMap.GetValueOrDefault(party.PartyId))).ToList();
    }

    /// <summary>
    /// مشتریان را فقط از User ثبت‌کنندهٔ سفارش و آخرین snapshot گیرنده استخراج می‌کند؛ CRM اختراع نمی‌شود.
    /// </summary>
    public async Task<IReadOnlyList<AdminCustomerListItem>> ListCustomersAsync(CancellationToken cancellationToken)
    {
        var rows = await _orders.Checkouts.AsNoTracking()
            .Select(x => new { x.PlacedByUserId, x.RecipientName, x.RecipientFirstName, x.RecipientLastName, x.ContactMobile, x.SubmittedAt })
            .ToListAsync(cancellationToken);
        return rows.GroupBy(x => x.PlacedByUserId)
            .Select(group =>
            {
                var latest = group.OrderByDescending(x => x.SubmittedAt).First();
                return new AdminCustomerListItem(
                    group.Key,
                    StorefrontRecipientNames.DisplayOrFallback(latest.RecipientFirstName, latest.RecipientLastName, latest.RecipientName),
                    string.IsNullOrWhiteSpace(latest.ContactMobile) ? null : latest.ContactMobile,
                    group.Count(),
                    latest.SubmittedAt,
                    "Active");
            })
            .OrderByDescending(x => x.LastOrderAt)
            .ToList();
    }

    /// <summary>صفحه‌بندی server-side گرید فروشندگان Admin (DB-native).</summary>
    public Task<GridPageResponse<AdminSellerListItem>> QuerySellersGridAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = AdminListGridPolicies.Sellers.Normalize(request);
        return _sellersGrid.QueryAsync(q, cancellationToken);
    }

    /// <summary>صفحه‌بندی server-side گرید مشتریان Admin (DB-native).</summary>
    public Task<GridPageResponse<AdminCustomerListItem>> QueryCustomersGridAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var q = AdminListGridPolicies.Customers.Normalize(request);
        return _customersGrid.QueryAsync(q, cancellationToken);
    }

    private async Task<IReadOnlyList<CheckoutGroup>> LoadOrderGroupsAsync(CancellationToken cancellationToken) =>
        await _orders.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .OrderByDescending(x => x.SubmittedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

    private async Task<IReadOnlyDictionary<Guid, string>> LoadSellerDisplayNamesAsync(
        IReadOnlyCollection<Guid> sellerIds,
        CancellationToken cancellationToken)
    {
        if (sellerIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var sellerRows = await _parties.Parties.AsNoTracking()
            .Where(x => sellerIds.Contains(x.PartyId))
            .Select(x => new { x.PartyId, x.DisplayName })
            .ToListAsync(cancellationToken);
        return sellerRows.ToDictionary(x => x.PartyId, x => x.DisplayName);
    }

    private async Task<AdminOrderListItem> MapOrderListItemAsync(
        CheckoutGroup group,
        IReadOnlyDictionary<Guid, string> sellerNames,
        CancellationToken cancellationToken)
    {
        var sellerOrderIds = group.SellerOrders.Select(x => x.SellerOrderId).ToList();
        var returns = await _returnOperations.ListBySellerOrderIdsAsync(sellerOrderIds, cancellationToken);
        var returnsLookup = returns
            .GroupBy(x => x.SellerOrderId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<Tooba.Returns.Contracts.Operations.ReturnSnapshot>)g.ToList());
        return AdminOrdersGridProjection.MapOrderListItem(group, sellerNames, returnsLookup);
    }
}
