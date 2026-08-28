using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Pricing.Infrastructure.Persistence;

namespace Tooba.Host.Grid;

/// <summary>
/// پرس‌وجوی گرید محصول Admin با فیلتر/مرتب‌سازی/صفحه‌بندی SQL و تجمیع ماژولی — enrich فقط روی صفحهٔ نهایی.
/// </summary>
internal sealed class AdminProductGridQueryEngine
{
    private readonly CatalogDbContext _catalog;
    private readonly OfferDbContext _offers;
    private readonly PricingDbContext _prices;
    private readonly InventoryDbContext _inventory;

    public AdminProductGridQueryEngine(
        CatalogDbContext catalog,
        OfferDbContext offers,
        PricingDbContext prices,
        InventoryDbContext inventory)
    {
        _catalog = catalog;
        _offers = offers;
        _prices = prices;
        _inventory = inventory;
    }

    public async Task<(IReadOnlyList<Guid> PageIds, int TotalCount)> ResolvePageProductIdsAsync(
        GridQueryRequest query,
        CancellationToken cancellationToken)
    {
        var products = _catalog.Products.AsNoTracking();
        products = ApplyCatalogScalarFilters(products, query);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchIds = await ResolveSearchProductIdsAsync(query.Search.Trim(), cancellationToken);
            products = products.Where(p => searchIds.Contains(p.ProductId));
        }

        foreach (var filter in query.Filters)
        {
            if (ShouldSkipFilter(filter))
            {
                continue;
            }

            if (filter.Field is "status" or "updatedAt")
            {
                continue;
            }

            var ids = await ResolveFilterProductIdsAsync(filter, cancellationToken);
            products = products.Where(p => ids.Contains(p.ProductId));
        }

        var advancedIds = await EvaluateAdvancedFilterAsync(query.AdvancedFilter, cancellationToken);
        if (advancedIds is not null)
        {
            products = products.Where(p => advancedIds.Contains(p.ProductId));
        }

        var total = await products.CountAsync(cancellationToken);
        if (total == 0)
        {
            return ([], 0);
        }

        var primarySort = query.Sort.FirstOrDefault(s => !string.Equals(s.Field, "productId", StringComparison.Ordinal))
            ?? new GridSortRequest("updatedAt", "desc");

        var pageIds = primarySort.Field switch
        {
            "title" => await OrderAndPageTitleAsync(products, query, primarySort, cancellationToken),
            "categorySummary" => await OrderAndPageCategorySummaryAsync(products, query, primarySort, cancellationToken),
            "variantCount" => await OrderAndPageVariantCountAsync(products, query, primarySort, cancellationToken),
            "offerCount" => await OrderAndPageByMetricAsync(products, query, primarySort, await BuildOfferCountMetricsAsync(cancellationToken), cancellationToken),
            "sellableUnits" => await OrderAndPageByMetricAsync(products, query, primarySort, await BuildSellableUnitsMetricsAsync(cancellationToken), cancellationToken),
            "locationCount" => await OrderAndPageByMetricAsync(products, query, primarySort, await BuildLocationCountMetricsAsync(cancellationToken), cancellationToken),
            "status" or "updatedAt" => await OrderAndPageCatalogFieldsAsync(products, query, primarySort, cancellationToken),
            _ => await OrderAndPageCatalogFieldsAsync(products, query, new GridSortRequest("updatedAt", "desc"), cancellationToken),
        };

