using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.Content.Application.Models;

namespace Tooba.Content.Infrastructure.Grid;

/// <summary>
/// سیاست Normalize گرید Admin مقالات/نویسندگان Content — مالک ماژول، بدون وابستگی به Host.Grid.
/// فیلدها و پیش‌فرض‌ها با رفتار قبلی AdminListGridPolicies.Content / ContentAuthors یکسان است.
/// </summary>
public static class ContentAdminGridPolicies
{
    private static readonly HashSet<string> ArticleSortableFields = new(StringComparer.Ordinal)
    {
        "title", "slug", "status", "category", "locale", "authorDisplayName", "updated",
    };

    private static readonly Dictionary<string, HashSet<string>> ArticleOperators = new(StringComparer.Ordinal)
    {
        ["articleId"] = GridQueryOperators.Enum,
        ["title"] = GridQueryOperators.Text,
        ["slug"] = GridQueryOperators.Text,
        ["status"] = GridQueryOperators.Enum,
        ["category"] = GridQueryOperators.Text,
        ["locale"] = GridQueryOperators.Text,
        ["authorDisplayName"] = GridQueryOperators.Text,
        ["authorId"] = GridQueryOperators.Enum,
        ["updated"] = GridQueryOperators.Date,
    };

    private static readonly HashSet<string> AuthorSortableFields = new(StringComparer.Ordinal)
    {
        "displayName", "slug", "isActive", "updated",
    };

    private static readonly Dictionary<string, HashSet<string>> AuthorOperators = new(StringComparer.Ordinal)
    {
        ["displayName"] = GridQueryOperators.Text,
        ["slug"] = GridQueryOperators.Text,
        ["isActive"] = GridQueryOperators.Enum,
        ["updated"] = GridQueryOperators.Date,
    };

    /// <summary>Normalize درخواست گرید مقالات Admin.</summary>
    public static GridQueryRequest NormalizeArticles(GridQueryRequest request) =>
        Normalize(request, ArticleSortableFields, ArticleOperators, "updated", "title");

    /// <summary>Normalize درخواست گرید نویسندگان Admin.</summary>
    public static GridQueryRequest NormalizeAuthors(GridQueryRequest request) =>
        Normalize(request, AuthorSortableFields, AuthorOperators, "updated", "displayName");

    private static GridQueryRequest Normalize(
        GridQueryRequest request,
        HashSet<string> sortableFields,
        Dictionary<string, HashSet<string>> operatorsByField,
        string defaultSortField,
        string tieBreakerField)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            return NormalizeCore(request, sortableFields, operatorsByField, defaultSortField);
        }
        catch (GridQueryValidationException ex)
        {
            throw new ContractOperationException(ex.ErrorCode);
        }
    }

    private static GridQueryRequest NormalizeCore(
        GridQueryRequest request,
        HashSet<string> sortableFields,
        Dictionary<string, HashSet<string>> operatorsByField,
        string defaultSortField)
    {
        var (page, pageSize) = GridQueryPolicyBase.NormalizePaging(
            request.Page,
            request.PageSize,
            GridQueryPolicyBase.DefaultMaxPageSize,
            GridQueryPolicyBase.DefaultDefaultPageSize);
        var search = GridQueryPolicyBase.NormalizeSearch(request.Search);

        var sorts = (request.Sort ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s.Field) && sortableFields.Contains(s.Field.Trim()))
            .Select(s => new GridSortRequest(
                s.Field.Trim(),
                string.Equals(s.Direction, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc"))
            .Take(GridQueryPolicyBase.DefaultMaxSortCount)
            .ToList();
        if (sorts.Count == 0)
        {
            sorts.Add(new GridSortRequest(defaultSortField, "desc"));
        }

        var filters = new List<GridFilterRequest>();
        foreach (var filter in request.Filters ?? [])
        {
            if (string.IsNullOrWhiteSpace(filter.Field)
                || !operatorsByField.TryGetValue(filter.Field.Trim(), out var allowedOps))
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
            NormalizeAdvanced(request.AdvancedFilter, operatorsByField));
    }

    private static GridAdvancedFilterExpression? NormalizeAdvanced(
        GridAdvancedFilterExpression? expression,
        Dictionary<string, HashSet<string>> operatorsByField)
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
                || !operatorsByField.TryGetValue(condition.Field.Trim(), out var allowedOps))
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

