using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;

using Tooba.Catalog.Contracts;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Fulfillment.Contracts.Returns;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Application.ReturnRequests.Ports;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Party.Contracts.Ports;
using Tooba.Persistence.Grid;
using Tooba.Returns.Domain.Aggregates;
using Tooba.Returns.Domain.ValueObjects;
using Tooba.Returns.Infrastructure.Persistence;

namespace Tooba.Returns.Infrastructure.Queries;

/// <summary>پرس‌وجوی DB-native صف کار مرجوعی Admin با batch map و بدون N+1.</summary>
public sealed class AdminReturnGridQueryEngine : IAdminReturnGridQuery
{
    private readonly ReturnsDbContext _db;
    private readonly IOrderGridEnrichmentReader _orders;
    private readonly IPartyLookup _parties;
    private readonly ICatalogVariantLookup _catalog;
    private readonly IFulfillmentReturnReader _fulfillment;
    private readonly IClock _clock;

    /// <summary>موتور گرید صف کار مرجوعی را با contextهای لازم می‌سازد.</summary>
    public AdminReturnGridQueryEngine(
        ReturnsDbContext db,
        IOrderGridEnrichmentReader orders,
        IPartyLookup parties,
        ICatalogVariantLookup catalog,
        IFulfillmentReturnReader fulfillment,
        IClock clock)
    {
        _db = db;
        _orders = orders;
        _parties = parties;
        _catalog = catalog;
        _fulfillment = fulfillment;
        _clock = clock;
    }

