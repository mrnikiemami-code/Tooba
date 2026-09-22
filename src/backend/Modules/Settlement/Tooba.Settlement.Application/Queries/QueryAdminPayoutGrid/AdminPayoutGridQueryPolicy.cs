using Tooba.BuildingBlocks.Grid;

namespace Tooba.Settlement.Application.Queries.QueryAdminPayoutGrid;

/// <summary>
/// Module-owned payout grid Normalize — whitelist seller/amount/status/created,
/// default sort created desc. Pending/Failed default stays in Infrastructure engine.
/// </summary>
public sealed class AdminPayoutGridQueryPolicy : IGridQueryPolicy
{
    /// <summary>Singleton policy instance.</summary>
    public static AdminPayoutGridQueryPolicy Instance { get; } = new();

    private static readonly HashSet<string> AllowedFields = new(StringComparer.Ordinal)
    {
        "seller", "amount", "status", "created",
    };

    private static readonly Dictionary<string, HashSet<string>> OperatorsByField = new(StringComparer.Ordinal)
    {
        ["seller"] = GridQueryOperators.Text,
        ["amount"] = GridQueryOperators.Number,
        ["status"] = GridQueryOperators.Enum,
        ["created"] = GridQueryOperators.Date,
    };

    /// <inheritdoc />
    public GridQueryRequest Normalize(GridQueryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (page, pageSize) = GridQueryPolicyBase.NormalizePaging(
            request.Page,
            request.PageSize,
            GridQueryPolicyBase.DefaultMaxPageSize,
            GridQueryPolicyBase.DefaultDefaultPageSize);
        var search = GridQueryPolicyBase.NormalizeSearch(request.Search);

        var sorts = (request.Sort ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s.Field)
                        && AllowedFields.Contains(s.Field.Trim()))
            .Select(s => new GridSortRequest(
                s.Field.Trim(),
                string.Equals(s.Direction, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc"))
            .Take(GridQueryPolicyBase.DefaultMaxSortCount)
            .ToList();
        if (sorts.Count == 0)
        {
            sorts.Add(new GridSortRequest("created", "desc"));
        }

        var filters = new List<GridFilterRequest>();
        foreach (var filter in request.Filters ?? [])
        {
            if (string.IsNullOrWhiteSpace(filter.Field)
                || !AllowedFields.Contains(filter.Field.Trim())
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

        var advancedFilter = NormalizeAdvancedFilter(request.AdvancedFilter);
        return new GridQueryRequest(page, pageSize, search, sorts, filters, advancedFilter);
    }

    private static GridAdvancedFilterExpression? NormalizeAdvancedFilter(GridAdvancedFilterExpression? expression)
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
            if (string.IsNullOrWhiteSpace(condition.Field)
                || !AllowedFields.Contains(condition.Field.Trim())
                || !OperatorsByField.TryGetValue(condition.Field.Trim(), out var allowedOps))
            {
                throw GridQueryValidationException.AdvancedFieldInvalid();
            }

            var op = (condition.Operator ?? string.Empty).Trim();
            if (!allowedOps.Contains(op))
            {
                throw GridQueryValidationException.FilterOperatorInvalid();
            }

            normalizedConditions.Add(new GridAdvancedFilterCondition(
                string.IsNullOrWhiteSpace(condition.Id) ? $"adv-{normalizedConditions.Count}" : condition.Id.Trim(),
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
}
