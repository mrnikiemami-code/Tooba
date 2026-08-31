using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;

namespace Tooba.Host.Grid;

/// <summary>
/// اعتبارسنجی و نرمال‌سازی GridQuery برای گرید محصولات Admin.
/// </summary>
public sealed class AdminProductGridQueryPolicy : IGridQueryPolicy
{
    /// <summary>حداکثر اندازهٔ صفحهٔ مجاز.</summary>
    public const int MaxPageSize = GridQueryPolicyBase.DefaultMaxPageSize;

    /// <summary>اندازهٔ پیش‌فرض صفحه.</summary>
    public const int DefaultPageSize = GridQueryPolicyBase.DefaultDefaultPageSize;

    private static readonly HashSet<string> SortableFields =
    [
        "title",
        "status",
        "variantCount",
        "offerCount",
        "categorySummary",
        "primaryCategoryName",
        "sellableUnits",
        "locationCount",
        "updatedAt",
        "offerAmountRange",
    ];

    private static readonly HashSet<string> FilterableFields = SortableFields;

    /// <summary>نقطهٔ ورود static برای endpointهای موجود.</summary>
    public static GridQueryRequest Normalize(GridQueryRequest request)
    {
        try
        {
            return NormalizeInternal(request);
        }
        catch (GridQueryValidationException ex)
        {
            throw new PlatformHttpException(ex.StatusCode, ex.Message, ex.ErrorCode);
        }
    }

    GridQueryRequest IGridQueryPolicy.Normalize(GridQueryRequest request) => Normalize(request);

    private static GridQueryRequest NormalizeInternal(GridQueryRequest request)
    {
        var (page, pageSize) = GridQueryPolicyBase.NormalizePaging(
            request.Page,
            request.PageSize,
            MaxPageSize,
            DefaultPageSize);
        var search = GridQueryPolicyBase.NormalizeSearch(request.Search);

        var sorts = (request.Sort ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s.Field) && SortableFields.Contains(s.Field))
            .Select(s => new GridSortRequest(
                s.Field.Trim(),
                string.Equals(s.Direction, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc"))
            .Take(GridQueryPolicyBase.DefaultMaxSortCount)
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
                throw GridQueryValidationException.FilterFieldInvalid();
            }

            var op = (filter.Operator ?? string.Empty).Trim();
            ValidateOperator(filter.Field, op);
            filters.Add(GridQueryPolicyBase.NormalizeFilter(filter));
        }

        var advancedFilter = NormalizeAdvancedFilter(request.AdvancedFilter);

        return new GridQueryRequest(page, pageSize, search, sorts, filters, advancedFilter);
    }

    internal static GridAdvancedFilterExpression? NormalizeAdvancedFilter(GridAdvancedFilterExpression? expression)
    {
        if (expression?.Conditions is not { Count: > 0 } conditions)
        {
            return null;
        }

        GridQueryPolicyBase.ValidateAdvancedConnectors(conditions.Count, expression.Connectors);
        var connectors = (expression.Connectors ?? []).Select(c => c.ToLowerInvariant()).ToList();

        var normalizedConditions = new List<GridAdvancedFilterCondition>();
        foreach (var condition in conditions)
        {
            if (string.IsNullOrWhiteSpace(condition.Field) || !FilterableFields.Contains(condition.Field))
            {
                throw GridQueryValidationException.AdvancedFieldInvalid();
            }

            var op = (condition.Operator ?? string.Empty).Trim();
            ValidateOperator(condition.Field, op);
            normalizedConditions.Add(new GridAdvancedFilterCondition(
                string.IsNullOrWhiteSpace(condition.Id) ? Guid.NewGuid().ToString("N") : condition.Id.Trim(),
                condition.Field.Trim(),
                op,
                GridQueryPolicyBase.NormalizeScalar(condition.Value),
                GridQueryPolicyBase.NormalizeScalar(condition.ValueTo),
                condition.Values?
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Select(v => v.Trim())
                    .Distinct(StringComparer.Ordinal)
                    .Take(20)
                    .ToList()));
        }

        return new GridAdvancedFilterExpression(normalizedConditions, connectors);
    }

    private static void ValidateOperator(string field, string op)
    {
        var allowed = field switch
        {
            "title" or "categorySummary" or "primaryCategoryName" => GridQueryOperators.Text,
            "offerAmountRange" or "variantCount" or "offerCount" or "sellableUnits" or "locationCount" => GridQueryOperators.Number,
            "status" => GridQueryOperators.Enum,
            "updatedAt" => GridQueryOperators.Date,
            _ => throw GridQueryValidationException.FilterFieldInvalid(),
        };

        if (!allowed.Contains(op))
        {
            throw GridQueryValidationException.FilterOperatorInvalid();
        }
    }
}