    /// <summary>
    /// TB-275: صفحه‌بندی صف کار مرجوعی از ONE classification of the search/column constrained set. The stage selector,
    /// the page and every strip count are read from that single in-memory classification instead of a second SQL
    /// predicate, so a badge can never disagree with the rows it selects.
    /// </summary>
    public async Task<AdminReturnWorkQueuePage> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        IQueryable<ReturnRequest> q = _db.ReturnRequests.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            var sellerIds = await _parties.SearchIdsByDisplayNameAsync(term, 200, cancellationToken);
            var matchingOrderIds = await _orders.SearchSellerOrderIdsByOrderNumberAsync(term, 200, cancellationToken);
            var matchingCheckoutIds = await _orders.SearchCheckoutIdsByRecipientAsync(term, 200, cancellationToken);
            q = q.Where(x =>
                x.Reason != null && x.Reason.ToLower().Contains(term)
                || sellerIds.Contains(x.SellerPartyId)
                || matchingOrderIds.Contains(x.SellerOrderId)
                || matchingCheckoutIds.Contains(x.CheckoutId));
        }

        // The work selector is answered by the classification itself, never by a SQL guess about a stage.
        var selection = AdminReturnWorkQueueSelection.Canonicalize(ReadSelectionToken(request));
        foreach (var filter in request.Filters)
        {
            if (string.Equals(filter.Field, "workCategory", StringComparison.Ordinal))
            {
                continue;
            }

            q = await ApplyFilterAsync(q, filter, cancellationToken);
        }

        var advancedIds = await EvaluateAdvancedAsync(request.AdvancedFilter, cancellationToken);
        if (advancedIds is not null)
        {
            q = q.Where(x => advancedIds.Contains(x.ReturnRequestId));
        }

        var facts = await q
            .Select(x => new CaseFact(
                x.ReturnRequestId,
                x.SellerOrderId,
                x.CheckoutId,
                x.SellerPartyId,
                x.RequestedByUserId,
                x.Status,
                x.Origin,
                x.Reason,
                x.Currency,
                x.RefundAmount,
                x.PaymentId,
                x.RefundDestination,
                x.CreatedAt,
                x.UpdatedAt,
                x.SourceFulfillmentId,
                x.SourceShipmentId,
                x.SourceReceiptId,
                x.SourceClaimId,
                x.SourceOccurrenceKey,
                x.ClosurePhase,
                x.ObligationClosedAt,
                x.StockSettledAt,
                x.ReleasedHeldQuantity))
            .ToListAsync(cancellationToken);

        var classified = await ClassifyAsync(facts, cancellationToken);
        var counts = new AdminReturnWorkCounts(
            classified.Count(x => x.Work.IsOpen),
            classified.Count(x => x.Work.Category == ReturnWorkCategory.Receiving),
            classified.Count(x => x.Work.Category == ReturnWorkCategory.Decision),
            classified.Count(x => x.Work.Category == ReturnWorkCategory.Refund),
            classified.Count(x => x.Work.Category == ReturnWorkCategory.RefundFailed),
            classified.Count(x => x.Work.Category == ReturnWorkCategory.Disposition),
            classified.Count(x => !x.Work.IsOpen));

        var selected = classified
            .Where(x => AdminReturnWorkQueueSelection.Matches(x.Work, selection))
            .ToList();

        var sort = request.Sort.FirstOrDefault() ?? new GridSortRequest("createdAt", "desc");
        var ordered = Sort(selected, sort).ToList();
        var pageRows = ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var items = await MapAsync(pageRows, cancellationToken);
        return new AdminReturnWorkQueuePage(items, request.Page, request.PageSize, ordered.Count, counts);
    }

    private static string? ReadSelectionToken(GridQueryRequest request)
    {
        var filter = request.Filters.FirstOrDefault(x => string.Equals(x.Field, "workCategory", StringComparison.Ordinal));
        return filter?.Values?.FirstOrDefault() ?? filter?.Value;
    }

    /// <summary>
    /// TB-275: classifies every row of the constrained set from the SAME stored facts the case detail reads: items,
    /// refund attempts, the durable decision, the recorded receipt occurrences and the line snapshots. The stage and
    /// the executable actions then come from <see cref="ReturnWorkPolicy"/> — the one authority the workspace uses too,
    /// so a queue bucket can never advertise work the case does not have.
    /// </summary>
    private async Task<List<ClassifiedCase>> ClassifyAsync(
        List<CaseFact> facts,
        CancellationToken cancellationToken)
    {
        if (facts.Count == 0)
        {
            return [];
        }

        var ids = facts.Select(x => x.ReturnRequestId).ToList();
        var items = await _db.ReturnItems.AsNoTracking()
            .Where(x => ids.Contains(x.ReturnRequestId))
            .ToListAsync(cancellationToken);
        var attempts = await _db.RefundAttempts.AsNoTracking()
            .Where(x => ids.Contains(x.ReturnRequestId))
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var decisions = await _db.ReturnDecisions.AsNoTracking()
            .Where(x => ids.Contains(x.ReturnRequestId))
            .Select(x => new { x.ReturnRequestId, x.Decision, x.Reason })
            .ToListAsync(cancellationToken);
        var receiptHeaders = await _db.ReturnReceipts.AsNoTracking()
            .Where(x => ids.Contains(x.ReturnRequestId))
            .Select(x => new { x.ReturnReceiptId, x.ReturnRequestId })
            .ToListAsync(cancellationToken);
        var receiptLines = await _db.ReturnReceiptLines.AsNoTracking()
            .Where(x => ids.Contains(x.ReturnRequestId))
            .Select(x => new { x.ReturnReceiptId, x.ReturnRequestId, x.ReturnItemId, x.Quantity, x.Condition })
            .ToListAsync(cancellationToken);

        var itemsBy = items.GroupBy(x => x.ReturnRequestId).ToDictionary(g => g.Key, g => g.ToList());
        var attemptsBy = attempts.GroupBy(x => x.ReturnRequestId).ToDictionary(g => g.Key, g => g.ToList());
        var decisionBy = decisions.GroupBy(x => x.ReturnRequestId).ToDictionary(g => g.Key, g => g.First());
        var receiptIdsBy = receiptHeaders.GroupBy(x => x.ReturnRequestId).ToDictionary(g => g.Key, g => g.Select(x => x.ReturnReceiptId).ToArray());
        var receiptLinesBy = receiptLines.GroupBy(x => x.ReturnRequestId).ToDictionary(g => g.Key, g => g.ToList());

        var classified = new List<ClassifiedCase>(facts.Count);
        foreach (var fact in facts)
        {
            itemsBy.TryGetValue(fact.ReturnRequestId, out var caseItems);
            attemptsBy.TryGetValue(fact.ReturnRequestId, out var caseAttempts);
            decisionBy.TryGetValue(fact.ReturnRequestId, out var decision);
            receiptIdsBy.TryGetValue(fact.ReturnRequestId, out var owningReceiptIds);
            receiptLinesBy.TryGetValue(fact.ReturnRequestId, out var caseReceiptLines);
            caseItems ??= [];
            caseAttempts ??= [];
            owningReceiptIds ??= [];
            caseReceiptLines ??= [];

            ReturnReceivingSummary? receiving = null;
            if (fact.Origin != ReturnOrigin.CourierOrigin)
            {
                receiving = ReturnReceivingSummary.Describe(
                    caseItems.Select(x => (x.ReturnItemId, x.Quantity)),
                    caseReceiptLines.Select(x => ((Guid?)x.ReturnReceiptId, x.ReturnItemId, x.Quantity, x.Condition)),
                    owningReceiptIds);
            }

            var snapshot = BuildSnapshot(fact, caseItems, caseAttempts, decision?.Decision, decision?.Reason, receiving);
            var described = snapshot with { Recovery = ReturnRecoveryPolicy.Describe(snapshot) };
            var work = ReturnWorkPolicy.Describe(described);
            classified.Add(new ClassifiedCase(fact, described with { Work = work }, work));
        }

        return classified;
    }

    private static ReturnSnapshot BuildSnapshot(
        CaseFact fact,
        IReadOnlyList<ReturnItem> items,
        IReadOnlyList<RefundAttempt> attempts,
        string? recordedDecision,
        string? decisionReason,
        ReturnReceivingSummary? receiving) =>
        new(
            fact.ReturnRequestId,
            fact.SellerOrderId,
            fact.CheckoutId,
            fact.SellerPartyId,
            fact.RequestedByUserId,
            fact.Status,
            fact.Reason,
            fact.Currency,
            fact.RefundAmount,
            fact.PaymentId,
            fact.RefundDestination,
            fact.CreatedAt,
            fact.UpdatedAt,
            items.Select(x => new ReturnItemSnapshot(
                x.ReturnItemId,
                x.OrderLineId,
                x.Quantity,
                x.UnitPriceSnapshot,
                x.Currency,
                x.ReservationId,
                x.Condition,
                x.Disposition,
                x.SourceShipmentId,
                x.SourceReceiptId,
                receiving is not null && x.Disposition == ReturnItemDisposition.SaleableRestock
                    ? receiving.UsableOf(x.ReturnItemId)
                    : null)).ToArray(),
            attempts.Select(x => new RefundAttemptSnapshot(
                x.RefundAttemptId,
                x.PaymentId,
                x.Amount,
                x.Currency,
                x.Status,
                x.IdempotencyKey,
                x.ProviderReference,
                x.FailureCode,
                x.CreatedAt,
                x.CompletedAt)).ToArray(),
            fact.Origin,
            fact.SourceFulfillmentId,
            fact.SourceShipmentId,
            fact.SourceReceiptId,
            fact.SourceClaimId,
            fact.SourceOccurrenceKey,
            fact.ClosurePhase,
            fact.ObligationClosedAt,
            fact.StockSettledAt,
            fact.ReleasedHeldQuantity,
            RecordedDecision: recordedDecision,
            Receiving: receiving,
            DecisionReason: decisionReason);

    private async Task<HashSet<Guid>?> EvaluateAdvancedAsync(
        GridAdvancedFilterExpression? expression,
        CancellationToken cancellationToken)
    {
        if (expression?.Conditions is not { Count: > 0 })
        {
            return null;
        }

        var sets = new List<HashSet<Guid>>();
        foreach (var condition in expression.Conditions)
        {
            var filter = new GridFilterRequest(
                condition.Field,
                condition.Operator,
                condition.Value,
                condition.ValueTo,
                condition.Values);
            var filtered = await ApplyFilterAsync(_db.ReturnRequests.AsNoTracking(), filter, cancellationToken);
            var ids = await filtered.Select(x => x.ReturnRequestId).ToListAsync(cancellationToken);
            sets.Add(ids.ToHashSet());
        }

        return GridAdvancedFilterEvaluator.EvaluateLeftToRight(sets, expression.Connectors);
    }

    private async Task<IQueryable<ReturnRequest>> ApplyFilterAsync(
        IQueryable<ReturnRequest> source,
        GridFilterRequest filter,
        CancellationToken cancellationToken)
    {
        switch (filter.Field)
        {
            case "returnReference":
            case "orderReference":
            {
                var ids = await _orders.FilterSellerOrderIdsByOrderNumberAsync(
                    filter.Operator, filter.Value, filter.Values, 500, cancellationToken);
                return source.Where(x => ids.Contains(x.SellerOrderId));
            }
            case "customerDisplayName":
            {
                var ids = await _orders.FilterCheckoutIdsByRecipientAsync(
                    filter.Operator, filter.Value, filter.Values, 500, cancellationToken);
                return source.Where(x => ids.Contains(x.CheckoutId));
            }
            case "sellerDisplayName":
            {
                var ids = await _parties.FilterIdsByDisplayNameAsync(
                    filter.Operator, filter.Value, filter.Values, 500, cancellationToken);
                return source.Where(x => ids.Contains(x.SellerPartyId));
            }
            case "status":
            case "returnStatus":
            {
                var raw = (filter.Values?.FirstOrDefault() ?? filter.Value ?? string.Empty).Trim();
                if (raw.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                {
                    return source.Where(x =>
                        x.Status == ReturnRequestStatus.Approved
                        || x.Status == ReturnRequestStatus.RefundProcessing
                        || x.Status == ReturnRequestStatus.RefundFailed);
                }

                return EfGridQuery.ApplyEnumFilter(source, x => x.Status, filter);
            }
            case "refundStatus":
            {
                var raw = (filter.Values?.FirstOrDefault() ?? filter.Value ?? string.Empty).Trim().ToLowerInvariant();
                return raw switch
                {
                    "none" => source.Where(x =>
                        x.Status == ReturnRequestStatus.Requested
                        || x.Status == ReturnRequestStatus.Rejected
                        || x.Status == ReturnRequestStatus.Cancelled),
                    "pending" => source.Where(x =>
                        x.Status == ReturnRequestStatus.Approved
                        || x.Status == ReturnRequestStatus.RefundProcessing),
                    "failed" => source.Where(x => x.Status == ReturnRequestStatus.RefundFailed),
                    "completed" => source.Where(x => x.Status == ReturnRequestStatus.Completed),
                    _ => source,
                };
            }
            case "createdAt":
            case "updatedAt":
                return filter.Field == "updatedAt"
                    ? EfGridQuery.ApplyDateFilter(source, x => x.UpdatedAt, filter)
                    : EfGridQuery.ApplyDateFilter(source, x => x.CreatedAt, filter);
            default:
                return source;
        }
    }

    /// <summary>
    /// TB-275: the server's own order over the classified set. The module default stays <c>createdAt desc</c> and is
    /// applied HERE, once, never re-applied by the client and never replaced by a synthetic column.
    /// </summary>
    private static IEnumerable<ClassifiedCase> Sort(IEnumerable<ClassifiedCase> rows, GridSortRequest sort)
    {
        var asc = sort.Direction == "asc";
        return sort.Field switch
        {
            "status" or "returnStatus" => asc
                ? rows.OrderBy(x => x.Snapshot.Status).ThenBy(x => x.Snapshot.ReturnRequestId)
                : rows.OrderByDescending(x => x.Snapshot.Status).ThenBy(x => x.Snapshot.ReturnRequestId),
            "updatedAt" => asc
                ? rows.OrderBy(x => x.Snapshot.UpdatedAt).ThenBy(x => x.Snapshot.ReturnRequestId)
                : rows.OrderByDescending(x => x.Snapshot.UpdatedAt).ThenBy(x => x.Snapshot.ReturnRequestId),
            _ => asc
                ? rows.OrderBy(x => x.Snapshot.CreatedAt).ThenBy(x => x.Snapshot.ReturnRequestId)
                : rows.OrderByDescending(x => x.Snapshot.CreatedAt).ThenBy(x => x.Snapshot.ReturnRequestId),
        };
    }

    private async Task<IReadOnlyList<AdminReturnWorkQueueRow>> MapAsync(
        List<ClassifiedCase> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var sellerOrderIds = rows.Select(x => x.Fact.SellerOrderId).Distinct().ToList();
        var checkoutIds = rows.Select(x => x.Fact.CheckoutId).Distinct().ToList();
        var sellerIds = rows.Select(x => x.Fact.SellerPartyId).Distinct().ToList();

        // The stable case ordinal of one seller order: the creation order of its cases, which never changes.
        var siblingCases = await _db.ReturnRequests.AsNoTracking()
            .Where(x => sellerOrderIds.Contains(x.SellerOrderId))
            .Select(x => new { x.ReturnRequestId, x.SellerOrderId, x.CreatedAt })
            .ToListAsync(cancellationToken);
        var ordinalBy = siblingCases
            .GroupBy(x => x.SellerOrderId)
            .SelectMany(group => group
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.ReturnRequestId)
                .Select((row, index) => (row.ReturnRequestId, Ordinal: index + 1)))
            .ToDictionary(x => x.ReturnRequestId, x => x.Ordinal);

        var orderBy = await _orders.GetOrderNumbersAsync(sellerOrderIds, cancellationToken);
        var recipientBy = await _orders.GetRecipientNamesAsync(checkoutIds, cancellationToken);
        var sellerBy = await _parties.GetDisplayNamesAsync(sellerIds, cancellationToken);

        var lineIds = rows
            .SelectMany(x => x.Snapshot.Items.Select(item => item.OrderLineId))
            .Distinct()
            .ToList();
        var lineBy = await _orders.GetLinesAsync(lineIds, cancellationToken);
        var variantIds = lineBy.Values.Select(x => x.CatalogVariantId).Distinct().ToList();
        var titles = await _catalog.GetVariantTitlesAsync(variantIds, cancellationToken);
        var deliveredBySeller = await _fulfillment.GetLastDeliveredAtBySellerOrderIdsAsync(sellerOrderIds, cancellationToken);

        return rows.Select(row =>
        {
            var fact = row.Fact;
            var snapshot = row.Snapshot;
            orderBy.TryGetValue(fact.SellerOrderId, out var orderNumber);
            recipientBy.TryGetValue(fact.CheckoutId, out var recipientName);
            sellerBy.TryGetValue(fact.SellerPartyId, out var sellerName);
            deliveredBySeller.TryGetValue(fact.SellerOrderId, out var lastDeliveredAt);

            var lines = snapshot.Items
                .Select(item =>
                {
                    lineBy.TryGetValue(item.OrderLineId, out var line);
                    var title = "کالای سفارش";
                    if (line is not null
                        && titles.TryGetValue(line.CatalogVariantId, out var variantTitle)
                        && !string.IsNullOrWhiteSpace(variantTitle))
                    {
                        title = variantTitle;
                    }

                    return new AdminReturnWorkQueueLine(
                        title,
                        item.Quantity,
                        string.IsNullOrWhiteSpace(line?.UnitCodeSnapshot) ? "واحد" : line!.UnitCodeSnapshot!,
                        item.Condition == ReturnItemCondition.Damaged ? "Damaged" : "Usable");
                })
                .ToArray();

            var firstLine = snapshot.Items.FirstOrDefault();
            lineBy.TryGetValue(firstLine?.OrderLineId ?? Guid.Empty, out var primaryLine);
            var eligibility = primaryLine is null
                ? "—"
                : AdminReturnQueueFilters.ComposeEligibilitySummary(
                    primaryLine.IsReturnableSnapshot,
                    primaryLine.ReturnWindowDaysSnapshot,
                    primaryLine.ReturnPolicyLabelSnapshot,
                    lastDeliveredAt,
                    _clock.UtcNow);

            var productLabel = lines.Length switch
            {
                0 => "کالای سفارش",
                1 => lines[0].ProductLabel,
                _ => $"{lines[0].ProductLabel} + {lines.Length - 1} قلم دیگر",
            };

            ordinalBy.TryGetValue(fact.ReturnRequestId, out var ordinal);
            var returnReference = string.IsNullOrWhiteSpace(orderNumber)
                ? "مرجوعی"
                : $"RET-{orderNumber}-{(ordinal > 0 ? ordinal : 1)}";

            return new AdminReturnWorkQueueRow(
                fact.ReturnRequestId,
                fact.SellerOrderId,
                fact.CheckoutId,
                fact.SellerPartyId,
                returnReference,
                string.IsNullOrWhiteSpace(orderNumber) ? "" : orderNumber!,
                string.IsNullOrWhiteSpace(recipientName) ? "مشتری" : recipientName!,
                string.IsNullOrWhiteSpace(sellerName) ? "فروشنده" : sellerName!,
                productLabel,
                snapshot.Items.Sum(x => x.Quantity),
                lines.Length == 1 ? lines[0].UnitLabel : "قلم",
                AdminReturnQueueFilters.ComposeReturnStatus(fact.Status),
                AdminReturnQueueFilters.ComposeRefundStatus(
                    fact.Status,
                    snapshot.RefundAttempts.Select(x => x.Status).ToArray()),
                eligibility,
                fact.CreatedAt,
                fact.UpdatedAt,
                row.Work.ActionCodes,
                lines,
                row.Work.CategoryCode,
                row.Work.BlockerCode,
                row.Work.NeedsOperator,
                row.Work.PendingRefundAttemptId,
                row.Work.FailedRefundAttemptId);
        }).ToList();
    }

    /// <summary>The minimal stored case facts the classification needs; never a second lifecycle.</summary>
    private sealed record CaseFact(
        Guid ReturnRequestId,
        Guid SellerOrderId,
        Guid CheckoutId,
        Guid SellerPartyId,
        Guid RequestedByUserId,
        ReturnRequestStatus Status,
        ReturnOrigin Origin,
        string? Reason,
        string Currency,
        decimal RefundAmount,
        Guid? PaymentId,
        RefundDestination RefundDestination,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        Guid? SourceFulfillmentId,
        Guid? SourceShipmentId,
        Guid? SourceReceiptId,
        Guid? SourceClaimId,
        string? SourceOccurrenceKey,
        CourierOriginClosurePhase ClosurePhase,
        DateTimeOffset? ObligationClosedAt,
        DateTimeOffset? StockSettledAt,
        decimal ReleasedHeldQuantity);

    private sealed record ClassifiedCase(CaseFact Fact, ReturnSnapshot Snapshot, ReturnWorkDescription Work);
}
