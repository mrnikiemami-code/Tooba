using System.Globalization;
using Tooba.Host.Admin;

namespace Tooba.Host.Grid;

/// <summary>
/// اعمال جستجو/فیلتر/مرتب‌سازی/صفحه‌بندی روی ردیف‌های غنی‌شدهٔ فهرست محصول.
/// </summary>
internal static class AdminProductGridEvaluator
{
    public static IReadOnlyList<AdminProductListItem> Apply(
        IReadOnlyList<AdminProductListItem> rows,
        GridQueryRequest query)
    {
        IEnumerable<AdminProductListItem> current = rows;

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            current = current.Where(row =>
                row.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
                || row.CategorySummary.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var filter in query.Filters)
        {
            current = ApplyFilter(current, filter);
        }

        current = ApplySort(current, query.Sort);
        return current.ToList();
    }

    public static GridPageResponse<AdminProductListItem> Page(
        IReadOnlyList<AdminProductListItem> filtered,
        GridQueryRequest query)
    {
        var total = filtered.Count;
        var skip = (query.Page - 1) * query.PageSize;
        var items = filtered.Skip(skip).Take(query.PageSize).ToList();
        return new GridPageResponse<AdminProductListItem>(items, query.Page, query.PageSize, total);
    }

    private static IEnumerable<AdminProductListItem> ApplyFilter(
        IEnumerable<AdminProductListItem> rows,
        GridFilterRequest filter)
    {
        return filter.Field switch
        {
            "title" => ApplyText(rows, filter, r => r.Title),
            "categorySummary" => ApplyText(rows, filter, r => r.CategorySummary),
            "offerAmountRange" => ApplyText(rows, filter, r => r.OfferAmountRange),
            "status" => ApplyEnum(rows, filter, r => r.Status),
            "variantCount" => ApplyNumber(rows, filter, r => r.VariantCount),
            "offerCount" => ApplyNumber(rows, filter, r => r.OfferCount),
            "sellableUnits" => ApplyNumber(rows, filter, r => r.SellableUnits),
            "locationCount" => ApplyNumber(rows, filter, r => r.LocationCount),
            "updatedAt" => ApplyDate(rows, filter, r => r.UpdatedAt),
            _ => rows,
        };
    }

    private static IEnumerable<AdminProductListItem> ApplyText(
        IEnumerable<AdminProductListItem> rows,
        GridFilterRequest filter,
        Func<AdminProductListItem, string> selector)
    {
        var value = filter.Value ?? string.Empty;
        return filter.Operator switch
        {
            "blank" => rows.Where(r => string.IsNullOrWhiteSpace(selector(r))),
            "notBlank" => rows.Where(r => !string.IsNullOrWhiteSpace(selector(r))),
            "equals" => rows.Where(r => string.Equals(selector(r), value, StringComparison.OrdinalIgnoreCase)),
            "notEqual" => rows.Where(r => !string.Equals(selector(r), value, StringComparison.OrdinalIgnoreCase)),
            "contains" => rows.Where(r => selector(r).Contains(value, StringComparison.OrdinalIgnoreCase)),
            "notContains" => rows.Where(r => !selector(r).Contains(value, StringComparison.OrdinalIgnoreCase)),
            "startsWith" => rows.Where(r => selector(r).StartsWith(value, StringComparison.OrdinalIgnoreCase)),
            "endsWith" => rows.Where(r => selector(r).EndsWith(value, StringComparison.OrdinalIgnoreCase)),
            _ => rows,
        };
    }

    private static IEnumerable<AdminProductListItem> ApplyEnum(
        IEnumerable<AdminProductListItem> rows,
        GridFilterRequest filter,
        Func<AdminProductListItem, string> selector)
    {
        var values = filter.Values?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
        return filter.Operator switch
        {
            "blank" => rows.Where(r => string.IsNullOrWhiteSpace(selector(r))),
            "notBlank" => rows.Where(r => !string.IsNullOrWhiteSpace(selector(r))),
            "equals" => rows.Where(r => string.Equals(selector(r), filter.Value, StringComparison.OrdinalIgnoreCase)),
            "notEqual" => rows.Where(r => !string.Equals(selector(r), filter.Value, StringComparison.OrdinalIgnoreCase)),
            "in" => rows.Where(r => values.Contains(selector(r))),
            "notIn" => rows.Where(r => !values.Contains(selector(r))),
            _ => rows,
        };
    }