        return (pageIds, total);
    }

    private async Task<HashSet<Guid>?> EvaluateAdvancedFilterAsync(
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
            sets.Add(await ResolveFilterProductIdsAsync(filter, cancellationToken));
        }

        return AdminProductGridAdvancedFilterEvaluator.EvaluateLeftToRight(sets, expression.Connectors);
    }

    private async Task<HashSet<Guid>> ResolveFilterProductIdsAsync(
        GridFilterRequest filter,
        CancellationToken cancellationToken)
    {
        if (filter.Field is "status" or "updatedAt")
        {
            var products = _catalog.Products.AsNoTracking();
            products = filter.Field switch
            {
                "status" => ApplyStatusFilter(products, filter),
                "updatedAt" => ApplyUpdatedAtFilter(products, filter),
                _ => products,
            };
            return (await products.Select(p => p.ProductId).ToListAsync(cancellationToken)).ToHashSet();
        }

        var ids = filter.Field switch
        {
            "title" => await ResolveTitleProductIdsAsync(filter, cancellationToken),
            "variantCount" => await ResolveVariantCountProductIdsAsync(filter, cancellationToken),
            "offerCount" => await ResolveOfferCountProductIdsAsync(filter, cancellationToken),
            "sellableUnits" => await ResolveSellableUnitsProductIdsAsync(filter, cancellationToken),
            "locationCount" => await ResolveLocationCountProductIdsAsync(filter, cancellationToken),
            "categorySummary" => await ResolveCategorySummaryProductIdsAsync(filter, cancellationToken),
            "offerAmountRange" => await ResolveOfferAmountRangeProductIdsAsync(filter, cancellationToken),
            _ => new HashSet<Guid>(),
        };

        return ids;
    }

    private static bool ShouldSkipFilter(GridFilterRequest filter) =>
        filter.Operator is "notBlank" && filter.Field is "variantCount" or "offerCount" or "sellableUnits" or "locationCount";

    private static IQueryable<CatalogProduct> ApplyCatalogScalarFilters(IQueryable<CatalogProduct> products, GridQueryRequest query)
    {
        foreach (var filter in query.Filters.Where(f => f.Field is "status" or "updatedAt"))
        {
            products = filter.Field switch
            {
                "status" => ApplyStatusFilter(products, filter),
                "updatedAt" => ApplyUpdatedAtFilter(products, filter),
                _ => products,
            };
        }

        return products;
    }

    private static IQueryable<CatalogProduct> ApplyStatusFilter(IQueryable<CatalogProduct> products, GridFilterRequest filter)
    {
        if (filter.Operator is "blank") return products.Where(_ => false);
        if (filter.Operator is "notBlank") return products;

        if (filter.Operator is "in" or "notIn")
        {
            var statuses = (filter.Values ?? [])
                .Select(v => Enum.TryParse<CatalogPublicationStatus>(v, true, out var s) ? s : (CatalogPublicationStatus?)null)
                .Where(s => s.HasValue)
                .Select(s => s!.Value)
                .ToList();
            return filter.Operator == "in"
                ? products.Where(p => statuses.Contains(p.Status))
                : products.Where(p => !statuses.Contains(p.Status));
        }

        if (!Enum.TryParse<CatalogPublicationStatus>(filter.Value, true, out var status))
        {
            return products;
        }

        return filter.Operator switch
        {
            "equals" => products.Where(p => p.Status == status),
            "notEqual" => products.Where(p => p.Status != status),
            _ => products,
        };
    }

    private static IQueryable<CatalogProduct> ApplyUpdatedAtFilter(IQueryable<CatalogProduct> products, GridFilterRequest filter)
    {
        if (filter.Operator is "blank" or "notBlank") return products;

        if (!DateTimeOffset.TryParse(filter.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date))
        {
            return products;
        }

        var dayStart = new DateTimeOffset(date.UtcDateTime.Date, TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);
        return filter.Operator switch
        {
            "on" => products.Where(p => p.UpdatedAt >= dayStart && p.UpdatedAt < dayEnd),
            "before" => products.Where(p => p.UpdatedAt < dayStart),
            "after" => products.Where(p => p.UpdatedAt >= dayEnd),
            "between" when DateTimeOffset.TryParse(filter.ValueTo, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var toDate)
                => products.Where(p => p.UpdatedAt >= dayStart && p.UpdatedAt < new DateTimeOffset(toDate.UtcDateTime.Date, TimeSpan.Zero).AddDays(1)),
            _ => products,
        };
    }

    private async Task<HashSet<Guid>> ResolveSearchProductIdsAsync(string term, CancellationToken cancellationToken)
    {
        var titleIds = await _catalog.LocalizedTexts.AsNoTracking()
            .Where(t => t.OwnerKind == CatalogLocalizedOwnerKind.Product && t.FieldKey == "name" && EF.Functions.ILike(t.Value, $"%{term}%"))
            .Select(t => t.OwnerId)
            .ToListAsync(cancellationToken);

        var categoryIds = await _catalog.LocalizedTexts.AsNoTracking()
            .Where(t => t.OwnerKind == CatalogLocalizedOwnerKind.Category && t.FieldKey == "name" && EF.Functions.ILike(t.Value, $"%{term}%"))
            .Select(t => t.OwnerId)
            .ToListAsync(cancellationToken);

        var categoryProductIds = categoryIds.Count == 0
            ? []
            : await _catalog.ProductCategories.AsNoTracking()
                .Where(link => categoryIds.Contains(link.CategoryId))
                .Select(link => link.ProductId)
                .ToListAsync(cancellationToken);

        return titleIds.Concat(categoryProductIds).ToHashSet();
    }

    private async Task<HashSet<Guid>> ResolveTitleProductIdsAsync(GridFilterRequest filter, CancellationToken cancellationToken)
    {
        if (filter.Operator is "blank")
        {
            var withTitle = await _catalog.LocalizedTexts.AsNoTracking()
                .Where(t => t.OwnerKind == CatalogLocalizedOwnerKind.Product && t.FieldKey == "name" && t.Value != "")
                .Select(t => t.OwnerId)
                .Distinct()
                .ToListAsync(cancellationToken);
            var all = await _catalog.Products.AsNoTracking().Select(p => p.ProductId).ToListAsync(cancellationToken);
            return all.Except(withTitle).ToHashSet();
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

        q = filter.Operator switch
        {
            "equals" => q.Where(t => t.Value.ToLower() == (filter.Value ?? string.Empty).ToLower()),
            "notEqual" => q.Where(t => t.Value.ToLower() != (filter.Value ?? string.Empty).ToLower()),
            "contains" => q.Where(t => EF.Functions.ILike(t.Value, $"%{filter.Value}%")),
            "notContains" => q.Where(t => !EF.Functions.ILike(t.Value, $"%{filter.Value}%")),
            "startsWith" => q.Where(t => EF.Functions.ILike(t.Value, $"{filter.Value}%")),
            "endsWith" => q.Where(t => EF.Functions.ILike(t.Value, $"%{filter.Value}")),
            _ => q,
        };

        return (await q.Select(t => t.OwnerId).Distinct().ToListAsync(cancellationToken)).ToHashSet();
    }

    private async Task<HashSet<Guid>> ResolveVariantCountProductIdsAsync(GridFilterRequest filter, CancellationToken cancellationToken)
    {
        if (filter.Operator is "blank") return [];
        if (filter.Operator is "notBlank")
        {
            return (await _catalog.Variants.AsNoTracking().Select(v => v.ProductId).Distinct().ToListAsync(cancellationToken)).ToHashSet();
        }

        if (!int.TryParse(filter.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
        {
            return [];
        }

        int? nTo = int.TryParse(filter.ValueTo, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
        var q = _catalog.Variants.AsNoTracking().GroupBy(v => v.ProductId).Select(g => new { ProductId = g.Key, Count = g.Count() });
        q = filter.Operator switch
        {
            "equals" => q.Where(x => x.Count == n),
            "notEqual" => q.Where(x => x.Count != n),
            "greaterThan" => q.Where(x => x.Count > n),
            "greaterThanOrEqual" => q.Where(x => x.Count >= n),
            "lessThan" => q.Where(x => x.Count < n),
            "lessThanOrEqual" => q.Where(x => x.Count <= n),
            "between" when nTo.HasValue => q.Where(x => x.Count >= n && x.Count <= nTo.Value),
            _ => q,
        };

        return (await q.Select(x => x.ProductId).ToListAsync(cancellationToken)).ToHashSet();
    }

    private async Task<Dictionary<Guid, Guid>> LoadVariantToProductMapAsync(CancellationToken cancellationToken) =>
        await _catalog.Variants.AsNoTracking().ToDictionaryAsync(v => v.VariantId, v => v.ProductId, cancellationToken);

    private async Task<HashSet<Guid>> ResolveOfferCountProductIdsAsync(GridFilterRequest filter, CancellationToken cancellationToken) =>
        FilterMetrics(await BuildOfferCountMetricsAsync(cancellationToken), filter);

    private async Task<Dictionary<Guid, int>> BuildOfferCountMetricsAsync(CancellationToken cancellationToken)
    {
        var variantToProduct = await LoadVariantToProductMapAsync(cancellationToken);
        var grouped = await _offers.Offers.AsNoTracking()
            .GroupBy(o => o.CatalogVariantId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var metrics = new Dictionary<Guid, int>();
        foreach (var row in grouped)
        {
            if (!variantToProduct.TryGetValue(row.Key, out var productId))
            {
                continue;
            }

            metrics[productId] = metrics.GetValueOrDefault(productId) + row.Count;
        }

        return metrics;
    }

    private async Task<HashSet<Guid>> ResolveSellableUnitsProductIdsAsync(GridFilterRequest filter, CancellationToken cancellationToken) =>
        FilterMetrics(await BuildSellableUnitsMetricsAsync(cancellationToken), filter);

    private async Task<Dictionary<Guid, int>> BuildSellableUnitsMetricsAsync(CancellationToken cancellationToken)
    {
        var variantToProduct = await LoadVariantToProductMapAsync(cancellationToken);
        var offerToVariant = await _offers.Offers.AsNoTracking()
            .Select(o => new { o.OfferId, o.CatalogVariantId })
            .ToDictionaryAsync(x => x.OfferId, x => x.CatalogVariantId, cancellationToken);
        var positions = await _inventory.Positions.AsNoTracking()
            .Select(p => new { p.OfferId, Units = p.OnHand - p.Reserved })
            .ToListAsync(cancellationToken);

        var metrics = new Dictionary<Guid, int>();
        foreach (var pos in positions)
        {
            if (!offerToVariant.TryGetValue(pos.OfferId, out var variantId) || !variantToProduct.TryGetValue(variantId, out var productId))
            {
                continue;
            }

            metrics[productId] = metrics.GetValueOrDefault(productId) + pos.Units;
        }

        return metrics;
    }

    private async Task<HashSet<Guid>> ResolveLocationCountProductIdsAsync(GridFilterRequest filter, CancellationToken cancellationToken) =>
        FilterMetrics(await BuildLocationCountMetricsAsync(cancellationToken), filter);

    private async Task<Dictionary<Guid, int>> BuildLocationCountMetricsAsync(CancellationToken cancellationToken)
    {
        var variantToProduct = await LoadVariantToProductMapAsync(cancellationToken);
        var offerToVariant = await _offers.Offers.AsNoTracking()
            .Select(o => new { o.OfferId, o.CatalogVariantId })
            .ToDictionaryAsync(x => x.OfferId, x => x.CatalogVariantId, cancellationToken);
        var positions = await _inventory.Positions.AsNoTracking()
            .Select(p => new { p.OfferId, p.LocationId })
            .ToListAsync(cancellationToken);

        var locSets = new Dictionary<Guid, HashSet<Guid>>();
        foreach (var pos in positions)
        {
            if (!offerToVariant.TryGetValue(pos.OfferId, out var variantId) || !variantToProduct.TryGetValue(variantId, out var productId))
            {
                continue;
            }

            if (!locSets.TryGetValue(productId, out var set))
            {
                set = [];
                locSets[productId] = set;
            }

            set.Add(pos.LocationId);
        }

        return locSets.ToDictionary(kv => kv.Key, kv => kv.Value.Count);
    }

    private async Task<HashSet<Guid>> ResolveCategorySummaryProductIdsAsync(GridFilterRequest filter, CancellationToken cancellationToken)
    {
        if (filter.Operator is "blank")
        {
            var withCategory = await _catalog.ProductCategories.AsNoTracking().Select(x => x.ProductId).Distinct().ToListAsync(cancellationToken);
            var all = await _catalog.Products.AsNoTracking().Select(p => p.ProductId).ToListAsync(cancellationToken);
            return all.Except(withCategory).ToHashSet();
        }

        if (filter.Operator is "notBlank")
        {
            return (await _catalog.ProductCategories.AsNoTracking().Select(x => x.ProductId).Distinct().ToListAsync(cancellationToken)).ToHashSet();
        }

        var q = _catalog.LocalizedTexts.AsNoTracking()
            .Where(t => t.OwnerKind == CatalogLocalizedOwnerKind.Category && t.FieldKey == "name");
        q = filter.Operator switch
        {
            "contains" => q.Where(t => EF.Functions.ILike(t.Value, $"%{filter.Value}%")),
            "notContains" => q.Where(t => !EF.Functions.ILike(t.Value, $"%{filter.Value}%")),
            "equals" => q.Where(t => t.Value.ToLower() == (filter.Value ?? string.Empty).ToLower()),
            "notEqual" => q.Where(t => t.Value.ToLower() != (filter.Value ?? string.Empty).ToLower()),
            "startsWith" => q.Where(t => EF.Functions.ILike(t.Value, $"{filter.Value}%")),
            "endsWith" => q.Where(t => EF.Functions.ILike(t.Value, $"%{filter.Value}")),
            _ => q,
        };

        var categoryIds = await q.Select(t => t.OwnerId).Distinct().ToListAsync(cancellationToken);
        if (categoryIds.Count == 0)
        {
            return [];
        }

        return (await _catalog.ProductCategories.AsNoTracking()
            .Where(link => categoryIds.Contains(link.CategoryId))
            .Select(link => link.ProductId)
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();
    }

    private async Task<HashSet<Guid>> ResolveOfferAmountRangeProductIdsAsync(GridFilterRequest filter, CancellationToken cancellationToken)
    {
        var ranges = await BuildOfferAmountRangeMetricsAsync(cancellationToken);
        if (filter.Operator is "blank")
        {
            var all = await _catalog.Products.AsNoTracking().Select(p => p.ProductId).ToListAsync(cancellationToken);
            return all.Except(ranges.Keys).ToHashSet();
        }

        if (filter.Operator is "notBlank")
        {
            return ranges.Keys.ToHashSet();
        }

        if (!decimal.TryParse(filter.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var n))
        {
            return [];
        }

        decimal? nTo = decimal.TryParse(filter.ValueTo, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
        return ranges.Where(kv => OfferAmountMatch(kv.Value, filter.Operator, n, nTo)).Select(kv => kv.Key).ToHashSet();
    }

    private async Task<Dictionary<Guid, (decimal Min, decimal Max)>> BuildOfferAmountRangeMetricsAsync(CancellationToken cancellationToken)
    {
        var variantToProduct = await LoadVariantToProductMapAsync(cancellationToken);
        var offerToVariant = await _offers.Offers.AsNoTracking()
            .Select(o => new { o.OfferId, o.CatalogVariantId })
            .ToDictionaryAsync(x => x.OfferId, x => x.CatalogVariantId, cancellationToken);
        var prices = await _prices.Prices.AsNoTracking().Select(p => new { p.OfferId, p.Amount }).ToListAsync(cancellationToken);
        var byProduct = new Dictionary<Guid, List<decimal>>();
        foreach (var price in prices)
        {
            if (!offerToVariant.TryGetValue(price.OfferId, out var variantId) || !variantToProduct.TryGetValue(variantId, out var productId))
            {
                continue;
            }

            if (!byProduct.TryGetValue(productId, out var list))
            {
                list = [];
                byProduct[productId] = list;
            }

            list.Add(price.Amount);
        }

        return byProduct.ToDictionary(kv => kv.Key, kv => (kv.Value.Min(), kv.Value.Max()));
    }

    private static bool OfferAmountMatch((decimal Min, decimal Max) range, string op, decimal n, decimal? nTo) => op switch
    {
        "equals" => range.Min <= n && n <= range.Max,
        "notEqual" => !(range.Min <= n && n <= range.Max),
        "greaterThan" => range.Max > n,
        "greaterThanOrEqual" => range.Max >= n,
        "lessThan" => range.Min < n,
        "lessThanOrEqual" => range.Min <= n,
        "between" when nTo.HasValue => range.Min <= nTo.Value && range.Max >= n,
        _ => true,
    };

    private static bool MatchText(string value, GridFilterRequest filter) =>
        filter.Operator switch
        {
            "equals" => string.Equals(value, filter.Value, StringComparison.OrdinalIgnoreCase),
            "notEqual" => !string.Equals(value, filter.Value, StringComparison.OrdinalIgnoreCase),
            "contains" => value.Contains(filter.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase),
            "notContains" => !value.Contains(filter.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase),
            "startsWith" => value.StartsWith(filter.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase),
            "endsWith" => value.EndsWith(filter.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase),
            _ => true,
        };

    private async Task<List<Guid>> OrderAndPageCatalogFieldsAsync(
        IQueryable<CatalogProduct> products,
        GridQueryRequest query,
        GridSortRequest sort,
        CancellationToken cancellationToken)
    {
        var ordered = sort.Field switch
        {
            "status" when sort.Direction == "asc" => products.OrderBy(p => p.Status).ThenBy(p => p.ProductId),
            "status" => products.OrderByDescending(p => p.Status).ThenBy(p => p.ProductId),
            "updatedAt" when sort.Direction == "asc" => products.OrderBy(p => p.UpdatedAt).ThenBy(p => p.ProductId),
            _ => products.OrderByDescending(p => p.UpdatedAt).ThenBy(p => p.ProductId),
        };

        return await ordered.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).Select(p => p.ProductId).ToListAsync(cancellationToken);
    }

    private async Task<List<Guid>> OrderAndPageTitleAsync(
        IQueryable<CatalogProduct> products,
        GridQueryRequest query,
        GridSortRequest sort,
        CancellationToken cancellationToken)
    {
        var names = _catalog.LocalizedTexts.AsNoTracking()
            .Where(t => t.OwnerKind == CatalogLocalizedOwnerKind.Product && t.FieldKey == "name" && t.Locale.StartsWith("fa"));

        var joined = from p in products
                     join n in names on p.ProductId equals n.OwnerId into nj
                     from n in nj.DefaultIfEmpty()
                     select new { p.ProductId, Title = n != null ? n.Value : p.SlugSeam ?? string.Empty };

        joined = sort.Direction == "asc"
            ? joined.OrderBy(x => x.Title).ThenBy(x => x.ProductId)
            : joined.OrderByDescending(x => x.Title).ThenBy(x => x.ProductId);

        return await joined.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).Select(x => x.ProductId).ToListAsync(cancellationToken);
    }

    private async Task<List<Guid>> OrderAndPageCategorySummaryAsync(
        IQueryable<CatalogProduct> products,
        GridQueryRequest query,
        GridSortRequest sort,
        CancellationToken cancellationToken)
    {
        var categoryNames = _catalog.LocalizedTexts.AsNoTracking()
            .Where(t => t.OwnerKind == CatalogLocalizedOwnerKind.Category && t.FieldKey == "name" && t.Locale.StartsWith("fa"));
        var links = _catalog.ProductCategories.AsNoTracking();

        var joined = from p in products
                     join link in links on p.ProductId equals link.ProductId into lj
                     from link in lj.DefaultIfEmpty()
                     join n in categoryNames on link.CategoryId equals n.OwnerId into nj
                     from n in nj.DefaultIfEmpty()
                     group n by p.ProductId into g
                     select new
                     {
                         ProductId = g.Key,
                         Summary = g.Where(x => x != null).Select(x => x!.Value).OrderBy(x => x).FirstOrDefault() ?? "بدون دسته",
                     };

        joined = sort.Direction == "asc"
            ? joined.OrderBy(x => x.Summary).ThenBy(x => x.ProductId)
            : joined.OrderByDescending(x => x.Summary).ThenBy(x => x.ProductId);

        return await joined.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).Select(x => x.ProductId).ToListAsync(cancellationToken);
    }

    private async Task<List<Guid>> OrderAndPageVariantCountAsync(
        IQueryable<CatalogProduct> products,
        GridQueryRequest query,
        GridSortRequest sort,
        CancellationToken cancellationToken)
    {
        var counts = _catalog.Variants.AsNoTracking().GroupBy(v => v.ProductId).Select(g => new { ProductId = g.Key, Count = g.Count() });
        var joined = from p in products
                     join c in counts on p.ProductId equals c.ProductId into cj
                     from c in cj.DefaultIfEmpty()
                     select new { p.ProductId, Count = c != null ? c.Count : 0 };

        joined = sort.Direction == "asc"
            ? joined.OrderBy(x => x.Count).ThenBy(x => x.ProductId)
            : joined.OrderByDescending(x => x.Count).ThenBy(x => x.ProductId);

        return await joined.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).Select(x => x.ProductId).ToListAsync(cancellationToken);
    }

    private async Task<List<Guid>> OrderAndPageByMetricAsync(
        IQueryable<CatalogProduct> products,
        GridQueryRequest query,
        GridSortRequest sort,
        Dictionary<Guid, int> metrics,
        CancellationToken cancellationToken)
    {
        var ids = await products.Select(p => p.ProductId).ToListAsync(cancellationToken);
        IEnumerable<Guid> ordered = sort.Direction == "asc"
            ? ids.OrderBy(id => metrics.GetValueOrDefault(id)).ThenBy(id => id)
            : ids.OrderByDescending(id => metrics.GetValueOrDefault(id)).ThenBy(id => id);

        return ordered.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList();
    }

    private static HashSet<Guid> FilterMetrics(Dictionary<Guid, int> metrics, GridFilterRequest filter)
    {
        if (filter.Operator is "blank") return [];
        if (filter.Operator is "notBlank") return metrics.Keys.ToHashSet();

        if (!int.TryParse(filter.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
        {
            return [];
        }

        int? nTo = int.TryParse(filter.ValueTo, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
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

    private static string FormatOfferAmountRange(IReadOnlyList<(decimal Amount, string Currency)> rows)
    {
        if (rows.Count == 0) return "بدون مبلغ";
        var min = rows.Min(x => x.Amount);
        var max = rows.Max(x => x.Amount);
        var currency = rows[0].Currency;
        return min == max ? $"{min:0} {currency}".Trim() : $"{min:0}–{max:0} {currency}".Trim();
    }
}
