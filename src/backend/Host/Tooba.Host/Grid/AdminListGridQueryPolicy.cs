using System.Globalization;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;

namespace Tooba.Host.Grid;

/// <summary>
/// اعتبارسنجی GridQuery برای فهرست‌های Admin flat و اجرای in-memory paging/filter/sort.
/// </summary>
public sealed class AdminListGridQueryPolicy<T>
{
    private readonly IReadOnlyDictionary<string, InMemoryGridField<T>> _fields;
    private readonly string _defaultSortField;
    private readonly string _defaultSortDirection;
    private readonly string? _tieBreakerField;

    /// <summary>سیاست گرید in-memory با whitelist فیلد.</summary>
    public AdminListGridQueryPolicy(
        IEnumerable<InMemoryGridField<T>> fields,
        string defaultSortField,
        string defaultSortDirection = "desc",
        string? tieBreakerField = null)
    {
        _fields = fields.ToDictionary(x => x.Name, StringComparer.Ordinal);
        _defaultSortField = defaultSortField;
        _defaultSortDirection = defaultSortDirection;
        _tieBreakerField = tieBreakerField;
    }

    /// <summary>درخواست را normalize می‌کند.</summary>
    public GridQueryRequest Normalize(GridQueryRequest request)
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

    /// <summary>فهرست را با درخواست normalize‌شده صفحه‌بندی می‌کند.</summary>
    public GridPageResponse<T> Execute(IReadOnlyList<T> source, GridQueryRequest request)
    {
        var normalized = NormalizeInternal(request);
        return InMemoryGridQueryEngine.Execute(source, normalized, _fields, _tieBreakerField);
    }

    private GridQueryRequest NormalizeInternal(GridQueryRequest request)
    {
        var (page, pageSize) = GridQueryPolicyBase.NormalizePaging(
            request.Page,
            request.PageSize,
            GridQueryPolicyBase.DefaultMaxPageSize,
            GridQueryPolicyBase.DefaultDefaultPageSize);
        var search = GridQueryPolicyBase.NormalizeSearch(request.Search);

        var sorts = (request.Sort ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s.Field) && _fields.TryGetValue(s.Field.Trim(), out var field) && field.Sortable)
            .Select(s => new GridSortRequest(
                s.Field.Trim(),
                string.Equals(s.Direction, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc"))
            .Take(GridQueryPolicyBase.DefaultMaxSortCount)
            .ToList();
        if (sorts.Count == 0)
        {
            sorts.Add(new GridSortRequest(_defaultSortField, _defaultSortDirection));
        }

        var filters = new List<GridFilterRequest>();
        foreach (var filter in request.Filters ?? [])
        {
            if (string.IsNullOrWhiteSpace(filter.Field) || !_fields.TryGetValue(filter.Field.Trim(), out var field) || !field.Filterable)
            {
                throw GridQueryValidationException.FilterFieldInvalid();
            }

            var op = (filter.Operator ?? string.Empty).Trim();
            ValidateOperator(field.Kind, op);
            filters.Add(GridQueryPolicyBase.NormalizeFilter(filter with { Field = filter.Field.Trim() }));
        }

        var advancedFilter = NormalizeAdvancedFilter(request.AdvancedFilter);
        return new GridQueryRequest(page, pageSize, search, sorts, filters, advancedFilter);
    }

    private GridAdvancedFilterExpression? NormalizeAdvancedFilter(GridAdvancedFilterExpression? expression)
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
            if (string.IsNullOrWhiteSpace(condition.Field) || !_fields.TryGetValue(condition.Field.Trim(), out var field) || !field.Filterable)
            {
                throw GridQueryValidationException.AdvancedFieldInvalid();
            }

            var op = (condition.Operator ?? string.Empty).Trim();
            ValidateOperator(field.Kind, op);
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

    private static void ValidateOperator(InMemoryGridFieldKind kind, string op)
    {
        var allowed = kind switch
        {
            InMemoryGridFieldKind.Text => GridQueryOperators.Text,
            InMemoryGridFieldKind.Number => GridQueryOperators.Number,
            InMemoryGridFieldKind.Date => GridQueryOperators.Date,
            InMemoryGridFieldKind.Enum => GridQueryOperators.Enum,
            _ => throw GridQueryValidationException.FilterOperatorInvalid(),
        };

        if (!allowed.Contains(op))
        {
            throw GridQueryValidationException.FilterOperatorInvalid();
        }
    }
}
