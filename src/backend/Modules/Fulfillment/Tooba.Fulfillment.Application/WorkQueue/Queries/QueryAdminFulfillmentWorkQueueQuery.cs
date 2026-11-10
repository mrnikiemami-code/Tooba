using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.WorkQueue.Models;
using Tooba.Fulfillment.Application.Fulfillments.Ports;

namespace Tooba.Fulfillment.Application.WorkQueue.Queries;

/// <summary>Module-owned fulfillment work-queue Normalize — whitelist + default sort updatedAt desc.</summary>
public sealed class AdminFulfillmentGridQueryPolicy : IGridQueryPolicy
{
    public static AdminFulfillmentGridQueryPolicy Instance { get; } = new();

    private static readonly HashSet<string> AllowedFields = new(StringComparer.Ordinal)
    {
        "orderReference", "recipientName", "fulfillmentId", "checkoutId", "cityName",
        "sellerPartyId", "sellerDisplayName", "shippingMethodCode", "shippingMethodLabel",
        "shipmentCount", "status", "queueFilter", "createdAt", "updatedAt",
    };

    /// <summary>
    /// TB-260: the dispatch timeline columns are server-classified, so they may be sorted by the owning
    /// projection but are deliberately <b>not</b> filterable here — timing is filtered by the
    /// server-authoritative quick tabs, and a filter the engine cannot honour is rejected instead of
    /// being silently ignored while the grid looks filtered.
    /// </summary>
    private static readonly HashSet<string> SortableOnlyFields = new(StringComparer.Ordinal)
    {
        "dispatchDeadline", "dispatchBucket",
    };

    private static readonly Dictionary<string, HashSet<string>> OperatorsByField = new(StringComparer.Ordinal)
    {
        ["orderReference"] = GridQueryOperators.Text,
        ["recipientName"] = GridQueryOperators.Text,
        ["fulfillmentId"] = GridQueryOperators.Text,
        ["checkoutId"] = GridQueryOperators.Text,
        ["cityName"] = GridQueryOperators.Text,
        ["sellerPartyId"] = GridQueryOperators.Text,
        ["sellerDisplayName"] = GridQueryOperators.Text,
        ["shippingMethodCode"] = GridQueryOperators.Enum,
        ["shippingMethodLabel"] = GridQueryOperators.Text,
        ["shipmentCount"] = GridQueryOperators.Number,
        ["status"] = GridQueryOperators.Enum,
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
            .Where(s => !string.IsNullOrWhiteSpace(s.Field)
                && (AllowedFields.Contains(s.Field.Trim()) || SortableOnlyFields.Contains(s.Field.Trim())))
            .Select(s => new GridSortRequest(
                s.Field.Trim(),
                string.Equals(s.Direction, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc"))
            .Take(GridQueryPolicyBase.DefaultMaxSortCount)
            .ToList();
        if (sorts.Count == 0)
            sorts.Add(new GridSortRequest("updatedAt", "desc"));

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

public sealed record QueryAdminFulfillmentWorkQueueQuery(GridQueryRequest Request)
    : IRequest<Result<AdminFulfillmentWorkQueuePage>>;

public sealed class QueryAdminFulfillmentWorkQueueHandler
    : IRequestHandler<QueryAdminFulfillmentWorkQueueQuery, Result<AdminFulfillmentWorkQueuePage>>
{
    private readonly IAdminFulfillmentWorkQueueQuery _query;
    public QueryAdminFulfillmentWorkQueueHandler(IAdminFulfillmentWorkQueueQuery query) => _query = query;

    public async Task<Result<AdminFulfillmentWorkQueuePage>> Handle(
        QueryAdminFulfillmentWorkQueueQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var normalized = AdminFulfillmentGridQueryPolicy.Instance.Normalize(request.Request);
            var page = await _query.QueryAsync(normalized, cancellationToken);
            // TB-260: the counts describe the SAME filtered set the page came from, so a tab can never
            // show "3" while the paged list holds items the count never saw.
            var counts = await _query.CountAsync(normalized, cancellationToken);
            return Result.Success(new AdminFulfillmentWorkQueuePage(
                page.Items,
                page.Page,
                page.PageSize,
                page.TotalCount,
                counts));
        }
        catch (GridQueryValidationException ex)
        {
            return Result.Failure<AdminFulfillmentWorkQueuePage>(new SemanticError(ex.ErrorCode));
        }
    }
}
