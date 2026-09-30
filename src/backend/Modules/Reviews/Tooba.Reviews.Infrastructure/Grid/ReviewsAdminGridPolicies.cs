using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;

namespace Tooba.Reviews.Infrastructure.Grid;

/// <summary>
/// سیاست Normalize گرید Admin نظرات — مالک ماژول، بدون وابستگی به Host.Grid.
/// فیلدها و پیش‌فرض‌ها با رفتار قبلی AdminListGridPolicies.Reviews یکسان است.
/// </summary>
public static class ReviewsAdminGridPolicies
{
    private const string DefaultSortField = "created";
    private const string DefaultSortDirection = "desc";

    private static readonly HashSet<string> SortableFields = new(StringComparer.Ordinal)
    {
        "reviewer", "product", "rating", "excerpt", "verified", "status", "created",
    };

    private static readonly Dictionary<string, HashSet<string>> OperatorsByField = new(StringComparer.Ordinal)
    {
        ["reviewer"] = GridQueryOperators.Text,
        ["product"] = GridQueryOperators.Text,
        ["rating"] = GridQueryOperators.Number,
        ["excerpt"] = GridQueryOperators.Text,
        ["verified"] = GridQueryOperators.Enum,
        ["status"] = GridQueryOperators.Enum,
        ["created"] = GridQueryOperators.Date,
    };

    /// <summary>Normalize درخواست گرید نظرات Admin.</summary>
    public static GridQueryRequest Normalize(GridQueryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            return NormalizeCore(request);
        }
        catch (GridQueryValidationException ex)
        {
            throw new PlatformHttpException(ex.StatusCode, ex.Message, ex.ErrorCode);
        }
    }

    private static GridQueryRequest NormalizeCore(GridQueryRequest request)
    {
        var (page, pageSize) = GridQueryPolicyBase.NormalizePaging(
            request.Page,
            request.PageSize,
            GridQueryPolicyBase.DefaultMaxPageSize,
            GridQueryPolicyBase.DefaultDefaultPageSize);
        var search = GridQueryPolicyBase.NormalizeSearch(request.Search);

        var sorts = (request.Sort ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s.Field) && SortableFields.Contains(s.Field.Trim()))
            .Select(s => new GridSortRequest(
                s.Field.Trim(),
                string.Equals(s.Direction, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc"))
            .Take(GridQueryPolicyBase.DefaultMaxSortCount)
            .ToList();
        if (sorts.Count == 0)
        {
            sorts.Add(new GridSortRequest(DefaultSortField, DefaultSortDirection));
        }

        var filters = new List<GridFilterRequest>();
        foreach (var filter in request.Filters ?? [])
        {
            if (string.IsNullOrWhiteSpace(filter.Field)
                || !OperatorsByField.TryGetValue(filter.Field.Trim(), out var allowedOps))
            {
                throw GridQueryValidationException.FilterFieldInvalid();
            }

            var op = (filter.Operator ?? string.Empty).Trim();
            if (!allowedOps.Contains(op))
            {
                throw GridQueryValidationException.FilterOperatorInvalid();
            }

            filters.Add(GridQueryPolicyBase.NormalizeFilter(filter with { Field = filter.Field.Trim() }));
        }

        return new GridQueryRequest(
            page,
            pageSize,
            search,
            sorts,
            filters,
            NormalizeAdvanced(request.AdvancedFilter));
    }

    private static GridAdvancedFilterExpression? NormalizeAdvanced(GridAdvancedFilterExpression? expression)
    {
        if (expression?.Conditions is not { Count: > 0 } conditions)
        {
            return null;
        }

        GridQueryPolicyBase.ValidateAdvancedConnectors(conditions.Count, expression.Connectors);
        var connectors = (expression.Connectors ?? []).Select(c => c.ToLowerInvariant()).ToList();
        var normalized = new List<GridAdvancedFilterCondition>();
        foreach (var condition in conditions)
        {
            if (string.IsNullOrWhiteSpace(condition.Field)
                || !OperatorsByField.TryGetValue(condition.Field.Trim(), out var allowedOps))
            {
                throw GridQueryValidationException.AdvancedFieldInvalid();
            }

            var op = (condition.Operator ?? string.Empty).Trim();
            if (!allowedOps.Contains(op))
            {
                throw GridQueryValidationException.FilterOperatorInvalid();
            }

            normalized.Add(new GridAdvancedFilterCondition(
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

        return new GridAdvancedFilterExpression(normalized, connectors);
    }
}
