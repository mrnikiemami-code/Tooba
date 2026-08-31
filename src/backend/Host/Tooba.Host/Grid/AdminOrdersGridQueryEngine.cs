using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Grid;
using Tooba.Host.Admin;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Host.Grid;

/// <summary>پرس‌وجوی DB-native گرید سفارش‌های Admin روی Checkout + aggregates SellerOrders.</summary>
internal sealed class AdminOrdersGridQueryEngine
{
    private readonly OrderDbContext _orders;

    public AdminOrdersGridQueryEngine(OrderDbContext orders) => _orders = orders;

    public async Task<GridPageResponse<AdminOrderListItem>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<CheckoutGroup> q = _orders.Checkouts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            q = q.Where(c =>
                c.RecipientName.ToLower().Contains(term)
                || c.CheckoutId.ToString().ToLower().Contains(term)
                || c.SellerOrders.Any(o => o.OrderNumber.ToLower().Contains(term)));
        }

        foreach (var filter in request.Filters)
        {
            q = ApplyFilter(q, filter);
        }

        var advancedIds = await EvaluateAdvancedAsync(request.AdvancedFilter, cancellationToken);
        if (advancedIds is not null)
        {
            q = q.Where(x => advancedIds.Contains(x.CheckoutId));
        }

        var sort = request.Sort.FirstOrDefault() ?? new GridSortRequest("created", "desc");
        return await AdminEfGridQuery.PageAsync(
            q,
            request,
            filtered => Order(filtered, sort),
            MapPageAsync,
            cancellationToken);
    }

    private async Task<HashSet<Guid>?> EvaluateAdvancedAsync(
        GridAdvancedFilterExpression? expression,
        CancellationToken cancellationToken)
    {
        if (expression?.Conditions is not { Count: > 0 })
        {
            return null;
        }

        var sets = new List<HashSet<Guid>>();
        foreach (var condition in expression.Conditions)
        {
            var filter = new GridFilterRequest(
                condition.Field,
                condition.Operator,
                condition.Value,
                condition.ValueTo,
                condition.Values);
            var ids = await ApplyFilter(_orders.Checkouts.AsNoTracking(), filter)
                .Select(x => x.CheckoutId)
                .ToListAsync(cancellationToken);
            sets.Add(ids.ToHashSet());
        }

        return GridAdvancedFilterEvaluator.EvaluateLeftToRight(sets, expression.Connectors);
    }

    private static IQueryable<CheckoutGroup> ApplyFilter(IQueryable<CheckoutGroup> source, GridFilterRequest filter)
    {
        switch (filter.Field)
        {
            case "reference":
            {
                var op = (filter.Operator ?? string.Empty).Trim();
                var value = (filter.Value ?? string.Empty).Trim().ToLower();
                return op switch
                {
                    "blank" => source.Where(c => !c.SellerOrders.Any(o => o.OrderNumber != "")),
                    "notBlank" => source.Where(c => c.SellerOrders.Any(o => o.OrderNumber != "")),
                    "equals" => source.Where(c =>
                        c.SellerOrders.Any(o => o.OrderNumber.ToLower() == value)
                        || c.CheckoutId.ToString().ToLower() == value),
                    "notEqual" => source.Where(c =>
                        !c.SellerOrders.Any(o => o.OrderNumber.ToLower() == value)
                        && c.CheckoutId.ToString().ToLower() != value),
                    "startsWith" => source.Where(c =>
                        c.SellerOrders.Any(o => o.OrderNumber.ToLower().StartsWith(value))
                        || c.CheckoutId.ToString().ToLower().StartsWith(value)),
                    "endsWith" => source.Where(c =>
                        c.SellerOrders.Any(o => o.OrderNumber.ToLower().EndsWith(value))
                        || c.CheckoutId.ToString().ToLower().EndsWith(value)),
                    "notContains" => source.Where(c =>
                        !c.SellerOrders.Any(o => o.OrderNumber.ToLower().Contains(value))
                        && !c.CheckoutId.ToString().ToLower().Contains(value)),
                    _ => source.Where(c =>
                        c.SellerOrders.Any(o => o.OrderNumber.ToLower().Contains(value))
                        || c.CheckoutId.ToString().ToLower().Contains(value)),
                };
            }
            case "customer":
                return AdminEfGridQuery.ApplyTextFilter(source, x => x.RecipientName, filter);
            case "sellers":
                return ApplyIntAggFilter(source, c => c.SellerOrders.Count(), filter);
            case "lines":
                return ApplyIntAggFilter(source, c => c.SellerOrders.SelectMany(o => o.Lines).Sum(l => l.Quantity), filter);
            case "payment":
                return ApplyPaymentFilter(source, filter);
            case "status":
                return ApplyStatusFilter(source, filter);
            case "amount":
                return ApplyDecimalAggFilter(source, c => c.SellerOrders.Sum(o => o.GrandTotalSnapshot), filter);
            case "created":
                return AdminEfGridQuery.ApplyDateFilter(source, x => x.SubmittedAt, filter);
            default:
                return source;
        }
    }

    private static IQueryable<CheckoutGroup> ApplyPaymentFilter(IQueryable<CheckoutGroup> source, GridFilterRequest filter)
    {
        var op = (filter.Operator ?? string.Empty).Trim();
        var values = (filter.Values ?? [])
            .Concat(string.IsNullOrWhiteSpace(filter.Value) ? [] : [filter.Value!])
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (op is "blank")
        {
            return source.Where(_ => false);
        }

        if (op is "notBlank")
        {
            return source;
        }

        bool wantsPaid = values.Contains("Paid");
        bool wantsPending = values.Contains("PendingPayment");

        return op switch
        {
            "notEqual" or "notIn" when wantsPaid && !wantsPending =>
                source.Where(c => !(c.SellerOrders.Count > 0 && c.SellerOrders.All(o => o.Status == SellerOrderStatus.Paid))),
            "notEqual" or "notIn" when wantsPending && !wantsPaid =>
                source.Where(c => c.SellerOrders.Count > 0 && c.SellerOrders.All(o => o.Status == SellerOrderStatus.Paid)),
            _ when wantsPaid && !wantsPending =>
                source.Where(c => c.SellerOrders.Count > 0 && c.SellerOrders.All(o => o.Status == SellerOrderStatus.Paid)),
            _ when wantsPending && !wantsPaid =>
                source.Where(c => !(c.SellerOrders.Count > 0 && c.SellerOrders.All(o => o.Status == SellerOrderStatus.Paid))),
            _ => source,
        };
    }

    private static IQueryable<CheckoutGroup> ApplyStatusFilter(IQueryable<CheckoutGroup> source, GridFilterRequest filter)
    {
        var op = (filter.Operator ?? string.Empty).Trim();
        var values = (filter.Values ?? [])
            .Concat(string.IsNullOrWhiteSpace(filter.Value) ? [] : [filter.Value!])
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (op is "blank")
        {
            return source.Where(_ => false);
        }

        if (op is "notBlank")
        {
            return source;
        }

        var wantsMixed = values.Contains("Mixed");
        var parsed = values
            .Where(v => !string.Equals(v, "Mixed", StringComparison.OrdinalIgnoreCase))
            .Select(v => Enum.TryParse<SellerOrderStatus>(v, true, out var s) ? (SellerOrderStatus?)s : null)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();

        if (op is "notEqual" or "notIn")
        {
            return source.Where(c =>
                !(wantsMixed && c.SellerOrders.Select(o => o.Status).Distinct().Count() > 1)
                && !(parsed.Count > 0
                    && c.SellerOrders.Select(o => o.Status).Distinct().Count() == 1
                    && parsed.Contains(c.SellerOrders.Select(o => o.Status).First())));
        }

        return source.Where(c =>
            (wantsMixed && c.SellerOrders.Select(o => o.Status).Distinct().Count() > 1)
            || (parsed.Count > 0
                && c.SellerOrders.Select(o => o.Status).Distinct().Count() == 1
                && parsed.Contains(c.SellerOrders.Select(o => o.Status).First())));
    }

    private static IQueryable<CheckoutGroup> ApplyIntAggFilter(
        IQueryable<CheckoutGroup> source,
        System.Linq.Expressions.Expression<Func<CheckoutGroup, int>> selector,
        GridFilterRequest filter)
    {
        // Materialize via projection join pattern using EF-translatable Count/Sum already in selector body.
        return AdminEfGridQuery.ApplyIntFilter(source, selector, filter);
    }

    private static IQueryable<CheckoutGroup> ApplyDecimalAggFilter(
        IQueryable<CheckoutGroup> source,
        System.Linq.Expressions.Expression<Func<CheckoutGroup, decimal>> selector,
        GridFilterRequest filter) =>
        AdminEfGridQuery.ApplyNumberFilter(source, selector, filter);

    private static IQueryable<CheckoutGroup> Order(IQueryable<CheckoutGroup> source, GridSortRequest sort)
    {
        var asc = sort.Direction == "asc";
        return sort.Field switch
        {
            "reference" => asc
                ? source.OrderBy(c => c.SellerOrders.Select(o => o.OrderNumber).FirstOrDefault() ?? c.CheckoutId.ToString())
                    .ThenBy(c => c.CheckoutId)
                : source.OrderByDescending(c => c.SellerOrders.Select(o => o.OrderNumber).FirstOrDefault() ?? c.CheckoutId.ToString())
                    .ThenBy(c => c.CheckoutId),
            "customer" => asc
                ? source.OrderBy(c => c.RecipientName).ThenBy(c => c.CheckoutId)
                : source.OrderByDescending(c => c.RecipientName).ThenBy(c => c.CheckoutId),
            "sellers" => asc
                ? source.OrderBy(c => c.SellerOrders.Count).ThenBy(c => c.CheckoutId)
                : source.OrderByDescending(c => c.SellerOrders.Count).ThenBy(c => c.CheckoutId),
            "lines" => asc
                ? source.OrderBy(c => c.SellerOrders.SelectMany(o => o.Lines).Sum(l => l.Quantity)).ThenBy(c => c.CheckoutId)
                : source.OrderByDescending(c => c.SellerOrders.SelectMany(o => o.Lines).Sum(l => l.Quantity)).ThenBy(c => c.CheckoutId),
            "payment" => asc
                ? source.OrderBy(c => c.SellerOrders.All(o => o.Status == SellerOrderStatus.Paid) ? 1 : 0).ThenBy(c => c.CheckoutId)
                : source.OrderByDescending(c => c.SellerOrders.All(o => o.Status == SellerOrderStatus.Paid) ? 1 : 0).ThenBy(c => c.CheckoutId),
            "status" => asc
                ? source.OrderBy(c => c.SellerOrders.Select(o => o.Status).Distinct().Count() == 1
                        ? c.SellerOrders.Select(o => o.Status).First().ToString()
                        : "Mixed")
                    .ThenBy(c => c.CheckoutId)
                : source.OrderByDescending(c => c.SellerOrders.Select(o => o.Status).Distinct().Count() == 1
                        ? c.SellerOrders.Select(o => o.Status).First().ToString()
                        : "Mixed")
                    .ThenBy(c => c.CheckoutId),
            "amount" => asc
                ? source.OrderBy(c => c.SellerOrders.Sum(o => o.GrandTotalSnapshot)).ThenBy(c => c.CheckoutId)
                : source.OrderByDescending(c => c.SellerOrders.Sum(o => o.GrandTotalSnapshot)).ThenBy(c => c.CheckoutId),
            _ => asc
                ? source.OrderBy(c => c.SubmittedAt).ThenBy(c => c.CheckoutId)
                : source.OrderByDescending(c => c.SubmittedAt).ThenBy(c => c.CheckoutId),
        };
    }

    private async Task<IReadOnlyList<AdminOrderListItem>> MapPageAsync(
        List<CheckoutGroup> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var ids = rows.Select(x => x.CheckoutId).ToList();
        var groups = await _orders.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .Where(x => ids.Contains(x.CheckoutId))
            .ToListAsync(cancellationToken);
        var byId = groups.ToDictionary(x => x.CheckoutId);
        return rows
            .Where(r => byId.ContainsKey(r.CheckoutId))
            .Select(r => MapOrderListItem(byId[r.CheckoutId]))
            .ToList();
    }

    private static AdminOrderListItem MapOrderListItem(CheckoutGroup group)
    {
        var orders = group.SellerOrders;
        var references = orders.Select(x => x.OrderNumber).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        var statuses = orders.Select(x => x.Status).Distinct().ToList();
        return new AdminOrderListItem(
            group.CheckoutId,
            references.Count == 0 ? group.CheckoutId.ToString("N")[..12] : string.Join(" / ", references),
            group.SubmittedAt,
            string.IsNullOrWhiteSpace(group.RecipientName) ? "مشتری توبا" : group.RecipientName,
            orders.Count,
            orders.Sum(x => x.Lines.Sum(line => line.Quantity)),
            orders.Sum(x => x.GrandTotalSnapshot),
            orders.Select(x => x.Currency).FirstOrDefault() ?? "IRR",
            orders.Count > 0 && orders.All(x => x.Status == SellerOrderStatus.Paid) ? "Paid" : "PendingPayment",
            statuses.Count == 1 ? statuses[0].ToString() : "Mixed");
    }
}
