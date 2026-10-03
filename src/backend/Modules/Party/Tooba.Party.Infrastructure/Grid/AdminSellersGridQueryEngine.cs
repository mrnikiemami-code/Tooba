using Tooba.BuildingBlocks.Grid;
using Tooba.Offer.Contracts.Ports;
using Tooba.Order.Contracts.Admin;
using Tooba.Party.Contracts.Ports;

namespace Tooba.Party.Infrastructure.Grid;

/// <summary>
/// پرس‌وجوی فروشندگان Admin.
/// Party scalars از مرز Contracts Party؛ شمارنده‌های Offer/Order از Contracts.
/// بدون تزریق context سفارش/فروشنده و بدون JOIN بین schema.
/// مرتب‌سازی name/status سمت adapter Party؛
/// مرتب‌سازی offers/orders با الگوی محصول (IDهای فیلترشده سپس sort در حافظه).
/// </summary>
public sealed class AdminSellersGridQueryEngine
{
    private readonly IOfferQueryGateway _offers;
    private readonly IPartyAdminSellerReadGateway _parties;
    private readonly IAdminSellerOrderCountPort _orderCounts;

    /// <summary>موتور گرید فروشندگان را با مرزهای Offer/Party/Order می‌سازد.</summary>
    public AdminSellersGridQueryEngine(
        IOfferQueryGateway offers,
        IPartyAdminSellerReadGateway parties,
        IAdminSellerOrderCountPort orderCounts)
    {
        _offers = offers;
        _parties = parties;
        _orderCounts = orderCounts;
    }

    /// <summary>صفحهٔ گرید فروشندگان را با فیلتر/مرتب‌سازی/صفحه‌بندی برمی‌گرداند.</summary>
    public async Task<GridPageResponse<AdminSellerListItem>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var sellerIds = await _offers.ListDistinctSellerPartyIdsAsync(cancellationToken);

        var offerMetrics = await BuildOfferCountMetricsAsync(cancellationToken);
        var orderMetrics = await BuildOrderCountMetricsAsync(sellerIds, cancellationToken);

        // Party scalars come from the Contracts boundary (no Host persistence context).
        var projections = await _parties.GetStatusProjectionsAsync(sellerIds, cancellationToken);
        IReadOnlyList<PartyGridRow> partyRows = projections
            .Select(p => new PartyGridRow(p.PartyId, p.DisplayName, p.Status))
            .ToList();

        partyRows = ApplySearch(partyRows, request.Search);
        foreach (var filter in request.Filters.Where(f => f.Field is "name" or "status"))
        {
            partyRows = ApplyPartyFilter(partyRows, filter);
        }

        foreach (var filter in request.Filters.Where(f => f.Field is "offers" or "orders"))
        {
            var ids = filter.Field == "offers"
                ? FilterMetrics(offerMetrics, filter)
                : FilterMetrics(orderMetrics, filter);
            partyRows = partyRows.Where(p => ids.Contains(p.PartyId)).ToList();
        }

        var advancedIds = EvaluateAdvanced(partyRows, offerMetrics, orderMetrics, request.AdvancedFilter);
        if (advancedIds is not null)
        {
            partyRows = partyRows.Where(p => advancedIds.Contains(p.PartyId)).ToList();
        }

        var total = partyRows.Count();
        if (total == 0)
        {
            return new GridPageResponse<AdminSellerListItem>([], request.Page, request.PageSize, 0);
        }

        var sort = request.Sort.FirstOrDefault() ?? new GridSortRequest("name", "asc");
        var ordered = sort.Field is "offers" or "orders"
            // Same compromise as AdminProductGridQueryEngine.OrderAndPageByMetricAsync:
            // metric-sort in memory after Contracts filtering (cross-module JOIN forbidden).
            ? OrderByMetric(partyRows, sort, sort.Field == "offers" ? offerMetrics : orderMetrics)
            : OrderParty(partyRows, sort);

        var items = ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(party => new AdminSellerListItem(
                party.PartyId,
                party.DisplayName,
                party.Status,
                offerMetrics.GetValueOrDefault(party.PartyId),
                orderMetrics.GetValueOrDefault(party.PartyId)))
            .ToList();

