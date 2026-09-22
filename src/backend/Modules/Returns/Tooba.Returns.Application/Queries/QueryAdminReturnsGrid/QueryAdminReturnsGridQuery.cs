using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Application.Models;
using Tooba.Returns.Application.Ports;

namespace Tooba.Returns.Application.Queries.QueryAdminReturnsGrid;

/// <summary>Module-owned Returns admin grid Normalize — whitelist + default sort createdAt desc.</summary>
public sealed class AdminReturnGridQueryPolicy : IGridQueryPolicy
{
    public static AdminReturnGridQueryPolicy Instance { get; } = new();

    private static readonly HashSet<string> AllowedFields = new(StringComparer.Ordinal)
    {
        "returnReference", "orderReference", "returnRequestId", "customerDisplayName", "sellerDisplayName",
        "productLabel", "quantityRequested", "returnStatus", "refundStatus", "queueFilter",
        "createdAt", "updatedAt",
    };

    private static readonly Dictionary<string, HashSet<string>> OperatorsByField = new(StringComparer.Ordinal)
    {
        ["returnReference"] = GridQueryOperators.Text,
        ["orderReference"] = GridQueryOperators.Text,
        ["returnRequestId"] = GridQueryOperators.Text,
        ["customerDisplayName"] = GridQueryOperators.Text,
        ["sellerDisplayName"] = GridQueryOperators.Text,
        ["productLabel"] = GridQueryOperators.Text,
        ["quantityRequested"] = GridQueryOperators.Number,
        ["returnStatus"] = GridQueryOperators.Enum,
        ["refundStatus"] = GridQueryOperators.Enum,
        ["queueFilter"] = GridQueryOperators.Enum,
        ["createdAt"] = GridQueryOperators.Date,
        ["updatedAt"] = GridQueryOperators.Date,
    };

    public GridQueryRequest Normalize(GridQueryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (page, pageSize) = GridQueryPolicyBase.NormalizePaging(
            request.Page, request.PageSize,
            GridQueryPolicyBase.DefaultMaxPageSize, GridQueryPolicyBase.DefaultDefaultPageSize);
        var search = GridQueryPolicyBase.NormalizeSearch(request.Search);
        var sorts = (request.Sort ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s.Field) && AllowedFields.Contains(s.Field.Trim()))
            .Select(s => new GridSortRequest(
                s.Field.Trim(),
                string.Equals(s.Direction, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc"))
            .Take(GridQueryPolicyBase.DefaultMaxSortCount)
            .ToList();
        if (sorts.Count == 0)
            sorts.Add(new GridSortRequest("createdAt", "desc"));

        var filters = new List<GridFilterRequest>();
        foreach (var filter in request.Filters ?? [])
        {
            if (string.IsNullOrWhiteSpace(filter.Field)
                || !AllowedFields.Contains(filter.Field.Trim())
                || !OperatorsByField.TryGetValue(filter.Field.Trim(), out var allowedOps))
                throw GridQueryValidationException.FilterFieldInvalid();
            var op = (filter.Operator ?? string.Empty).Trim();
            if (!allowedOps.Contains(op))
                throw GridQueryValidationException.FilterOperatorInvalid();
            filters.Add(GridQueryPolicyBase.NormalizeFilter(filter with { Field = filter.Field.Trim() }));
        }

        return new GridQueryRequest(page, pageSize, search, sorts, filters, NormalizeAdvanced(request.AdvancedFilter));
    }

    private static GridAdvancedFilterExpression? NormalizeAdvanced(GridAdvancedFilterExpression? expression)
    {
        if (expression?.Conditions is not { Count: > 0 } conditions) return null;
        GridQueryPolicyBase.ValidateAdvancedConnectors(conditions.Count, expression.Connectors);
        var connectors = (expression.Connectors ?? []).Select(c => c.ToLowerInvariant()).ToList();
        var normalized = new List<GridAdvancedFilterCondition>();
        foreach (var condition in conditions)
        {
            if (string.IsNullOrWhiteSpace(condition.Field)
                || !AllowedFields.Contains(condition.Field.Trim())
                || !OperatorsByField.TryGetValue(condition.Field.Trim(), out var allowedOps))
                throw GridQueryValidationException.AdvancedFieldInvalid();
            var op = (condition.Operator ?? string.Empty).Trim();
            if (!allowedOps.Contains(op))
                throw GridQueryValidationException.FilterOperatorInvalid();
            normalized.Add(new GridAdvancedFilterCondition(
                string.IsNullOrWhiteSpace(condition.Id) ? $"adv-{normalized.Count}" : condition.Id.Trim(),
                condition.Field.Trim(), op,
                GridQueryPolicyBase.NormalizeScalar(condition.Value),
                GridQueryPolicyBase.NormalizeScalar(condition.ValueTo),
                condition.Values?.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim())
                    .Distinct(StringComparer.Ordinal).Take(20).ToList()));
        }
        return new GridAdvancedFilterExpression(normalized, connectors);
    }
}

public sealed record QueryAdminReturnsGridQuery(GridQueryRequest Request)
    : IRequest<Result<GridPageResponse<AdminReturnWorkQueueRow>>>;

public sealed class QueryAdminReturnsGridHandler(IAdminReturnGridQuery query)
    : IRequestHandler<QueryAdminReturnsGridQuery, Result<GridPageResponse<AdminReturnWorkQueueRow>>>
{
    public async Task<Result<GridPageResponse<AdminReturnWorkQueueRow>>> Handle(
        QueryAdminReturnsGridQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var normalized = AdminReturnGridQueryPolicy.Instance.Normalize(request.Request);
            return Result.Success(await query.QueryAsync(normalized, cancellationToken));
        }
        catch (GridQueryValidationException ex)
        {
            return Result.Failure<GridPageResponse<AdminReturnWorkQueueRow>>(new SemanticError(ex.ErrorCode));
        }
    }
}
