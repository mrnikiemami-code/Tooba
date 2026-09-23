using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts;
using Tooba.Order.Application.Seller.Models;
using Tooba.Order.Application.Seller.Ports;
using Tooba.Order.Domain;
using Tooba.Party.Contracts;

namespace Tooba.Order.Application.Seller;

/// <summary>
/// ترکیب read-model سفارش فروشنده با فیلتر scope و Contracts بیگانه (Catalog/Party).
/// </summary>
public sealed class SellerOrderComposer
{
    private readonly ISellerOrderStore _store;
    private readonly ISellerOrderViewAccessReader _access;
    private readonly IPartyLookup _parties;
    private readonly ICatalogVariantLookup _catalog;

    public SellerOrderComposer(
        ISellerOrderStore store,
        ISellerOrderViewAccessReader access,
        IPartyLookup parties,
        ICatalogVariantLookup catalog)
    {
        _store = store;
        _access = access;
        _parties = parties;
        _catalog = catalog;
    }

    /// <summary>وجود فروشنده را تضمین می‌کند یا seller.missing برمی‌گرداند.</summary>
    public async Task<Result> EnsureSellerAsync(Guid sellerPartyId, CancellationToken cancellationToken)
    {
        if (await _parties.FindByIdAsync(sellerPartyId, cancellationToken) is null)
        {
            return Result.Failure(new SemanticError(SellerOrderErrors.SellerMissing));
        }

        return Result.Success();
    }

    /// <summary>فهرست سفارش‌های مجاز Actor.</summary>
    public async Task<Result<IReadOnlyList<SellerOrderListItem>>> ListAsync(
        Guid sellerPartyId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var ensure = await EnsureSellerAsync(sellerPartyId, cancellationToken);
        if (ensure.IsFailure)
        {
            return Result.Failure<IReadOnlyList<SellerOrderListItem>>(ensure.FirstError);
        }

        var scope = await _access.GetOrderViewAccessAsync(actorUserId, sellerPartyId, cancellationToken);
        if (scope.Denied)
        {
            return Result.Success<IReadOnlyList<SellerOrderListItem>>([]);
        }

        var rows = await _store.ListBySellerAsync(sellerPartyId, take: 200, cancellationToken);
        var allowed = scope.AllowedCategoryIds.ToHashSet();
        var resolved = scope.GlobalWithinOwner
            ? new Dictionary<Guid, Guid?>()
            : await ResolveLineCategoriesAsync(rows.SelectMany(x => x.Lines).ToList(), cancellationToken);
        var visible = scope.GlobalWithinOwner
            ? rows
            : rows.Where(order => order.Lines.Any(line => IsAllowed(line, allowed, resolved))).ToList();
        var checkoutIds = visible.Select(x => x.CheckoutId).Distinct().ToArray();
        var checkouts = await _store.GetCheckoutsAsync(checkoutIds, cancellationToken);
        return Result.Success<IReadOnlyList<SellerOrderListItem>>(visible.Select(order =>
        {
            checkouts.TryGetValue(order.CheckoutId, out var checkout);
            var count = scope.GlobalWithinOwner
                ? order.Lines.Count
                : order.Lines.Count(x => IsAllowed(x, allowed, resolved));
            return new SellerOrderListItem(
                order.SellerOrderId,
                order.OrderNumber,
                checkout?.SubmittedAt ?? default,
                checkout?.RecipientName ?? string.Empty,
                count,
                order.GrandTotalSnapshot,
                order.Currency,
                order.Status.ToString(),
                order.Status.ToString());
        }).ToArray());
    }

