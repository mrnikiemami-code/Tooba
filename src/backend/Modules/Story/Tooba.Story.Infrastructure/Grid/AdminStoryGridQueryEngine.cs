using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Grid;
using Tooba.Party.Contracts.Ports;
using Tooba.Persistence.Grid;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Domain.Aggregates;
using Tooba.Story.Domain.Enums;
using Tooba.Story.Infrastructure.Directories;
using Tooba.Story.Infrastructure.Persistence;
using StoryEntity = Tooba.Story.Domain.Aggregates.Story;

namespace Tooba.Story.Infrastructure.Grid;

/// <summary>پرس‌وجوی DB-native گرید استوری Admin؛ آیتم‌ها فقط برای صفحه enrich می‌شوند.</summary>
public sealed class AdminStoryGridQueryEngine
{
    private readonly StoryDbContext _db;
    private readonly IPartyAdminSellerReadGateway? _parties;

    /// <summary>موتور گرید استوری Admin.</summary>
    public AdminStoryGridQueryEngine(StoryDbContext db, IPartyAdminSellerReadGateway? parties = null)
    {
        _db = db;
        _parties = parties;
    }

    /// <summary>صفحه‌بندی DB-native گرید استوری.</summary>
    public async Task<GridPageResponse<AdminStorySnapshot>> QueryAsync(
        Guid tenantId,
        StoryReviewStatus? reviewStatus,
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<StoryEntity> q = _db.Stories.AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (reviewStatus.HasValue)
            q = q.Where(x => x.ReviewStatus == reviewStatus.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
            q = EfGridQuery.ApplySearchAny(q, request.Search, x => x.Title);

        // Shop human-name filter uses the complete Party projection for shops present on Story rows
        // (no truncated global Party search that would under-count).
        q = await ApplyShopNameFiltersAsync(q, request.Filters, cancellationToken);

        foreach (var filter in request.Filters.Where(f =>
                     !string.Equals(f.Field, "shopDisplayName", StringComparison.OrdinalIgnoreCase)
                     && !string.Equals(f.Field, "shopName", StringComparison.OrdinalIgnoreCase)))
        {
            q = ApplyFilter(q, filter);
        }

        var advancedIds = await EvaluateAdvancedAsync(tenantId, reviewStatus, request.AdvancedFilter, cancellationToken);
        if (advancedIds is not null)
            q = q.Where(x => advancedIds.Contains(x.StoryId));

        var sort = request.Sort.FirstOrDefault() ?? new GridSortRequest("displayOrder", "asc");
        return await EfGridQuery.PageAsync(
            q,
            request,
            filtered => Order(filtered, sort),
            MapPageAsync,
            cancellationToken);
    }

    private async Task<IQueryable<StoryEntity>> ApplyShopNameFiltersAsync(
        IQueryable<StoryEntity> source,
        IReadOnlyList<GridFilterRequest> filters,
        CancellationToken cancellationToken)
    {
        var shopFilters = filters
            .Where(f => string.Equals(f.Field, "shopDisplayName", StringComparison.OrdinalIgnoreCase)
                || string.Equals(f.Field, "shopName", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (shopFilters.Count == 0)
            return source;
        if (_parties is null)
            return source.Where(_ => false);

        var shopIds = await source
            .Where(x => x.SellerPartyId != null)
            .Select(x => x.SellerPartyId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (shopIds.Count == 0)
            return source.Where(_ => false);

        var projections = await _parties.GetStatusProjectionsAsync(shopIds, cancellationToken);
        HashSet<Guid>? matching = null;
        foreach (var filter in shopFilters)
        {
            var ids = MatchShopNames(projections, filter).ToHashSet();
            matching = matching is null ? ids : matching.Intersect(ids).ToHashSet();
        }

        matching ??= [];
        return source.Where(x => x.SellerPartyId != null && matching.Contains(x.SellerPartyId.Value));
    }

    private static IEnumerable<Guid> MatchShopNames(
        IReadOnlyList<PartyStatusProjection> projections,
        GridFilterRequest filter)
    {
        var op = (filter.Operator ?? "contains").Trim().ToLowerInvariant();
        var value = filter.Value?.Trim() ?? string.Empty;
        foreach (var row in projections)
        {
            var name = row.DisplayName ?? string.Empty;
            var hit = op switch
            {
                "eq" or "equals" => string.Equals(name, value, StringComparison.OrdinalIgnoreCase),
                "startswith" => name.StartsWith(value, StringComparison.OrdinalIgnoreCase),
                "endswith" => name.EndsWith(value, StringComparison.OrdinalIgnoreCase),
                _ => name.Contains(value, StringComparison.OrdinalIgnoreCase),
            };
            if (hit)
                yield return row.PartyId;
        }
    }

    private async Task<HashSet<Guid>?> EvaluateAdvancedAsync(
        Guid tenantId,
        StoryReviewStatus? reviewStatus,
        GridAdvancedFilterExpression? expression,
        CancellationToken cancellationToken)
    {
        if (expression?.Conditions is not { Count: > 0 })
            return null;

        IQueryable<StoryEntity> baseQ = _db.Stories.AsNoTracking().Where(x => x.TenantId == tenantId);
        if (reviewStatus.HasValue)
            baseQ = baseQ.Where(x => x.ReviewStatus == reviewStatus.Value);

        var sets = new List<HashSet<Guid>>();
        foreach (var condition in expression.Conditions)
        {
            var filter = new GridFilterRequest(
                condition.Field,
                condition.Operator,
                condition.Value,
                condition.ValueTo,
                condition.Values);
            IQueryable<StoryEntity> filtered = baseQ;
            if (string.Equals(filter.Field, "shopDisplayName", StringComparison.OrdinalIgnoreCase)
                || string.Equals(filter.Field, "shopName", StringComparison.OrdinalIgnoreCase))
            {
                filtered = await ApplyShopNameFiltersAsync(filtered, [filter], cancellationToken);
            }
            else
            {
                filtered = ApplyFilter(filtered, filter);
            }

            var ids = await filtered.Select(x => x.StoryId).ToListAsync(cancellationToken);
            sets.Add(ids.ToHashSet());
        }

        return GridAdvancedFilterEvaluator.EvaluateLeftToRight(sets, expression.Connectors);
    }

    private IQueryable<StoryEntity> ApplyFilter(IQueryable<StoryEntity> source, GridFilterRequest filter)
    {
        switch (filter.Field)
        {
            case "title":
                return EfGridQuery.ApplyTextFilter(source, x => x.Title, filter);
            case "status":
                return EfGridQuery.ApplyEnumFilter(source, x => x.Status, filter);
            case "reviewStatus":
                return EfGridQuery.ApplyEnumFilter(source, x => x.ReviewStatus, filter);
            case "origin":
                return EfGridQuery.ApplyEnumFilter(source, x => x.Origin, filter);
            case "locale":
                return EfGridQuery.ApplyTextFilter(source, x => x.Locale, filter);
            case "market":
                return EfGridQuery.ApplyTextFilter(source, x => x.Market, filter);
            case "displayOrder":
                return EfGridQuery.ApplyIntFilter(source, x => x.DisplayOrder, filter);
            case "sellerPartyId":
            {
                if (!Guid.TryParse(filter.Value, out var sellerId))
                    return source.Where(_ => false);
                return source.Where(x => x.SellerPartyId == sellerId);
            }
            case "items":
            {
                var counts = _db.StoryItems.AsNoTracking()
                    .GroupBy(x => x.StoryId)
                    .Select(g => new { StoryId = g.Key, Count = g.Count() });
                var joined = from s in source
                             join c in counts on s.StoryId equals c.StoryId into cj
                             from c in cj.DefaultIfEmpty()
                             select new { Story = s, Count = c != null ? c.Count : 0 };
                joined = EfGridQuery.ApplyIntFilter(joined, x => x.Count, filter);
                return joined.Select(x => x.Story);
            }
            default:
                return source;
        }
    }

    private IQueryable<StoryEntity> Order(IQueryable<StoryEntity> source, GridSortRequest sort)
    {
        var asc = sort.Direction == "asc";
        if (sort.Field == "items")
        {
            var counts = _db.StoryItems.AsNoTracking()
                .GroupBy(x => x.StoryId)
                .Select(g => new { StoryId = g.Key, Count = g.Count() });
            var joined = from s in source
                         join c in counts on s.StoryId equals c.StoryId into cj
                         from c in cj.DefaultIfEmpty()
                         select new { Story = s, Count = c != null ? c.Count : 0 };
            var ordered = asc
                ? joined.OrderBy(x => x.Count).ThenBy(x => x.Story.Title)
                : joined.OrderByDescending(x => x.Count).ThenBy(x => x.Story.Title);
            return ordered.Select(x => x.Story);
        }

        return sort.Field switch
        {
            "title" => asc
                ? source.OrderBy(x => x.Title).ThenBy(x => x.DisplayOrder)
                : source.OrderByDescending(x => x.Title).ThenBy(x => x.DisplayOrder),
            "status" => asc
                ? source.OrderBy(x => x.Status).ThenBy(x => x.Title)
                : source.OrderByDescending(x => x.Status).ThenBy(x => x.Title),
            "reviewStatus" => asc
                ? source.OrderBy(x => x.ReviewStatus).ThenBy(x => x.Title)
                : source.OrderByDescending(x => x.ReviewStatus).ThenBy(x => x.Title),
            "origin" => asc
                ? source.OrderBy(x => x.Origin).ThenBy(x => x.Title)
                : source.OrderByDescending(x => x.Origin).ThenBy(x => x.Title),
            "locale" => asc
                ? source.OrderBy(x => x.Locale).ThenBy(x => x.Title)
                : source.OrderByDescending(x => x.Locale).ThenBy(x => x.Title),
            "market" => asc
                ? source.OrderBy(x => x.Market).ThenBy(x => x.Title)
                : source.OrderByDescending(x => x.Market).ThenBy(x => x.Title),
            _ => asc
                ? source.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Title)
                : source.OrderByDescending(x => x.DisplayOrder).ThenBy(x => x.Title),
        };
    }

    private async Task<IReadOnlyList<AdminStorySnapshot>> MapPageAsync(
        List<StoryEntity> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
            return [];

        var ids = rows.Select(x => x.StoryId).ToList();
        var items = await _db.StoryItems.AsNoTracking()
            .Where(x => ids.Contains(x.StoryId))
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);
        var byStory = items.GroupBy(x => x.StoryId).ToDictionary(g => g.Key, g => (IReadOnlyList<StoryItem>)g.ToList());

        var mapped = rows.Select(story =>
        {
            story.AttachItems(byStory.GetValueOrDefault(story.StoryId, []));
            return StoryDirectory.MapAdmin(story, isAdmin: true);
        }).ToList();

        if (_parties is null)
            return mapped;

        var shops = mapped
            .Where(row => row.SellerPartyId is Guid id && id != Guid.Empty)
            .Select(row => row.SellerPartyId!.Value)
            .Distinct()
            .ToList();
        if (shops.Count == 0)
            return mapped;

        var projections = await _parties.GetStatusProjectionsAsync(shops, cancellationToken);
        var names = projections.ToDictionary(row => row.PartyId, row => row.DisplayName);
        return mapped.Select(row =>
            row.SellerPartyId is Guid shop && names.TryGetValue(shop, out var name)
                ? row with { ShopDisplayName = name }
                : row).ToList();
    }
}
