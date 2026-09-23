using Tooba.BuildingBlocks.Grid;

namespace Tooba.Order.Application.Admin.Customers;

/// <summary>نوع فیلد whitelist گرید مشتریان مدیر.</summary>
public enum AdminCustomersGridFieldKind
{
    Text = 0,
    Number = 1,
    Date = 2,
    Enum = 3,
}

/// <summary>
/// whitelist فیلد و normalize درخواست گرید مشتریان مدیر.
/// همان قرارداد فیلد/عملگر Host پیش از انتقال را نگه می‌دارد.
/// </summary>
public static class AdminCustomersGridPolicy
{
    private const string DefaultSortField = "activity";
    private const string DefaultSortDirection = "desc";

    private static readonly IReadOnlyDictionary<string, AdminCustomersGridFieldKind> Fields =
        new Dictionary<string, AdminCustomersGridFieldKind>(StringComparer.Ordinal)
        {
            ["name"] = AdminCustomersGridFieldKind.Text,
            ["contact"] = AdminCustomersGridFieldKind.Text,
            ["orders"] = AdminCustomersGridFieldKind.Number,
            ["activity"] = AdminCustomersGridFieldKind.Date,
            ["status"] = AdminCustomersGridFieldKind.Enum,
        };

    /// <summary>
    /// درخواست را normalize می‌کند و فیلد/عملگر ناشناخته را با
    /// <see cref="GridQueryValidationException"/> رد می‌کند.
    /// </summary>
    public static GridQueryRequest Normalize(GridQueryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (page, pageSize) = GridQueryPolicyBase.NormalizePaging(
            request.Page,
            request.PageSize,
            GridQueryPolicyBase.DefaultMaxPageSize,
            GridQueryPolicyBase.DefaultDefaultPageSize);
        var search = GridQueryPolicyBase.NormalizeSearch(request.Search);

        var sorts = (request.Sort ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s.Field) && Fields.ContainsKey(s.Field.Trim()))
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
            if (string.IsNullOrWhiteSpace(filter.Field) || !Fields.TryGetValue(filter.Field.Trim(), out var kind))
            {
                throw GridQueryValidationException.FilterFieldInvalid();
            }

            ValidateOperator(kind, (filter.Operator ?? string.Empty).Trim());
            filters.Add(GridQueryPolicyBase.NormalizeFilter(filter with { Field = filter.Field.Trim() }));
        }

        return new GridQueryRequest(page, pageSize, search, sorts, filters, NormalizeAdvanced(request.AdvancedFilter));
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
            if (string.IsNullOrWhiteSpace(condition.Field) || !Fields.TryGetValue(condition.Field.Trim(), out var kind))
            {
                throw GridQueryValidationException.AdvancedFieldInvalid();
            }

            var op = (condition.Operator ?? string.Empty).Trim();
            ValidateOperator(kind, op);
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

    private static void ValidateOperator(AdminCustomersGridFieldKind kind, string op)
    {
        var allowed = kind switch
        {
            AdminCustomersGridFieldKind.Text => GridQueryOperators.Text,
            AdminCustomersGridFieldKind.Number => GridQueryOperators.Number,
            AdminCustomersGridFieldKind.Date => GridQueryOperators.Date,
            AdminCustomersGridFieldKind.Enum => GridQueryOperators.Enum,
            _ => throw GridQueryValidationException.FilterOperatorInvalid(),
        };

        if (!allowed.Contains(op))
        {
            throw GridQueryValidationException.FilterOperatorInvalid();
        }
    }
}
