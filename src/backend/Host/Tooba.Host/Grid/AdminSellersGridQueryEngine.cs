using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Grid;
using Tooba.Host.Admin;
using Tooba.Offer.Domain;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Party.Domain;
using Tooba.Party.Infrastructure.Persistence;

namespace Tooba.Host.Grid;

/// <summary>
/// پرس‌وجوی فروشندگان Admin.
/// Party scalars در SQL؛ شمارنده‌های Offer/Order جدا (بدون JOIN بین schema).
/// مرتب‌سازی name/status در Party SQL؛ مرتب‌سازی offers/orders با الگوی محصول (IDهای فیلترشده سپس sort در حافظه).
/// </summary>
internal sealed class AdminSellersGridQueryEngine
{
    private readonly OfferDbContext _offers;
    private readonly PartyDbContext _parties;
    private readonly OrderDbContext _orders;

    public AdminSellersGridQueryEngine(
        OfferDbContext offers,
        PartyDbContext parties,
        OrderDbContext orders)
    {
        _offers = offers;
        _parties = parties;
        _orders = orders;
    }

    public async Task<GridPageResponse<AdminSellerListItem>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        var sellerIds = await _offers.Offers.AsNoTracking()
            .Select(x => x.SellerPartyId)
            .Distinct()
            .ToListAsync(cancellationToken);

        IQueryable<BusinessParty> parties = _parties.Parties.AsNoTracking()
            .Where(p => sellerIds.Contains(p.PartyId));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            parties = AdminEfGridQuery.ApplySearchAny(parties, request.Search, x => x.DisplayName);
        }

        foreach (var filter in request.Filters.Where(f => f.Field is "name" or "status"))
        {
            parties = ApplyPartyFilter(parties, filter);
        }

        var offerMetrics = await BuildOfferCountMetricsAsync(cancellationToken);
        var orderMetrics = await BuildOrderCountMetricsAsync(sellerIds, cancellationToken);

        foreach (var filter in request.Filters.Where(f => f.Field is "offers" or "orders"))
        {
            var ids = filter.Field == "offers"
                ? FilterMetrics(offerMetrics, filter)
                : FilterMetrics(orderMetrics, filter);
            parties = parties.Where(p => ids.Contains(p.PartyId));
        }

        var advancedIds = await EvaluateAdvancedAsync(
            sellerIds,
            offerMetrics,
            orderMetrics,
            request.AdvancedFilter,
            cancellationToken);
        if (advancedIds is not null)
        {
            parties = parties.Where(p => advancedIds.Contains(p.PartyId));
        }

        var total = await parties.CountAsync(cancellationToken);
        if (total == 0)
        {
            return new GridPageResponse<AdminSellerListItem>([], request.Page, request.PageSize, 0);
        }

        var sort = request.Sort.FirstOrDefault() ?? new GridSortRequest("name", "asc");
        List<BusinessParty> pageParties;
        if (sort.Field is "offers" or "orders")
        {
            // Same compromise as AdminProductGridQueryEngine.OrderAndPageByMetricAsync:
            // load filtered IDs then metric-sort in memory (cross-module JOIN forbidden).
            var filteredIds = await parties.Select(p => p.PartyId).ToListAsync(cancellationToken);
            var metrics = sort.Field == "offers" ? offerMetrics : orderMetrics;
            IEnumerable<Guid> orderedIds = sort.Direction == "asc"
                ? filteredIds.OrderBy(id => metrics.GetValueOrDefault(id)).ThenBy(id => id)
                : filteredIds.OrderByDescending(id => metrics.GetValueOrDefault(id)).ThenBy(id => id);
            var pageIds = orderedIds
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();
            var loaded = await _parties.Parties.AsNoTracking()
                .Where(p => pageIds.Contains(p.PartyId))
                .ToListAsync(cancellationToken);
            var byId = loaded.ToDictionary(x => x.PartyId);
            pageParties = pageIds.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
        }
        else
        {
            var ordered = OrderParty(parties, sort);
            pageParties = await ordered
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);
        }

        var items = pageParties.Select(party => new AdminSellerListItem(
            party.PartyId,
            party.DisplayName,
            party.Status.ToString(),
            offerMetrics.GetValueOrDefault(party.PartyId),
            orderMetrics.GetValueOrDefault(party.PartyId))).ToList();

        return new GridPageResponse<AdminSellerListItem>(items, request.Page, request.PageSize, total);
    }

    private async Task<HashSet<Guid>?> EvaluateAdvancedAsync(
        IReadOnlyList<Guid> sellerIds,
        Dictionary<Guid, int> offerMetrics,
        Dictionary<Guid, int> orderMetrics,
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
                var q = ApplyPartyFilter(
                    _parties.Parties.AsNoTracking().Where(p => sellerIds.Contains(p.PartyId)),
                    filter);
                ids = (await q.Select(p => p.PartyId).ToListAsync(cancellationToken)).ToHashSet();
            }

            sets.Add(ids);
        }

        return GridAdvancedFilterEvaluator.EvaluateLeftToRight(sets, expression.Connectors);
    }

    private static IQueryable<BusinessParty> ApplyPartyFilter(
        IQueryable<BusinessParty> source,
        GridFilterRequest filter) =>
        filter.Field switch
        {
            "name" => AdminEfGridQuery.ApplyTextFilter(source, x => x.DisplayName, filter),
            "status" => AdminEfGridQuery.ApplyEnumFilter(source, x => x.Status, filter),
            _ => source,
        };

    private static IQueryable<BusinessParty> OrderParty(IQueryable<BusinessParty> source, GridSortRequest sort)
    {
        var asc = sort.Direction == "asc";
        return sort.Field switch
        {
            "status" => asc
                ? source.OrderBy(x => x.Status).ThenBy(x => x.DisplayName)
                : source.OrderByDescending(x => x.Status).ThenBy(x => x.DisplayName),
            _ => asc
                ? source.OrderBy(x => x.DisplayName).ThenBy(x => x.PartyId)
                : source.OrderByDescending(x => x.DisplayName).ThenBy(x => x.PartyId),
        };
    }

    private async Task<Dictionary<Guid, int>> BuildOfferCountMetricsAsync(CancellationToken cancellationToken)
    {
        var rows = await _offers.Offers.AsNoTracking()
            .Where(x => x.Status == OfferStatus.Active)
            .GroupBy(x => x.SellerPartyId)
            .Select(g => new { SellerPartyId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(x => x.SellerPartyId, x => x.Count);
    }

    private async Task<Dictionary<Guid, int>> BuildOrderCountMetricsAsync(
        IReadOnlyList<Guid> sellerIds,
        CancellationToken cancellationToken)
    {
        if (sellerIds.Count == 0)
        {
            return [];
        }

        var rows = await _orders.SellerOrders.AsNoTracking()
            .Where(x => sellerIds.Contains(x.SellerPartyId))
            .GroupBy(x => x.SellerPartyId)
            .Select(g => new { SellerPartyId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(x => x.SellerPartyId, x => x.Count);
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
}
