using Microsoft.EntityFrameworkCore;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure;

/// <summary>غنی‌سازی گرید از دادهٔ Order بدون افشای DbContext به ماژول‌های مصرف‌کننده.</summary>
public sealed class OrderGridEnrichmentBridge : IOrderGridEnrichmentReader
{
    private readonly OrderDbContext _db;

    /// <summary>پل را به OrderDbContext وصل می‌کند.</summary>
    public OrderGridEnrichmentBridge(OrderDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, string>> GetOrderNumbersAsync(
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken)
    {
        if (sellerOrderIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var rows = await _db.SellerOrders.AsNoTracking()
            .Where(x => sellerOrderIds.Contains(x.SellerOrderId))
            .Select(x => new { x.SellerOrderId, x.OrderNumber })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(x => x.SellerOrderId, x => x.OrderNumber);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Guid>> SearchSellerOrderIdsByOrderNumberAsync(
        string term,
        int take,
        CancellationToken cancellationToken) =>
        FilterSellerOrderIdsByOrderNumberAsync("contains", term, null, take, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> FilterSellerOrderIdsByOrderNumberAsync(
        string? op,
        string? value,
        IReadOnlyList<string>? values,
        int take,
        CancellationToken cancellationToken)
    {
        var q = _db.SellerOrders.AsNoTracking().AsQueryable();
        q = ApplyStringFilter(q, x => x.OrderNumber, op, value, values);
        return await q.Select(x => x.SellerOrderId).Take(take).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, string>> GetRecipientNamesAsync(
        IReadOnlyList<Guid> checkoutIds,
        CancellationToken cancellationToken)
    {
        if (checkoutIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var rows = await _db.Checkouts.AsNoTracking()
            .Where(x => checkoutIds.Contains(x.CheckoutId))
            .Select(x => new { x.CheckoutId, x.RecipientName })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(x => x.CheckoutId, x => x.RecipientName);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Guid>> SearchCheckoutIdsByRecipientAsync(
        string term,
        int take,
        CancellationToken cancellationToken) =>
        FilterCheckoutIdsByRecipientAsync("contains", term, null, take, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> FilterCheckoutIdsByRecipientAsync(
        string? op,
        string? value,
        IReadOnlyList<string>? values,
        int take,
        CancellationToken cancellationToken)
    {
        var q = _db.Checkouts.AsNoTracking().AsQueryable();
        q = ApplyStringFilter(q, x => x.RecipientName, op, value, values);
        return await q.Select(x => x.CheckoutId).Take(take).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, OrderGridLineSnapshot>> GetLinesAsync(
        IReadOnlyList<Guid> orderLineIds,
        CancellationToken cancellationToken)
    {
        if (orderLineIds.Count == 0)
        {
            return new Dictionary<Guid, OrderGridLineSnapshot>();
        }

        var rows = await _db.Lines.AsNoTracking()
            .Where(x => orderLineIds.Contains(x.LineId))
            .Select(x => new OrderGridLineSnapshot(
                x.LineId,
                x.CatalogVariantId,
                x.UnitCodeSnapshot,
                x.QuantityDecimalPlacesSnapshot,
                x.ReturnPolicyLabelSnapshot,
                x.IsReturnableSnapshot,
                x.ReturnWindowDaysSnapshot))
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(x => x.LineId);
    }

    private static IQueryable<T> ApplyStringFilter<T>(
        IQueryable<T> source,
        System.Linq.Expressions.Expression<Func<T, string?>> selector,
        string? op,
        string? value,
        IReadOnlyList<string>? values)
    {
        var operatorKind = (op ?? string.Empty).Trim();
        var text = (value ?? string.Empty).Trim();
        return operatorKind switch
        {
            "blank" => source.Where(BuildBlank(selector, blank: true)),
            "notBlank" => source.Where(BuildBlank(selector, blank: false)),
            "equals" => source.Where(BuildEquals(selector, text)),
            "notEqual" => source.Where(BuildNotEquals(selector, text)),
            "startsWith" => source.Where(BuildStartsWith(selector, text)),
            "endsWith" => source.Where(BuildEndsWith(selector, text)),
            "notContains" => source.Where(BuildNotContains(selector, text)),
            "notIn" when values is { Count: > 0 } => source.Where(BuildNotIn(selector, values)),
            "in" when values is { Count: > 0 } => source.Where(BuildIn(selector, values)),
            _ => source.Where(BuildContains(selector, text)),
        };
    }

    private static System.Linq.Expressions.Expression<Func<T, bool>> BuildBlank<T>(
        System.Linq.Expressions.Expression<Func<T, string?>> selector,
        bool blank)
    {
        var p = selector.Parameters[0];
        var body = selector.Body;
        var isNull = System.Linq.Expressions.Expression.Equal(body, System.Linq.Expressions.Expression.Constant(null, typeof(string)));
        var trim = System.Linq.Expressions.Expression.Condition(
            isNull,
            System.Linq.Expressions.Expression.Constant(string.Empty),
            System.Linq.Expressions.Expression.Call(body, nameof(string.Trim), Type.EmptyTypes));
        var isEmpty = System.Linq.Expressions.Expression.Equal(trim, System.Linq.Expressions.Expression.Constant(string.Empty));
        var pred = blank
            ? System.Linq.Expressions.Expression.OrElse(isNull, isEmpty)
            : System.Linq.Expressions.Expression.AndAlso(
                System.Linq.Expressions.Expression.Not(isNull),
                System.Linq.Expressions.Expression.Not(isEmpty));
        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(pred, p);
    }

    private static System.Linq.Expressions.Expression<Func<T, bool>> BuildContains<T>(
        System.Linq.Expressions.Expression<Func<T, string?>> selector,
        string value)
    {
        var p = selector.Parameters[0];
        var coalesced = System.Linq.Expressions.Expression.Coalesce(selector.Body, System.Linq.Expressions.Expression.Constant(string.Empty));
        var lower = System.Linq.Expressions.Expression.Call(coalesced, nameof(string.ToLower), Type.EmptyTypes);
        var call = System.Linq.Expressions.Expression.Call(
            lower,
            nameof(string.Contains),
            Type.EmptyTypes,
            System.Linq.Expressions.Expression.Constant(value.ToLowerInvariant()));
        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(call, p);
    }

    private static System.Linq.Expressions.Expression<Func<T, bool>> BuildNotContains<T>(
        System.Linq.Expressions.Expression<Func<T, string?>> selector,
        string value)
    {
        var contains = BuildContains(selector, value);
        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(
            System.Linq.Expressions.Expression.Not(contains.Body),
            contains.Parameters);
    }

    private static System.Linq.Expressions.Expression<Func<T, bool>> BuildEquals<T>(
        System.Linq.Expressions.Expression<Func<T, string?>> selector,
        string value)
    {
        var p = selector.Parameters[0];
        var coalesced = System.Linq.Expressions.Expression.Coalesce(selector.Body, System.Linq.Expressions.Expression.Constant(string.Empty));
        var lower = System.Linq.Expressions.Expression.Call(coalesced, nameof(string.ToLower), Type.EmptyTypes);
        var eq = System.Linq.Expressions.Expression.Equal(
            lower,
            System.Linq.Expressions.Expression.Constant(value.ToLowerInvariant()));
        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(eq, p);
    }

    private static System.Linq.Expressions.Expression<Func<T, bool>> BuildNotEquals<T>(
        System.Linq.Expressions.Expression<Func<T, string?>> selector,
        string value)
    {
        var eq = BuildEquals(selector, value);
        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(
            System.Linq.Expressions.Expression.Not(eq.Body),
            eq.Parameters);
    }

    private static System.Linq.Expressions.Expression<Func<T, bool>> BuildStartsWith<T>(
        System.Linq.Expressions.Expression<Func<T, string?>> selector,
        string value)
    {
        var p = selector.Parameters[0];
        var coalesced = System.Linq.Expressions.Expression.Coalesce(selector.Body, System.Linq.Expressions.Expression.Constant(string.Empty));
        var lower = System.Linq.Expressions.Expression.Call(coalesced, nameof(string.ToLower), Type.EmptyTypes);
        var call = System.Linq.Expressions.Expression.Call(
            lower,
            nameof(string.StartsWith),
            Type.EmptyTypes,
            System.Linq.Expressions.Expression.Constant(value.ToLowerInvariant()));
        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(call, p);
    }

    private static System.Linq.Expressions.Expression<Func<T, bool>> BuildEndsWith<T>(
        System.Linq.Expressions.Expression<Func<T, string?>> selector,
        string value)
    {
        var p = selector.Parameters[0];
        var coalesced = System.Linq.Expressions.Expression.Coalesce(selector.Body, System.Linq.Expressions.Expression.Constant(string.Empty));
        var lower = System.Linq.Expressions.Expression.Call(coalesced, nameof(string.ToLower), Type.EmptyTypes);
        var call = System.Linq.Expressions.Expression.Call(
            lower,
            nameof(string.EndsWith),
            Type.EmptyTypes,
            System.Linq.Expressions.Expression.Constant(value.ToLowerInvariant()));
        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(call, p);
    }

    private static System.Linq.Expressions.Expression<Func<T, bool>> BuildIn<T>(
        System.Linq.Expressions.Expression<Func<T, string?>> selector,
        IReadOnlyList<string> values)
    {
        var lowered = values.Select(v => v.Trim().ToLowerInvariant()).ToList();
        var p = selector.Parameters[0];
        var coalesced = System.Linq.Expressions.Expression.Coalesce(selector.Body, System.Linq.Expressions.Expression.Constant(string.Empty));
        var lower = System.Linq.Expressions.Expression.Call(coalesced, nameof(string.ToLower), Type.EmptyTypes);
        var contains = System.Linq.Expressions.Expression.Call(
            System.Linq.Expressions.Expression.Constant(lowered),
            typeof(List<string>).GetMethod(nameof(List<string>.Contains), [typeof(string)])!,
            lower);
        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(contains, p);
    }

    private static System.Linq.Expressions.Expression<Func<T, bool>> BuildNotIn<T>(
        System.Linq.Expressions.Expression<Func<T, string?>> selector,
        IReadOnlyList<string> values)
    {
        var included = BuildIn(selector, values);
        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(
            System.Linq.Expressions.Expression.Not(included.Body),
            included.Parameters);
    }
}
