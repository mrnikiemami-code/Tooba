using Microsoft.EntityFrameworkCore;
using Tooba.AccessControl.Application;
using Tooba.AccessControl.Domain;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Party.Application;

namespace Tooba.Host.Seller;

/// <summary>Composes the remaining non-Offer seller dashboard and order views.</summary>
public sealed class SellerPanelComposer(
    CatalogDbContext catalog,
    OrderDbContext orders,
    IPartyLookupGateway parties,
    IAccessControlDirectory access,
    ICatalogLookupGateway catalogLookup)
{
    /// <summary>Builds the seller dashboard from Party and Order owner boundaries.</summary>
    public async Task<SellerDashboardSummary> GetDashboardAsync(
        Guid sellerPartyId, Guid actorUserId, CancellationToken cancellationToken)
    {
        var seller = await parties.FindByIdAsync(sellerPartyId, cancellationToken)
            ?? throw new PlatformHttpException(404, "Seller was not found.", "seller.missing");
        var rows = await orders.SellerOrders.AsNoTracking().Include(x => x.Lines)
            .Where(x => x.SellerPartyId == sellerPartyId).ToListAsync(cancellationToken);
        var scope = await ResolveOrderViewScopeAsync(sellerPartyId, actorUserId, cancellationToken);
        var visible = await FilterOrdersByScopeAsync(rows, scope, cancellationToken);
        var open = visible.Count(x => x.Status is SellerOrderStatus.Submitted
            or SellerOrderStatus.PendingPayment or SellerOrderStatus.ReservationRequested);
        return new SellerDashboardSummary(
            sellerPartyId, seller.DisplayName, 0, open,
            visible.Count(x => x.Status == SellerOrderStatus.Paid));
    }

    /// <summary>Lists published Catalog variants available for seller selection.</summary>
    public async Task<IReadOnlyList<SellerCatalogVariantOption>> ListCatalogVariantsAsync(
        Guid sellerPartyId, CancellationToken cancellationToken)
    {
        await EnsureSellerAsync(sellerPartyId, cancellationToken);
        var products = await catalog.Products.AsNoTracking()
            .Where(x => x.Status == CatalogPublicationStatus.Published)
            .OrderByDescending(x => x.UpdatedAt).Take(100).ToListAsync(cancellationToken);
        var ids = products.Select(x => x.ProductId).ToArray();
        var names = await catalog.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Product
                        && ids.Contains(x.OwnerId) && x.FieldKey == "name")
            .ToListAsync(cancellationToken);
        var nameMap = names.GroupBy(x => x.OwnerId).ToDictionary(
            x => x.Key, x => x.OrderBy(y => y.Locale.StartsWith("fa") ? 0 : 1).First().Value);
        var variants = await catalog.Variants.AsNoTracking()
            .Where(x => ids.Contains(x.ProductId)).OrderBy(x => x.CatalogCodeSeam).ToListAsync(cancellationToken);
        var statuses = products.ToDictionary(x => x.ProductId, x => x.Status.ToString());
        return variants.Select(x => new SellerCatalogVariantOption(
            x.VariantId, x.ProductId, nameMap.GetValueOrDefault(x.ProductId) ?? string.Empty,
            x.CatalogCodeSeam, statuses.GetValueOrDefault(x.ProductId) ?? "Published")).ToArray();
    }

    /// <summary>Lists seller orders permitted by the actor's order scope.</summary>
    public async Task<IReadOnlyList<SellerOrderListItem>> ListOrdersAsync(
        Guid sellerPartyId, Guid actorUserId, CancellationToken cancellationToken)
    {
        await EnsureSellerAsync(sellerPartyId, cancellationToken);
        var scope = await ResolveOrderViewScopeAsync(sellerPartyId, actorUserId, cancellationToken);
        if (scope.Denied) return [];
        var rows = await orders.SellerOrders.AsNoTracking().Include(x => x.Lines)
            .Where(x => x.SellerPartyId == sellerPartyId)
            .OrderByDescending(x => x.SellerOrderId).Take(200).ToListAsync(cancellationToken);
        var resolved = scope.Global
            ? new Dictionary<Guid, Guid?>()
            : await ResolveLineCategoriesAsync(rows.SelectMany(x => x.Lines).ToList(), cancellationToken);
        var visible = scope.Global ? rows : rows.Where(order =>
            order.Lines.Any(line => IsAllowed(line, scope.AllowedCategoryIds, resolved))).ToList();
        var checkoutIds = visible.Select(x => x.CheckoutId).Distinct().ToArray();
        var checkouts = await orders.Checkouts.AsNoTracking()
            .Where(x => checkoutIds.Contains(x.CheckoutId)).ToDictionaryAsync(x => x.CheckoutId, cancellationToken);
        return visible.Select(order =>
        {
            checkouts.TryGetValue(order.CheckoutId, out var checkout);
            var count = scope.Global ? order.Lines.Count : order.Lines.Count(x => IsAllowed(x, scope.AllowedCategoryIds, resolved));
            return new SellerOrderListItem(
                order.SellerOrderId, order.OrderNumber, checkout?.SubmittedAt ?? default,
                checkout?.RecipientName ?? string.Empty, count, order.GrandTotalSnapshot,
                order.Currency, order.Status.ToString(), order.Status.ToString());
        }).ToArray();
    }

    /// <summary>Gets a seller order permitted by the actor's order scope.</summary>
    public async Task<SellerOrderDetailPage?> GetOrderAsync(
        Guid sellerPartyId, Guid actorUserId, Guid sellerOrderId, CancellationToken cancellationToken)
    {
        await EnsureSellerAsync(sellerPartyId, cancellationToken);
        var scope = await ResolveOrderViewScopeAsync(sellerPartyId, actorUserId, cancellationToken);
        if (scope.Denied) throw new PlatformHttpException(403, "Order access denied.", "seller.order.view.denied");
        var order = await orders.SellerOrders.AsNoTracking().Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.SellerOrderId == sellerOrderId && x.SellerPartyId == sellerPartyId, cancellationToken);
        if (order is null) return null;
        var resolved = await ResolveLineCategoriesAsync(order.Lines, cancellationToken);
        var visible = scope.Global ? order.Lines.ToList()
            : order.Lines.Where(x => IsAllowed(x, scope.AllowedCategoryIds, resolved)).ToList();
        if (visible.Count == 0) throw new PlatformHttpException(403, "Order access denied.", "seller.order.view.denied");
        var checkout = await orders.Checkouts.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CheckoutId == order.CheckoutId, cancellationToken);
        var lines = visible.Select(x => new SellerOrderLineView(
            x.OfferId, "Order item", x.Quantity, x.UnitPriceSnapshot,
            x.LineTotalSnapshot + x.TaxAmountSnapshot - x.DiscountAmountSnapshot, x.Currency)).ToArray();
        var subtotal = visible.Sum(x => x.PostDiscountTaxExclusiveSnapshot);
        var tax = visible.Sum(x => x.TaxAmountSnapshot);
        var discount = visible.Sum(x => x.DiscountAmountSnapshot);
        return new SellerOrderDetailPage(
            order.SellerOrderId, order.OrderNumber, order.SellerPartyId, checkout?.SubmittedAt ?? default,
            order.Status.ToString(), order.Status.ToString(), subtotal, tax, discount, subtotal + tax,
            order.Currency, checkout?.RecipientName ?? string.Empty, checkout?.ContactMobile ?? string.Empty,
            checkout?.ProvinceName ?? string.Empty, checkout?.CityName ?? string.Empty,
            checkout?.PostalAddress ?? string.Empty, checkout?.PostalCode ?? string.Empty,
            checkout?.ShippingMethodLabel ?? string.Empty, lines);
    }

    private sealed record OrderViewScope(bool Denied, bool Global, HashSet<Guid> AllowedCategoryIds);

    private async Task<OrderViewScope> ResolveOrderViewScopeAsync(
        Guid sellerPartyId, Guid actorUserId, CancellationToken cancellationToken)
    {
        var effective = await access.GetEffectiveAccessAsync(
            actorUserId, new AccessOwnerScope(AccessOwnerScopeKind.Seller, sellerPartyId), cancellationToken);
        var permissions = effective.Permissions.Where(x => x.PermissionId == "order.view" && !x.DeniedByCeiling).ToList();
        if (permissions.Count == 0) return new(true, false, []);
        if (permissions.Any(x => x.ScopeKind == AccessScopeKind.GlobalWithinOwner)) return new(false, true, []);
        var allowed = permissions.Where(x => x.ScopeKind == AccessScopeKind.Category && x.ScopeResourceId != null)
            .Select(x => x.ScopeResourceId!.Value).ToHashSet();
        return new(allowed.Count == 0, false, allowed);
    }

    private async Task<IReadOnlyList<SellerOrder>> FilterOrdersByScopeAsync(
        IReadOnlyList<SellerOrder> rows, OrderViewScope scope, CancellationToken cancellationToken)
    {
        if (scope.Denied) return [];
        if (scope.Global) return rows;
        var resolved = await ResolveLineCategoriesAsync(rows.SelectMany(x => x.Lines).ToList(), cancellationToken);
        return rows.Where(x => x.Lines.Any(line => IsAllowed(line, scope.AllowedCategoryIds, resolved))).ToArray();
    }

    private async Task<IReadOnlyDictionary<Guid, Guid?>> ResolveLineCategoriesAsync(
        IReadOnlyCollection<OrderLine> lines, CancellationToken cancellationToken)
    {
        var missing = lines.Where(x => x.CategoryIdSnapshot is null)
            .Select(x => x.CatalogVariantId).Distinct().ToArray();
        return missing.Length == 0
            ? new Dictionary<Guid, Guid?>()
            : await catalogLookup.GetPrimaryCategoryIdsByVariantIdsAsync(missing, cancellationToken);
    }

    private static bool IsAllowed(
        OrderLine line, HashSet<Guid> allowed, IReadOnlyDictionary<Guid, Guid?> resolved)
    {
        var category = line.CategoryIdSnapshot ?? resolved.GetValueOrDefault(line.CatalogVariantId);
        return category is Guid id && allowed.Contains(id);
    }

    private async Task EnsureSellerAsync(Guid sellerPartyId, CancellationToken cancellationToken)
    {
        if (await parties.FindByIdAsync(sellerPartyId, cancellationToken) is null)
            throw new PlatformHttpException(404, "Seller was not found.", "seller.missing");
    }
}