        return new GridPageResponse<AdminSellerListItem>(items, request.Page, request.PageSize, total);
    }

    private static IReadOnlyList<PartyGridRow> ApplySearch(IReadOnlyList<PartyGridRow> source, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return source;
        }

        var term = search.Trim();
        return source
            .Where(p => p.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static HashSet<Guid>? EvaluateAdvanced(
        IReadOnlyList<PartyGridRow> partyRows,
        Dictionary<Guid, int> offerMetrics,
        Dictionary<Guid, int> orderMetrics,
        GridAdvancedFilterExpression? expression)
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
            HashSet<Guid> ids;
            if (filter.Field is "offers")
            {
                ids = FilterMetrics(offerMetrics, filter);
            }
            else if (filter.Field is "orders")
            {
                ids = FilterMetrics(orderMetrics, filter);
            }
            else
            {
                ids = ApplyPartyFilter(partyRows, filter).Select(p => p.PartyId).ToHashSet();
            }

            sets.Add(ids);
        }

        return GridAdvancedFilterEvaluator.EvaluateLeftToRight(sets, expression.Connectors);
    }

    private static IReadOnlyList<PartyGridRow> ApplyPartyFilter(
        IReadOnlyList<PartyGridRow> source,
        GridFilterRequest filter) =>
        filter.Field switch
        {
            "name" => ApplyTextFilter(source, filter, x => x.DisplayName),
            "status" => ApplyStatusFilter(source, filter),
            _ => source,
        };

    private static IReadOnlyList<PartyGridRow> ApplyTextFilter(
        IReadOnlyList<PartyGridRow> source,
        GridFilterRequest filter,
        Func<PartyGridRow, string> selector)
    {
        var op = (filter.Operator ?? string.Empty).Trim();
        var value = (filter.Value ?? string.Empty).Trim();
        return op switch
        {
            "blank" => source.Where(x => string.IsNullOrWhiteSpace(selector(x))).ToList(),
            "notBlank" => source.Where(x => !string.IsNullOrWhiteSpace(selector(x))).ToList(),
            "equals" => source.Where(x => string.Equals(selector(x), value, StringComparison.OrdinalIgnoreCase)).ToList(),
            "notEqual" => source.Where(x => !string.Equals(selector(x), value, StringComparison.OrdinalIgnoreCase)).ToList(),
            "startsWith" => source.Where(x => selector(x).StartsWith(value, StringComparison.OrdinalIgnoreCase)).ToList(),
            "endsWith" => source.Where(x => selector(x).EndsWith(value, StringComparison.OrdinalIgnoreCase)).ToList(),
            "notContains" => source.Where(x => !selector(x).Contains(value, StringComparison.OrdinalIgnoreCase)).ToList(),
            _ => source.Where(x => selector(x).Contains(value, StringComparison.OrdinalIgnoreCase)).ToList(),
        };
    }

    private static IReadOnlyList<PartyGridRow> ApplyStatusFilter(
        IReadOnlyList<PartyGridRow> source,
        GridFilterRequest filter)
    {
        var op = (filter.Operator ?? string.Empty).Trim();
        if (op is "blank")
        {
            return source;
        }

        if (op is "notBlank")
        {
            return source;
        }

        var values = (filter.Values ?? [])
            .Concat(string.IsNullOrWhiteSpace(filter.Value) ? [] : [filter.Value!])
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (values.Count == 0)
        {
            return [];
        }

        return op is "notEqual" or "notIn"
            ? source.Where(x => !values.Contains(x.Status, StringComparer.OrdinalIgnoreCase)).ToList()
            : source.Where(x => values.Contains(x.Status, StringComparer.OrdinalIgnoreCase)).ToList();
    }

    private static IReadOnlyList<PartyGridRow> OrderParty(IReadOnlyList<PartyGridRow> source, GridSortRequest sort)
    {
        var asc = sort.Direction == "asc";
        return sort.Field switch
        {
            "status" => asc
                ? source.OrderBy(x => x.Status, StringComparer.Ordinal).ThenBy(x => x.DisplayName, StringComparer.Ordinal).ToList()
                : source.OrderByDescending(x => x.Status, StringComparer.Ordinal).ThenBy(x => x.DisplayName, StringComparer.Ordinal).ToList(),
            _ => asc
                ? source.OrderBy(x => x.DisplayName, StringComparer.Ordinal).ThenBy(x => x.PartyId).ToList()
                : source.OrderByDescending(x => x.DisplayName, StringComparer.Ordinal).ThenBy(x => x.PartyId).ToList(),
        };
    }

    private static IReadOnlyList<PartyGridRow> OrderByMetric(
        IReadOnlyList<PartyGridRow> source,
        GridSortRequest sort,
        Dictionary<Guid, int> metrics) =>
        sort.Direction == "asc"
            ? source.OrderBy(x => metrics.GetValueOrDefault(x.PartyId)).ThenBy(x => x.PartyId).ToList()
            : source.OrderByDescending(x => metrics.GetValueOrDefault(x.PartyId)).ThenBy(x => x.PartyId).ToList();

    private async Task<Dictionary<Guid, int>> BuildOfferCountMetricsAsync(CancellationToken cancellationToken)
    {
        var rows = await _offers.CountActiveOffersBySellerAsync(cancellationToken);
        return rows.ToDictionary(x => x.Key, x => x.Value);
    }

    private async Task<Dictionary<Guid, int>> BuildOrderCountMetricsAsync(
        IReadOnlyList<Guid> sellerIds,
        CancellationToken cancellationToken)
    {
        var counts = await _orderCounts.GetCountsBySellerAsync(sellerIds, cancellationToken);
        return counts is Dictionary<Guid, int> dict
            ? dict
            : counts.ToDictionary(x => x.Key, x => x.Value);
    }

    private static HashSet<Guid> FilterMetrics(Dictionary<Guid, int> metrics, GridFilterRequest filter)
    {
        if (filter.Operator is "blank")
        {
            return [];
        }

        if (filter.Operator is "notBlank")
        {
            return metrics.Keys.ToHashSet();
        }

        if (!int.TryParse(filter.Value, out var n))
        {
            return [];
        }

        int? nTo = int.TryParse(filter.ValueTo, out var parsed) ? parsed : null;
        return metrics.Where(kv => NumberMatch(kv.Value, filter.Operator, n, nTo)).Select(kv => kv.Key).ToHashSet();
    }

    private static bool NumberMatch(int value, string op, int n, int? nTo) => op switch
    {
        "equals" => value == n,
        "notEqual" => value != n,
        "greaterThan" => value > n,
        "greaterThanOrEqual" => value >= n,
        "lessThan" => value < n,
        "lessThanOrEqual" => value <= n,
        "between" when nTo.HasValue => value >= n && value <= nTo.Value,
        _ => true,
    };

    private sealed record PartyGridRow(Guid PartyId, string DisplayName, string Status);
}
