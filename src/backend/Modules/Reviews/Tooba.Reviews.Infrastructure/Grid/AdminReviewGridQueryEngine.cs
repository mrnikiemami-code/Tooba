using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Grid;
using Tooba.Catalog.Application;
using Tooba.Catalog.Contracts;
using Tooba.Persistence.Grid;
using Tooba.Reviews.Application;
using Tooba.Reviews.Domain;
using Tooba.Reviews.Infrastructure.Persistence;

namespace Tooba.Reviews.Infrastructure.Grid;

/// <summary>
/// پرس‌وجوی DB-native صف نظرات Pending Admin.
/// عنوان محصول برای filter/search از Catalog Contracts resolve می‌شود؛ enrich عنوان فقط روی صفحه.
/// </summary>
public sealed class AdminReviewGridQueryEngine
{
    private readonly ReviewsDbContext _reviews;
    private readonly ICatalogAdminProductTitleIdLookup _productTitles;
    private readonly ICatalogLookupGateway _catalogLookup;

    /// <summary>موتور گرید نظرات Admin.</summary>
    public AdminReviewGridQueryEngine(
        ReviewsDbContext reviews,
        ICatalogAdminProductTitleIdLookup productTitles,
        ICatalogLookupGateway catalogLookup)
    {
        _reviews = reviews;
        _productTitles = productTitles;
        _catalogLookup = catalogLookup;
    }

