using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;

namespace Tooba.Payment.Endpoints.Admin;

/// <summary>
/// Payment-owned admin payments grid normalizer. Owns the exact field whitelist and operator policy for
/// the admin payments list; uses only <see cref="Tooba.BuildingBlocks.Grid"/> primitives and never the
/// Host grid engine, Host grid policy, or Host row models.
/// </summary>
public sealed class PaymentAdminGridQueryNormalizer : IPaymentAdminGridQueryNormalizer
{
    private const string DefaultSortField = "created";
    private const string DefaultSortDirection = "desc";

    private static readonly HashSet<string> AllowedFields = new(StringComparer.Ordinal)
    {
        "reference", "customer", "amount", "status", "supply", "reservation", "provider", "created", "completed",
    };

    private static readonly Dictionary<string, HashSet<string>> OperatorsByField = new(StringComparer.Ordinal)
    {
        ["reference"] = GridQueryOperators.Text,
        ["customer"] = GridQueryOperators.Text,
        ["amount"] = GridQueryOperators.Number,
        ["status"] = GridQueryOperators.Enum,
        ["supply"] = GridQueryOperators.Enum,
        ["reservation"] = GridQueryOperators.Enum,
        ["provider"] = GridQueryOperators.Text,
        ["created"] = GridQueryOperators.Date,
        ["completed"] = GridQueryOperators.Date,
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
            .Where(s => !string.IsNullOrWhiteSpace(s.Field) && AllowedFields.Contains(s.Field.Trim()))
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
            if (!TryResolveOperators(filter.Field, out var allowedOps))
            {
                throw ValidationFailure(GridQueryValidationException.FilterFieldInvalid().ErrorCode);
            }

            var op = (filter.Operator ?? string.Empty).Trim();
            if (!allowedOps.Contains(op))
            {
                throw ValidationFailure(GridQueryValidationException.FilterOperatorInvalid().ErrorCode);
            }

            filters.Add(GridQueryPolicyBase.NormalizeFilter(filter with { Field = filter.Field!.Trim() }));
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
            if (!TryResolveOperators(condition.Field, out var allowedOps))
            {
                throw ValidationFailure(GridQueryValidationException.AdvancedFieldInvalid().ErrorCode);
            }

            var op = (condition.Operator ?? string.Empty).Trim();
            if (!allowedOps.Contains(op))
            {
                throw ValidationFailure(GridQueryValidationException.FilterOperatorInvalid().ErrorCode);
            }

            normalized.Add(new GridAdvancedFilterCondition(
                string.IsNullOrWhiteSpace(condition.Id) ? $"adv-{normalized.Count}" : condition.Id.Trim(),
                condition.Field!.Trim(),
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

    private static bool TryResolveOperators(string? field, out HashSet<string> operators)
    {
        operators = GridQueryOperators.Text;
        return !string.IsNullOrWhiteSpace(field)
               && AllowedFields.Contains(field.Trim())
               && OperatorsByField.TryGetValue(field.Trim(), out operators!);
    }

    private static SemanticException ValidationFailure(string errorCode) =>
        new(new SemanticError(errorCode));
}