    private static IEnumerable<AdminProductListItem> ApplyNumber(
        IEnumerable<AdminProductListItem> rows,
        GridFilterRequest filter,
        Func<AdminProductListItem, int> selector)
    {
        if (filter.Operator is "blank" or "notBlank")
        {
            return filter.Operator == "blank" ? [] : rows;
        }

        if (!int.TryParse(filter.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
        {
            return rows;
        }

        int? nTo = int.TryParse(filter.ValueTo, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedTo)
            ? parsedTo
            : null;

        return filter.Operator switch
        {
            "equals" => rows.Where(r => selector(r) == n),
            "notEqual" => rows.Where(r => selector(r) != n),
            "greaterThan" => rows.Where(r => selector(r) > n),
            "greaterThanOrEqual" => rows.Where(r => selector(r) >= n),
            "lessThan" => rows.Where(r => selector(r) < n),
            "lessThanOrEqual" => rows.Where(r => selector(r) <= n),
            "between" when nTo.HasValue => rows.Where(r => selector(r) >= n && selector(r) <= nTo.Value),
            _ => rows,
        };
    }

    private static IEnumerable<AdminProductListItem> ApplyDate(
        IEnumerable<AdminProductListItem> rows,
        GridFilterRequest filter,
        Func<AdminProductListItem, DateTimeOffset> selector)
    {
        if (filter.Operator is "blank" or "notBlank")
        {
            return rows;
        }

        if (!DateTimeOffset.TryParse(filter.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d))
        {
            return rows;
        }

        DateTimeOffset? dTo = DateTimeOffset.TryParse(filter.ValueTo, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedTo)
            ? parsedTo
            : null;

        var dayStart = new DateTimeOffset(d.UtcDateTime.Date, TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);

        return filter.Operator switch
        {
            "on" => rows.Where(r => r.UpdatedAt >= dayStart && r.UpdatedAt < dayEnd),
            "before" => rows.Where(r => selector(r) < dayStart),
            "after" => rows.Where(r => selector(r) >= dayEnd),
            "between" when dTo.HasValue => rows.Where(r =>
            {
                var start = new DateTimeOffset(dTo.Value.UtcDateTime.Date, TimeSpan.Zero);
                var end = start.AddDays(1);
                return selector(r) >= dayStart && selector(r) < end;
            }),
            _ => rows,
        };
    }

    private static IEnumerable<AdminProductListItem> ApplySort(
        IEnumerable<AdminProductListItem> rows,
        IReadOnlyList<GridSortRequest> sorts)
    {
        IOrderedEnumerable<AdminProductListItem>? ordered = null;
        foreach (var sort in sorts)
        {
            ordered = sort.Field switch
            {
                "title" => Order(ordered, rows, r => r.Title, sort.Direction),
                "status" => Order(ordered, rows, r => r.Status, sort.Direction),
                "variantCount" => Order(ordered, rows, r => r.VariantCount, sort.Direction),
                "offerCount" => Order(ordered, rows, r => r.OfferCount, sort.Direction),
                "categorySummary" => Order(ordered, rows, r => r.CategorySummary, sort.Direction),
                "sellableUnits" => Order(ordered, rows, r => r.SellableUnits, sort.Direction),
                "locationCount" => Order(ordered, rows, r => r.LocationCount, sort.Direction),
                "updatedAt" => Order(ordered, rows, r => r.UpdatedAt, sort.Direction),
                "productId" => Order(ordered, rows, r => r.ProductId, sort.Direction),
                _ => ordered ?? rows.OrderBy(_ => 0),
            };
            rows = ordered ?? rows;
        }

        return ordered ?? rows;
    }

    private static IOrderedEnumerable<AdminProductListItem> Order<TKey>(
        IOrderedEnumerable<AdminProductListItem>? ordered,
        IEnumerable<AdminProductListItem> rows,
        Func<AdminProductListItem, TKey> key,
        string direction)
    {
        if (ordered is null)
        {
            return string.Equals(direction, "asc", StringComparison.OrdinalIgnoreCase)
                ? rows.OrderBy(key)
                : rows.OrderByDescending(key);
        }

        return string.Equals(direction, "asc", StringComparison.OrdinalIgnoreCase)
            ? ordered.ThenBy(key)
            : ordered.ThenByDescending(key);
    }
}