    /// <summary>جزئیات سفارش با فیلتر خطوط بر اساس scope.</summary>
    public async Task<Result<SellerOrderDetailPage>> GetDetailAsync(
        Guid sellerPartyId,
        Guid actorUserId,
        Guid sellerOrderId,
        CancellationToken cancellationToken)
    {
        var ensure = await EnsureSellerAsync(sellerPartyId, cancellationToken);
        if (ensure.IsFailure)
        {
            return Result.Failure<SellerOrderDetailPage>(ensure.FirstError);
        }

        var scope = await _access.GetOrderViewAccessAsync(actorUserId, sellerPartyId, cancellationToken);
        if (scope.Denied)
        {
            return Result.Failure<SellerOrderDetailPage>(new SemanticError(SellerOrderErrors.ViewDenied));
        }

        var order = await _store.GetOwnedAsync(sellerPartyId, sellerOrderId, cancellationToken);
        if (order is null)
        {
            return Result.Failure<SellerOrderDetailPage>(new SemanticError(SellerOrderErrors.OrderMissing));
        }

        var allowed = scope.AllowedCategoryIds.ToHashSet();
        var resolved = await ResolveLineCategoriesAsync(order.Lines, cancellationToken);
        var visible = scope.GlobalWithinOwner
            ? order.Lines.ToList()
            : order.Lines.Where(x => IsAllowed(x, allowed, resolved)).ToList();
        if (visible.Count == 0)
        {
            return Result.Failure<SellerOrderDetailPage>(new SemanticError(SellerOrderErrors.ViewDenied));
        }

        var checkout = await _store.GetCheckoutAsync(order.CheckoutId, cancellationToken);
        var lines = visible.Select(x => new SellerOrderLineView(
            x.OfferId,
            "Order item",
            x.Quantity,
            x.UnitPriceSnapshot,
            x.LineTotalSnapshot + x.TaxAmountSnapshot - x.DiscountAmountSnapshot,
            x.Currency)).ToArray();
        var subtotal = visible.Sum(x => x.PostDiscountTaxExclusiveSnapshot);
        var tax = visible.Sum(x => x.TaxAmountSnapshot);
        var discount = visible.Sum(x => x.DiscountAmountSnapshot);
        return Result.Success(new SellerOrderDetailPage(
            order.SellerOrderId,
            order.OrderNumber,
            order.SellerPartyId,
            checkout?.SubmittedAt ?? default,
            order.Status.ToString(),
            order.Status.ToString(),
            subtotal,
            tax,
            discount,
            subtotal + tax,
            order.Currency,
            checkout?.RecipientName ?? string.Empty,
            checkout?.ContactMobile ?? string.Empty,
            checkout?.ProvinceName ?? string.Empty,
            checkout?.CityName ?? string.Empty,
            checkout?.PostalAddress ?? string.Empty,
            checkout?.PostalCode ?? string.Empty,
            checkout?.ShippingMethodLabel ?? string.Empty,
            lines));
    }

    /// <summary>شمارش open/paid پس از فیلتر scope.</summary>
    public async Task<Result<SellerOrderDashboardSummary>> GetDashboardSummaryAsync(
        Guid sellerPartyId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var ensure = await EnsureSellerAsync(sellerPartyId, cancellationToken);
        if (ensure.IsFailure)
        {
            return Result.Failure<SellerOrderDashboardSummary>(ensure.FirstError);
        }

        var rows = await _store.ListAllBySellerAsync(sellerPartyId, cancellationToken);
        var scope = await _access.GetOrderViewAccessAsync(actorUserId, sellerPartyId, cancellationToken);
        var visible = await FilterOrdersByScopeAsync(rows, scope, cancellationToken);
        var open = visible.Count(x => x.Status is SellerOrderStatus.Submitted
            or SellerOrderStatus.PendingPayment or SellerOrderStatus.ReservationRequested);
        var paid = visible.Count(x => x.Status == SellerOrderStatus.Paid);
        return Result.Success(new SellerOrderDashboardSummary(open, paid));
    }

    private async Task<IReadOnlyList<SellerOrder>> FilterOrdersByScopeAsync(
        IReadOnlyList<SellerOrder> rows,
        SellerOrderViewAccessSnapshot scope,
        CancellationToken cancellationToken)
    {
        if (scope.Denied)
        {
            return [];
        }

        if (scope.GlobalWithinOwner)
        {
            return rows;
        }

        var allowed = scope.AllowedCategoryIds.ToHashSet();
        var resolved = await ResolveLineCategoriesAsync(rows.SelectMany(x => x.Lines).ToList(), cancellationToken);
        return rows.Where(x => x.Lines.Any(line => IsAllowed(line, allowed, resolved))).ToArray();
    }

    private async Task<IReadOnlyDictionary<Guid, Guid?>> ResolveLineCategoriesAsync(
        IReadOnlyCollection<OrderLine> lines,
        CancellationToken cancellationToken)
    {
        var missing = lines.Where(x => x.CategoryIdSnapshot is null)
            .Select(x => x.CatalogVariantId)
            .Distinct()
            .ToArray();
        return missing.Length == 0
            ? new Dictionary<Guid, Guid?>()
            : await _catalog.GetPrimaryCategoryIdsByVariantIdsAsync(missing, cancellationToken);
    }

    private static bool IsAllowed(
        OrderLine line,
        HashSet<Guid> allowed,
        IReadOnlyDictionary<Guid, Guid?> resolved)
    {
        var category = line.CategoryIdSnapshot ?? resolved.GetValueOrDefault(line.CatalogVariantId);
        return category is Guid id && allowed.Contains(id);
    }
}
