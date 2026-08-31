using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Grid;
using Tooba.Content.Application;
using Tooba.Content.Domain;
using Tooba.Content.Infrastructure.Persistence;

namespace Tooba.Host.Grid;

/// <summary>پرس‌وجوی DB-native گرید مقالات Admin — CountAsync + Skip/Take قبل از materialize.</summary>
internal sealed class AdminContentGridQueryEngine
{
    private readonly ContentDbContext _db;

    public AdminContentGridQueryEngine(ContentDbContext db) => _db = db;

    public async Task<GridPageResponse<AdminArticleSnapshot>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<ContentArticle> q = _db.Articles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            q = AdminEfGridQuery.ApplySearchAny(q, request.Search, x => x.Title, x => x.Slug, x => x.Category);
        }

        foreach (var filter in request.Filters)
        {
            q = ApplyFilter(q, filter);
        }

        var advancedIds = await EvaluateAdvancedAsync(request.AdvancedFilter, cancellationToken);
        if (advancedIds is not null)
        {
            q = q.Where(x => advancedIds.Contains(x.ArticleId));
        }

        var sort = request.Sort.FirstOrDefault() ?? new GridSortRequest("updated", "desc");
        return await AdminEfGridQuery.PageAsync(
            q,
            request,
            filtered => Order(filtered, sort),
            (rows, _) => Task.FromResult<IReadOnlyList<AdminArticleSnapshot>>(rows.Select(MapAdmin).ToList()),
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
            var ids = await ApplyFilter(_db.Articles.AsNoTracking(), filter)
                .Select(x => x.ArticleId)
                .ToListAsync(cancellationToken);
            sets.Add(ids.ToHashSet());
        }

        return GridAdvancedFilterEvaluator.EvaluateLeftToRight(sets, expression.Connectors);
    }

    private static IQueryable<ContentArticle> ApplyFilter(IQueryable<ContentArticle> source, GridFilterRequest filter) =>
        filter.Field switch
        {
            "title" => AdminEfGridQuery.ApplyTextFilter(source, x => x.Title, filter),
            "slug" => AdminEfGridQuery.ApplyTextFilter(source, x => x.Slug, filter),
            "category" => AdminEfGridQuery.ApplyTextFilter(source, x => x.Category, filter),
            "status" => AdminEfGridQuery.ApplyEnumFilter(source, x => x.Status, filter),
            "updated" => AdminEfGridQuery.ApplyDateFilter(source, x => x.UpdatedAt, filter),
            _ => source,
        };

    private static IOrderedQueryable<ContentArticle> Order(IQueryable<ContentArticle> source, GridSortRequest sort)
    {
        var asc = sort.Direction == "asc";
        return sort.Field switch
        {
            "title" => asc
                ? source.OrderBy(x => x.Title).ThenByDescending(x => x.UpdatedAt)
                : source.OrderByDescending(x => x.Title).ThenByDescending(x => x.UpdatedAt),
            "slug" => asc
                ? source.OrderBy(x => x.Slug).ThenBy(x => x.Title)
                : source.OrderByDescending(x => x.Slug).ThenBy(x => x.Title),
            "status" => asc
                ? source.OrderBy(x => x.Status).ThenBy(x => x.Title)
                : source.OrderByDescending(x => x.Status).ThenBy(x => x.Title),
            "category" => asc
                ? source.OrderBy(x => x.Category).ThenBy(x => x.Title)
                : source.OrderByDescending(x => x.Category).ThenBy(x => x.Title),
            _ => asc
                ? source.OrderBy(x => x.UpdatedAt).ThenBy(x => x.Title)
                : source.OrderByDescending(x => x.UpdatedAt).ThenBy(x => x.Title),
        };
    }

    private static AdminArticleSnapshot MapAdmin(ContentArticle article) => new(
        article.ArticleId,
        article.Slug,
        article.Title,
        article.Excerpt,
        article.Body,
        article.Locale,
        article.SeoTitle,
        article.SeoDescription,
        article.Category,
        article.CoverMediaAssetId,
        article.AuthorDisplayName,
        string.IsNullOrWhiteSpace(article.TagsCsv)
            ? []
            : article.TagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        article.IsFeatured,
        article.Status,
        article.PublishDate,
        article.CreatedAt,
        article.UpdatedAt);
}
