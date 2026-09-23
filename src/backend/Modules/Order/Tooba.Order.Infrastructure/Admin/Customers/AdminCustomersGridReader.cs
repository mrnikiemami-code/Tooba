using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Grid;
using Tooba.Order.Application.Admin.Customers.Models;
using Tooba.Order.Application.Admin.Customers.Ports;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Persistence.Grid;

namespace Tooba.Order.Infrastructure.Admin.Customers;

/// <summary>
/// پرس‌وجوی DB-native مشتریان Admin — گروه روی PlacedByUserId با Count/Max؛
/// نام/موبایل آخرین checkout فقط برای صفحه.
/// Status همیشه Active (فیلتر status=Active عبوری است).
/// </summary>
internal sealed class AdminCustomersGridReader(OrderDbContext orders) : IAdminCustomersGridReader
{
    public async Task<IReadOnlyList<AdminCustomerListItem>> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await orders.Checkouts.AsNoTracking()
            .Select(x => new
            {
                x.PlacedByUserId,
                x.RecipientName,
                x.RecipientFirstName,
                x.RecipientLastName,
                x.ContactMobile,
                x.SubmittedAt,
            })
            .ToListAsync(cancellationToken);
        return rows.GroupBy(x => x.PlacedByUserId)
            .Select(group =>
            {
                var latest = group.OrderByDescending(x => x.SubmittedAt).First();
                return new AdminCustomerListItem(
                    group.Key,
                    StorefrontRecipientNames.DisplayOrFallback(
                        latest.RecipientFirstName,
                        latest.RecipientLastName,
                        latest.RecipientName),
                    string.IsNullOrWhiteSpace(latest.ContactMobile) ? null : latest.ContactMobile,
                    group.Count(),
                    latest.SubmittedAt,
                    "Active");
            })
            .OrderByDescending(x => x.LastOrderAt)
            .ToList();
    }

    public async Task<GridPageResponse<AdminCustomerListItem>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var aggregates = orders.Checkouts.AsNoTracking()
            .GroupBy(x => x.PlacedByUserId)
            .Select(g => new CustomerAgg(
                g.Key,
                g.Count(),
                g.Max(x => x.SubmittedAt)));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            var matchingUserIds = await orders.Checkouts.AsNoTracking()
                .Where(c =>
                    c.RecipientName.ToLower().Contains(term)
                    || c.ContactMobile.ToLower().Contains(term))
                .Select(c => c.PlacedByUserId)
                .Distinct()
                .ToListAsync(cancellationToken);
            aggregates = aggregates.Where(a => matchingUserIds.Contains(a.UserId));
        }

        foreach (var filter in request.Filters)
        {
            aggregates = await ApplyFilterAsync(aggregates, filter, cancellationToken);
        }

        var advancedIds = await EvaluateAdvancedAsync(request.AdvancedFilter, cancellationToken);
        if (advancedIds is not null)
        {
            aggregates = aggregates.Where(a => advancedIds.Contains(a.UserId));
        }

        var total = await aggregates.CountAsync(cancellationToken);
        if (total == 0)
        {
            return new GridPageResponse<AdminCustomerListItem>([], request.Page, request.PageSize, 0);
        }

        var sort = request.Sort.FirstOrDefault() ?? new GridSortRequest("activity", "desc");
        var ordered = Order(aggregates, sort);
        var pageAggs = await ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = await MaterializePageAsync(pageAggs, cancellationToken);
        return new GridPageResponse<AdminCustomerListItem>(items, request.Page, request.PageSize, total);
    }

    private async Task<HashSet<Guid>?> EvaluateAdvancedAsync(
        GridAdvancedFilterExpression? expression,
        CancellationToken cancellationToken)
    {
        if (expression?.Conditions is not { Count: > 0 })
        {
            return null;
        }

        var baseAgg = orders.Checkouts.AsNoTracking()
            .GroupBy(x => x.PlacedByUserId)
            .Select(g => new CustomerAgg(g.Key, g.Count(), g.Max(x => x.SubmittedAt)));

        var sets = new List<HashSet<Guid>>();
        foreach (var condition in expression.Conditions)
        {
            var filter = new GridFilterRequest(
                condition.Field,
                condition.Operator,
                condition.Value,
                condition.ValueTo,
                condition.Values);
            var filtered = await ApplyFilterAsync(baseAgg, filter, cancellationToken);
            var ids = await filtered.Select(x => x.UserId).ToListAsync(cancellationToken);
            sets.Add(ids.ToHashSet());
        }

        return GridAdvancedFilterEvaluator.EvaluateLeftToRight(sets, expression.Connectors);
    }

    private async Task<IQueryable<CustomerAgg>> ApplyFilterAsync(
        IQueryable<CustomerAgg> source,
        GridFilterRequest filter,
        CancellationToken cancellationToken)
    {
        switch (filter.Field)
        {
            case "name":
            case "contact":
            {
                var ids = await ResolveUserIdsByCheckoutTextAsync(filter, cancellationToken);
                return source.Where(a => ids.Contains(a.UserId));
            }
            case "orders":
                return EfGridQuery.ApplyIntFilter(source, x => x.OrderCount, filter);
            case "activity":
                return EfGridQuery.ApplyDateFilter(source, x => x.LastOrderAt, filter);
            case "status":
                return IsActiveOnlyFilter(filter) ? source : source.Where(_ => false);
            default:
                return source;
        }
    }

    private static bool IsActiveOnlyFilter(GridFilterRequest filter)
    {
        var op = (filter.Operator ?? string.Empty).Trim();
        if (op is "blank")
        {
            return false;
        }

        if (op is "notBlank")
        {
            return true;
        }

        var values = (filter.Values ?? [])
            .Concat(string.IsNullOrWhiteSpace(filter.Value) ? [] : [filter.Value!])
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (values.Count == 0)
        {
            return true;
        }

        return op switch
        {
            "notEqual" or "notIn" => !values.Contains("Active"),
            _ => values.Contains("Active"),
        };
    }

    private async Task<HashSet<Guid>> ResolveUserIdsByCheckoutTextAsync(
        GridFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var fieldIsName = filter.Field == "name";
        IQueryable<Tooba.Order.Domain.CheckoutGroup> q = orders.Checkouts.AsNoTracking();
        q = fieldIsName
            ? EfGridQuery.ApplyTextFilter(q, x => x.RecipientName, filter)
            : EfGridQuery.ApplyTextFilter(q, x => x.ContactMobile, filter);
        return (await q.Select(x => x.PlacedByUserId).Distinct().ToListAsync(cancellationToken)).ToHashSet();
    }

    private static IQueryable<CustomerAgg> Order(IQueryable<CustomerAgg> source, GridSortRequest sort)
    {
        var asc = sort.Direction == "asc";
        return sort.Field switch
        {
            "orders" => asc
                ? source.OrderBy(x => x.OrderCount).ThenBy(x => x.UserId)
                : source.OrderByDescending(x => x.OrderCount).ThenBy(x => x.UserId),
            "status" => source.OrderBy(x => x.UserId),
            "name" or "contact" => asc
                ? source.OrderBy(x => x.UserId)
                : source.OrderByDescending(x => x.UserId),
            _ => asc
                ? source.OrderBy(x => x.LastOrderAt).ThenBy(x => x.UserId)
                : source.OrderByDescending(x => x.LastOrderAt).ThenBy(x => x.UserId),
        };
    }

    private async Task<IReadOnlyList<AdminCustomerListItem>> MaterializePageAsync(
        List<CustomerAgg> pageAggs,
        CancellationToken cancellationToken)
    {
        if (pageAggs.Count == 0)
        {
            return [];
        }

        var userIds = pageAggs.Select(x => x.UserId).ToList();
        var checkouts = await orders.Checkouts.AsNoTracking()
            .Where(x => userIds.Contains(x.PlacedByUserId))
            .Select(x => new { x.PlacedByUserId, x.RecipientName, x.ContactMobile, x.SubmittedAt })
            .ToListAsync(cancellationToken);
        var latestByUser = checkouts
            .GroupBy(x => x.PlacedByUserId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.SubmittedAt).First());

        return pageAggs.Select(agg =>
        {
            latestByUser.TryGetValue(agg.UserId, out var latest);
            return new AdminCustomerListItem(
                agg.UserId,
                latest is null || string.IsNullOrWhiteSpace(latest.RecipientName)
                    ? "مشتری توبا"
                    : latest.RecipientName,
                latest is null || string.IsNullOrWhiteSpace(latest.ContactMobile)
                    ? null
                    : latest.ContactMobile,
                agg.OrderCount,
                agg.LastOrderAt,
                "Active");
        }).ToList();
    }

    private sealed record CustomerAgg(Guid UserId, int OrderCount, DateTimeOffset LastOrderAt);
}