    /// <summary>صفحه‌بندی DB-native گرید نظرات Pending.</summary>
    public async Task<GridPageResponse<AdminReviewItem>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<ProductReview> q = _reviews.Reviews.AsNoTracking()
            .Where(x => x.Status == ReviewStatus.Pending);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            var productIds = await _productTitles.ResolveProductIdsByTitleContainsAsync(term, cancellationToken);
            var lower = term.ToLower();
            q = q.Where(x =>
                x.AuthorDisplayName.ToLower().Contains(lower)
                || x.Body.ToLower().Contains(lower)
                || (x.Title != null && x.Title.ToLower().Contains(lower))
                || productIds.Contains(x.ProductId));
        }

        foreach (var filter in request.Filters)
        {
            q = await ApplyFilterAsync(q, filter, cancellationToken);
        }

        var advancedIds = await EvaluateAdvancedAsync(request.AdvancedFilter, cancellationToken);
        if (advancedIds is not null)
        {
            q = q.Where(x => advancedIds.Contains(x.ReviewId));
        }

        var sort = request.Sort.FirstOrDefault() ?? new GridSortRequest("created", "desc");
        return await EfGridQuery.PageAsync(
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

        var baseQ = _reviews.Reviews.AsNoTracking().Where(x => x.Status == ReviewStatus.Pending);
        var sets = new List<HashSet<Guid>>();
        foreach (var condition in expression.Conditions)
        {
            var filter = new GridFilterRequest(
                condition.Field,
                condition.Operator,
                condition.Value,
                condition.ValueTo,
                condition.Values);
            var filtered = await ApplyFilterAsync(baseQ, filter, cancellationToken);
            var ids = await filtered.Select(x => x.ReviewId).ToListAsync(cancellationToken);
            sets.Add(ids.ToHashSet());
        }

        return GridAdvancedFilterEvaluator.EvaluateLeftToRight(sets, expression.Connectors);
    }

    private async Task<IQueryable<ProductReview>> ApplyFilterAsync(
        IQueryable<ProductReview> source,
        GridFilterRequest filter,
        CancellationToken cancellationToken)
    {
        switch (filter.Field)
        {
            case "reviewer":
                return EfGridQuery.ApplyTextFilter(source, x => x.AuthorDisplayName, filter);
            case "product":
            {
                var ids = await _productTitles.ResolveProductIdsByTitleFilterAsync(
                    new CatalogProductTitleTextFilter(filter.Operator ?? string.Empty, filter.Value),
                    cancellationToken);
                return source.Where(x => ids.Contains(x.ProductId));
            }
            case "rating":
                return EfGridQuery.ApplyIntFilter(source, x => x.Rating, filter);
            case "excerpt":
                return EfGridQuery.ApplyTextFilter(source, x => x.Body, filter);
            case "verified":
                return ApplyVerifiedFilter(source, filter);
            case "status":
                return EfGridQuery.ApplyEnumFilter(source, x => x.Status, filter);
            case "created":
                return EfGridQuery.ApplyDateFilter(source, x => x.CreatedAt, filter);
            default:
                return source;
        }
    }

    private static IQueryable<ProductReview> ApplyVerifiedFilter(
        IQueryable<ProductReview> source,
        GridFilterRequest filter)
    {
        var op = (filter.Operator ?? string.Empty).Trim();
        if (op is "blank")
        {
            return source.Where(_ => false);
        }

        if (op is "notBlank")
        {
            return source;
        }

        var values = (filter.Values ?? [])
            .Concat(string.IsNullOrWhiteSpace(filter.Value) ? [] : [filter.Value!])
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .ToList();

        var bools = new List<bool>();
        foreach (var value in values)
        {
            if (bool.TryParse(value, out var b))
            {
                bools.Add(b);
            }
            else if (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase))
            {
                bools.Add(true);
            }
            else if (string.Equals(value, "0", StringComparison.OrdinalIgnoreCase))
            {
                bools.Add(false);
            }
        }

        if (bools.Count == 0)
        {
            return source.Where(_ => false);
        }

        return op switch
        {
            "notEqual" or "notIn" => source.Where(x => !bools.Contains(x.IsVerifiedPurchase)),
            _ => source.Where(x => bools.Contains(x.IsVerifiedPurchase)),
        };
    }

    private static IQueryable<ProductReview> Order(IQueryable<ProductReview> source, GridSortRequest sort)
    {
        var asc = sort.Direction == "asc";
        return sort.Field switch
        {
            "reviewer" => asc
                ? source.OrderBy(x => x.AuthorDisplayName).ThenByDescending(x => x.CreatedAt)
                : source.OrderByDescending(x => x.AuthorDisplayName).ThenByDescending(x => x.CreatedAt),
            "rating" => asc
                ? source.OrderBy(x => x.Rating).ThenBy(x => x.AuthorDisplayName)
                : source.OrderByDescending(x => x.Rating).ThenBy(x => x.AuthorDisplayName),
            "excerpt" => asc
                ? source.OrderBy(x => x.Body).ThenBy(x => x.AuthorDisplayName)
                : source.OrderByDescending(x => x.Body).ThenBy(x => x.AuthorDisplayName),
            "verified" => asc
                ? source.OrderBy(x => x.IsVerifiedPurchase).ThenBy(x => x.AuthorDisplayName)
                : source.OrderByDescending(x => x.IsVerifiedPurchase).ThenBy(x => x.AuthorDisplayName),
            "status" => asc
                ? source.OrderBy(x => x.Status).ThenBy(x => x.AuthorDisplayName)
                : source.OrderByDescending(x => x.Status).ThenBy(x => x.AuthorDisplayName),
            "product" => asc
                ? source.OrderBy(x => x.ProductId).ThenBy(x => x.AuthorDisplayName)
                : source.OrderByDescending(x => x.ProductId).ThenBy(x => x.AuthorDisplayName),
            _ => asc
                ? source.OrderBy(x => x.CreatedAt).ThenBy(x => x.AuthorDisplayName)
                : source.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.AuthorDisplayName),
        };
    }

    private async Task<IReadOnlyList<AdminReviewItem>> MapPageAsync(
        List<ProductReview> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var titles = await _catalogLookup.GetProductTitlesAsync(
            rows.Select(x => x.ProductId).Distinct().ToArray(),
            cancellationToken);

        return rows.Select(x => new AdminReviewItem(
            x.ReviewId,
            titles.GetValueOrDefault(x.ProductId) ?? "محصول",
            x.AuthorDisplayName,
            x.Rating,
            x.Title,
            x.Body,
            x.Status.ToString(),
            x.IsVerifiedPurchase,
            x.CreatedAt)).ToList();
    }
}
