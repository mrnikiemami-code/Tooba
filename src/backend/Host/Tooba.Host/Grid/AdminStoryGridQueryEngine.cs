using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Grid;
using Tooba.Story.Application;
using Tooba.Story.Domain;
using Tooba.Story.Infrastructure.Persistence;
using StoryEntity = Tooba.Story.Domain.Story;

namespace Tooba.Host.Grid;

/// <summary>پرس‌وجوی DB-native گرید استوری Admin؛ آیتم‌ها فقط برای صفحه enrich می‌شوند.</summary>
internal sealed class AdminStoryGridQueryEngine
{
    private readonly StoryDbContext _db;

    public AdminStoryGridQueryEngine(StoryDbContext db) => _db = db;

    public async Task<GridPageResponse<AdminStorySnapshot>> QueryAsync(
        Guid tenantId,
        StoryReviewStatus? reviewStatus,
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<StoryEntity> q = _db.Stories.AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (reviewStatus.HasValue)
        {
            q = q.Where(x => x.ReviewStatus == reviewStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            q = AdminEfGridQuery.ApplySearchAny(q, request.Search, x => x.Title);
        }

        foreach (var filter in request.Filters)
        {
            q = ApplyFilter(q, filter);
        }

        var advancedIds = await EvaluateAdvancedAsync(tenantId, reviewStatus, request.AdvancedFilter, cancellationToken);
        if (advancedIds is not null)
        {
            q = q.Where(x => advancedIds.Contains(x.StoryId));
        }

        var sort = request.Sort.FirstOrDefault() ?? new GridSortRequest("displayOrder", "asc");
        return await AdminEfGridQuery.PageAsync(
            q,
            request,
            filtered => Order(filtered, sort),
            MapPageAsync,
            cancellationToken);
    }

    private async Task<HashSet<Guid>?> EvaluateAdvancedAsync(
        Guid tenantId,
        StoryReviewStatus? reviewStatus,
        GridAdvancedFilterExpression? expression,
        CancellationToken cancellationToken)
    {
        if (expression?.Conditions is not { Count: > 0 })
        {
            return null;
        }

        IQueryable<StoryEntity> baseQ = _db.Stories.AsNoTracking().Where(x => x.TenantId == tenantId);
        if (reviewStatus.HasValue)
        {
            baseQ = baseQ.Where(x => x.ReviewStatus == reviewStatus.Value);
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
            var ids = await ApplyFilter(baseQ, filter).Select(x => x.StoryId).ToListAsync(cancellationToken);
            sets.Add(ids.ToHashSet());
        }

        return GridAdvancedFilterEvaluator.EvaluateLeftToRight(sets, expression.Connectors);
    }

    private IQueryable<StoryEntity> ApplyFilter(IQueryable<StoryEntity> source, GridFilterRequest filter)
    {
        switch (filter.Field)
        {
            case "title":
                return AdminEfGridQuery.ApplyTextFilter(source, x => x.Title, filter);
            case "status":
                return AdminEfGridQuery.ApplyEnumFilter(source, x => x.Status, filter);
            case "reviewStatus":
                return AdminEfGridQuery.ApplyEnumFilter(source, x => x.ReviewStatus, filter);
            case "origin":
                return AdminEfGridQuery.ApplyEnumFilter(source, x => x.Origin, filter);
            case "locale":
                return AdminEfGridQuery.ApplyTextFilter(source, x => x.Locale, filter);
            case "market":
                return AdminEfGridQuery.ApplyTextFilter(source, x => x.Market, filter);
            case "displayOrder":
                return AdminEfGridQuery.ApplyIntFilter(source, x => x.DisplayOrder, filter);
            case "items":
            {
                var counts = _db.StoryItems.AsNoTracking()
                    .GroupBy(x => x.StoryId)
                    .Select(g => new { StoryId = g.Key, Count = g.Count() });
                var joined = from s in source
                             join c in counts on s.StoryId equals c.StoryId into cj
                             from c in cj.DefaultIfEmpty()
                             select new { Story = s, Count = c != null ? c.Count : 0 };
                joined = AdminEfGridQuery.ApplyIntFilter(joined, x => x.Count, filter);
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
        {
            return [];
        }

        var ids = rows.Select(x => x.StoryId).ToList();
        var items = await _db.StoryItems.AsNoTracking()
            .Where(x => ids.Contains(x.StoryId))
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);
        var byStory = items.GroupBy(x => x.StoryId).ToDictionary(g => g.Key, g => (IReadOnlyList<StoryItem>)g.ToList());

        return rows.Select(story =>
        {
            story.AttachItems(byStory.GetValueOrDefault(story.StoryId, []));
            return MapAdmin(story);
        }).ToList();
    }

    private static AdminStorySnapshot MapAdmin(StoryEntity story) => new(
        story.StoryId,
        story.TenantId,
        story.Origin,
        story.SellerPartyId,
        story.ReviewStatus,
        story.SubmittedByActorUserId,
        story.ReviewedByActorUserId,
        story.SubmittedAt,
        story.ReviewedAt,
        story.RejectionReason,
        story.Locale,
        story.Market,
        story.Title,
        story.CoverMediaAssetId,
        story.CoverMediaUrl,
        story.DisplayOrder,
        story.StartAt,
        story.EndAt,
        story.Status,
        story.CtaType,
        story.CtaTarget,
        story.VersionToken,
        story.CreatedAt,
        story.UpdatedAt,
        story.Items
            .OrderBy(item => item.DisplayOrder)
            .Select(item => new AdminStoryItemSnapshot(
                item.StoryItemId,
                item.DisplayOrder,
                item.MediaType,
                item.MediaAssetId,
                item.MediaUrl,
                item.Caption,
                item.DurationMs,
                item.CtaType,
                item.CtaTarget,
                item.CreatedAt,
                item.UpdatedAt))
            .ToList());
}
