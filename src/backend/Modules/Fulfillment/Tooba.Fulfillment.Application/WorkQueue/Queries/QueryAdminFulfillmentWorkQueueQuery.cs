using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.WorkQueue.Models;
using Tooba.Fulfillment.Application.Fulfillments.Ports;

namespace Tooba.Fulfillment.Application.WorkQueue.Queries;

/// <summary>Module-owned fulfillment work-queue Normalize — whitelist, refusal of unevaluable selections, no fake sort.</summary>
public sealed class AdminFulfillmentGridQueryPolicy : IGridQueryPolicy
{
    public static AdminFulfillmentGridQueryPolicy Instance { get; } = new();

    private static readonly HashSet<string> FilterFields = new(StringComparer.Ordinal)
    {
        "orderReference", "recipientName", "fulfillmentId", "checkoutId", "cityName",
        "sellerPartyId", "sellerDisplayName", "shippingMethodCode", "shippingMethodLabel",
        "shipmentCount", "status", "queueFilter", "createdAt", "updatedAt",
        // TB-271: the ONE job-stage selection and the independent time scope. They are TOP-LEVEL selections only:
        // never sortable columns and never an advanced-filter condition (see AdvancedFilterRefusedFields).
        "jobStage", "timeScope",
    };

    /// <summary>
    /// TB-271-R1: the ACTUAL engine whitelist for an explicit column sort. It deliberately excludes the selection
    /// tokens (<c>jobStage</c>/<c>timeScope</c>) and the legacy <c>queueFilter</c> token: containing a filter token in
    /// the filter whitelist must never make it a sortable column.
    /// </summary>
    private static readonly HashSet<string> SortableFields = new(StringComparer.Ordinal)
    {
        "fulfillmentId", "checkoutId", "orderReference", "cityName", "status",
        "shippingMethodCode", "shippingMethodLabel", "sellerPartyId", "sellerDisplayName",
        "recipientName", "shipmentCount", "createdAt", "updatedAt",
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

    /// <summary>
    /// TB-271-R1: fields the advanced expression may NOT address. They are single-valued selections with their own
    /// documented top-level semantics, so accepting them as an AND/OR condition would look applied while being
    /// ignored by the evaluator. They are refused with the stable advanced-field error before any read.
    /// </summary>
    private static readonly HashSet<string> AdvancedFilterRefusedFields = new(StringComparer.Ordinal)
    {
        "jobStage", "timeScope", "queueFilter",
    };

    private static readonly HashSet<string> SelectionFields = new(StringComparer.Ordinal)
    {
        "jobStage", "timeScope", "queueFilter",
    };

    private static readonly HashSet<string> SelectionOperators = new(StringComparer.Ordinal)
    {
        "equals", "in",
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
        // TB-271: the stage/time tokens are server-side selections, validated as enums (values) with no column sort.
        ["jobStage"] = GridQueryOperators.Enum,
        ["timeScope"] = GridQueryOperators.Enum,
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
        // TB-271-R1: an ABSENT sort stays absent. Injecting a synthetic `updatedAt desc` here made the documented
        // job-priority ordering unreachable on a clean entry, and there is no way to tell a synthetic default from a
        // deliberate user/saved-view `updatedAt desc`. So the engine's overdue → today → undated → future ordering
        // runs exactly when the client genuinely sent no sort, and any explicit sort is preserved verbatim.
        var sorts = (request.Sort ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s.Field)
                && (SortableFields.Contains(s.Field.Trim()) || SortableOnlyFields.Contains(s.Field.Trim())))
            .Select(s => new GridSortRequest(
                s.Field.Trim(),
                string.Equals(s.Direction, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc"))
            .Take(GridQueryPolicyBase.DefaultMaxSortCount)
            .ToList();

        var filters = new List<GridFilterRequest>();
        foreach (var filter in request.Filters ?? [])
        {
            if (string.IsNullOrWhiteSpace(filter.Field)
                || !FilterFields.Contains(filter.Field.Trim())
                || !OperatorsByField.TryGetValue(filter.Field.Trim(), out var allowedOps))
                throw GridQueryValidationException.FilterFieldInvalid();
            var field = filter.Field.Trim();
            var op = (filter.Operator ?? string.Empty).Trim();
            if (!allowedOps.Contains(op))
                throw GridQueryValidationException.FilterOperatorInvalid();
            var normalizedFilter = GridQueryPolicyBase.NormalizeFilter(filter with { Field = field });
            if (SelectionFields.Contains(field))
            {
                EnsureSingleSelectionValue(normalizedFilter);
            }

            filters.Add(normalizedFilter);
        }

        return new GridQueryRequest(page, pageSize, search, sorts, filters, NormalizeAdvanced(request.AdvancedFilter));
    }

    /// <summary>
    /// TB-271-R1: one stage and one scope are SINGLE-VALUED selections. `notEqual`/`notIn`/`blank` or a multi-value
    /// input cannot mean "this stage", so they are refused instead of being silently read as the first token.
    /// </summary>
    private static void EnsureSingleSelectionValue(GridFilterRequest filter)
    {
        if (!SelectionOperators.Contains(filter.Operator))
        {
            throw GridQueryValidationException.FilterOperatorInvalid();
        }

        var values = (filter.Values ?? [])
            .Concat(string.IsNullOrWhiteSpace(filter.Value) ? [] : [filter.Value!])
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();
        if (values.Count != 1)
        {
            throw GridQueryValidationException.FilterOperatorInvalid();
        }
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
                || !FilterFields.Contains(condition.Field.Trim())
                || !OperatorsByField.TryGetValue(condition.Field.Trim(), out var allowedOps)
                // TB-271-R1: a stage/scope/legacy token inside an AND/OR expression cannot be evaluated by the
                // owning filter engine, so it is REFUSED here rather than silently returning the unfiltered source.
                || AdvancedFilterRefusedFields.Contains(condition.Field.Trim()))
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
            // TB-271-R1: ONE call — the page and the counts come from a single classification under a single
            // server now. The split QueryAsync+CountAsync path is never used by the live endpoint.
            var snapshot = await _query.QueryWithCountsAsync(normalized, cancellationToken);
            return Result.Success(new AdminFulfillmentWorkQueuePage(
                snapshot.Items,
                snapshot.Page,
                snapshot.PageSize,
                snapshot.TotalCount,
                snapshot.Counts));
        }
        catch (GridQueryValidationException ex)
        {
            return Result.Failure<AdminFulfillmentWorkQueuePage>(new SemanticError(ex.ErrorCode));
        }
    }
}
