using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Grid;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Reviews;
using Tooba.Reviews.Domain;
using Tooba.Reviews.Infrastructure.Persistence;

namespace Tooba.Host.Grid;

/// <summary>
/// پرس‌وجوی DB-native صف نظرات Pending Admin.
/// عنوان محصول برای filter/search از Catalog ID resolve می‌شود؛ enrich عنوان فقط روی صفحه.
/// </summary>
internal sealed class AdminReviewGridQueryEngine
{
    private readonly ReviewsDbContext _reviews;
    private readonly CatalogDbContext _catalog;
    private readonly ICatalogLookupGateway _catalogLookup;

    public AdminReviewGridQueryEngine(
        ReviewsDbContext reviews,
        CatalogDbContext catalog,
        ICatalogLookupGateway catalogLookup)
    {
        _reviews = reviews;
        _catalog = catalog;
        _catalogLookup = catalogLookup;
    }

    public async Task<GridPageResponse<AdminReviewItem>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<ProductReview> q = _reviews.Reviews.AsNoTracking()
            .Where(x => x.Status == ReviewStatus.Pending);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            var productIds = await ResolveProductIdsByTitleContainsAsync(term, cancellationToken);
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
                return AdminEfGridQuery.ApplyTextFilter(source, x => x.AuthorDisplayName, filter);
            case "product":
            {
                var ids = await ResolveProductIdsByTitleFilterAsync(filter, cancellationToken);
                return source.Where(x => ids.Contains(x.ProductId));
            }
            case "rating":
                return AdminEfGridQuery.ApplyIntFilter(source, x => x.Rating, filter);
            case "excerpt":
                return AdminEfGridQuery.ApplyTextFilter(source, x => x.Body, filter);
            case "verified":
                return ApplyVerifiedFilter(source, filter);
            case "status":
                return AdminEfGridQuery.ApplyEnumFilter(source, x => x.Status, filter);
            case "created":
                return AdminEfGridQuery.ApplyDateFilter(source, x => x.CreatedAt, filter);
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

    private async Task<HashSet<Guid>> ResolveProductIdsByTitleContainsAsync(
        string term,
        CancellationToken cancellationToken)
    {
        var ids = await _catalog.LocalizedTexts.AsNoTracking()
            .Where(t =>
                t.OwnerKind == CatalogLocalizedOwnerKind.Product
                && t.FieldKey == "name"
                && t.Value.ToLower().Contains(term.ToLower()))
            .Select(t => t.OwnerId)
            .Distinct()
            .ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }

    private async Task<HashSet<Guid>> ResolveProductIdsByTitleFilterAsync(
        GridFilterRequest filter,
        CancellationToken cancellationToken)
    {
        if (filter.Operator is "blank")
        {
            var withTitle = await _catalog.LocalizedTexts.AsNoTracking()
                .Where(t => t.OwnerKind == CatalogLocalizedOwnerKind.Product && t.FieldKey == "name" && t.Value != "")
                .Select(t => t.OwnerId)
                .Distinct()
                .ToListAsync(cancellationToken);
            var allProducts = await _catalog.Products.AsNoTracking().Select(p => p.ProductId).ToListAsync(cancellationToken);
            var blank = allProducts.Except(withTitle).ToHashSet();
            return (await _reviews.Reviews.AsNoTracking()
                .Where(x => x.Status == ReviewStatus.Pending && blank.Contains(x.ProductId))
                .Select(x => x.ProductId)
                .Distinct()
                .ToListAsync(cancellationToken)).ToHashSet();
        }

        if (filter.Operator is "notBlank")
        {
            return (await _catalog.LocalizedTexts.AsNoTracking()
                .Where(t => t.OwnerKind == CatalogLocalizedOwnerKind.Product && t.FieldKey == "name" && t.Value != "")
                .Select(t => t.OwnerId)
                .Distinct()
                .ToListAsync(cancellationToken)).ToHashSet();
        }

        var q = _catalog.LocalizedTexts.AsNoTracking()
            .Where(t => t.OwnerKind == CatalogLocalizedOwnerKind.Product && t.FieldKey == "name");
        var value = filter.Value ?? string.Empty;
        q = filter.Operator switch
        {
            "equals" => q.Where(t => t.Value.ToLower() == value.ToLower()),
            "notEqual" => q.Where(t => t.Value.ToLower() != value.ToLower()),
            "notContains" => q.Where(t => !t.Value.ToLower().Contains(value.ToLower())),
            "startsWith" => q.Where(t => t.Value.ToLower().StartsWith(value.ToLower())),
            "endsWith" => q.Where(t => t.Value.ToLower().EndsWith(value.ToLower())),
            _ => q.Where(t => t.Value.ToLower().Contains(value.ToLower())),
        };

        return (await q.Select(t => t.OwnerId).Distinct().ToListAsync(cancellationToken)).ToHashSet();
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
