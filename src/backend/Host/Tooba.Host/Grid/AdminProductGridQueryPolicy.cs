using Tooba.BuildingBlocks;

namespace Tooba.Host.Grid;

/// <summary>
/// اعتبارسنجی و نرمال‌سازی GridQuery برای گرید محصولات Admin.
/// </summary>
public static class AdminProductGridQueryPolicy
{
    /// <summary>حداکثر اندازهٔ صفحهٔ مجاز.</summary>
    public const int MaxPageSize = 100;

    /// <summary>اندازهٔ پیش‌فرض صفحه.</summary>
    public const int DefaultPageSize = 20;

    private static readonly HashSet<string> SortableFields =
    [
        "title",
        "status",
        "variantCount",
        "offerCount",
        "categorySummary",
        "sellableUnits",
        "locationCount",
        "updatedAt",
    ];

    private static readonly HashSet<string> FilterableFields = SortableFields;

    private static readonly HashSet<string> TextOperators = ["contains", "equals", "startsWith", "notContains", "notEqual", "endsWith", "blank", "notBlank"];
    private static readonly HashSet<string> NumberOperators = ["equals", "notEqual", "greaterThan", "greaterThanOrEqual", "lessThan", "lessThanOrEqual", "between", "blank", "notBlank"];
    private static readonly HashSet<string> DateOperators = ["on", "before", "after", "between", "blank", "notBlank"];
    private static readonly HashSet<string> EnumOperators = ["equals", "notEqual", "in", "notIn", "blank", "notBlank"];

    /// <summary>
    /// Query ورودی را sanitize می‌کند یا خطای 400 می‌دهد.
    /// </summary>
    public static GridQueryRequest Normalize(GridQueryRequest request)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);
        var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        if (search?.Length > 200)
        {
            search = search[..200];
        }

        var sorts = (request.Sort ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s.Field) && SortableFields.Contains(s.Field))
            .Select(s => new GridSortRequest(
                s.Field.Trim(),
                string.Equals(s.Direction, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc"))
            .Take(3)
            .ToList();
        if (sorts.Count == 0)
        {
            sorts.Add(new GridSortRequest("updatedAt", "desc"));
        }

        sorts.Add(new GridSortRequest("productId", "asc"));

        var filters = new List<GridFilterRequest>();
        foreach (var filter in request.Filters ?? [])
        {
            if (string.IsNullOrWhiteSpace(filter.Field) || !FilterableFields.Contains(filter.Field))
            {
                throw new PlatformHttpException(400, "فیلد فیلتر مجاز نیست.", "grid.filter.field.invalid");
            }

            var op = (filter.Operator ?? string.Empty).Trim();
            ValidateOperator(filter.Field, op);
            filters.Add(new GridFilterRequest(
                filter.Field,
                op,
                NormalizeScalar(filter.Value),
                NormalizeScalar(filter.ValueTo),
                filter.Values?.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()).Distinct(StringComparer.Ordinal).Take(20).ToList()));
        }

        return new GridQueryRequest(page, pageSize, search, sorts, filters);
    }

    private static string? NormalizeScalar(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateOperator(string field, string op)
    {
        var allowed = field switch
        {
            "title" or "categorySummary" or "offerAmountRange" => TextOperators,
            "status" => EnumOperators,
            "variantCount" or "offerCount" or "sellableUnits" or "locationCount" => NumberOperators,
            "updatedAt" => DateOperators,
            _ => throw new PlatformHttpException(400, "فیلد فیلتر نامعتبر است.", "grid.filter.field.invalid"),
        };

        if (!allowed.Contains(op))
        {
            throw new PlatformHttpException(400, "عملگر فیلتر مجاز نیست.", "grid.filter.operator.invalid");
        }
    }
}
