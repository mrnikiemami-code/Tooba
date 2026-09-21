using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;

using Tooba.Catalog.Contracts;
using Tooba.Fulfillment.Contracts.Returns;
using Tooba.Returns.Application.Models;
using Tooba.Returns.Application.Ports;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Party.Contracts;
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

    /// <summary>صفحه‌بندی و فیلتر DB-native صف کار مرجوعی.</summary>
    public async Task<GridPageResponse<AdminReturnWorkQueueRow>> QueryAsync(
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

        foreach (var filter in request.Filters)
        {
            q = await ApplyFilterAsync(q, filter, cancellationToken);
        }

        var advancedIds = await EvaluateAdvancedAsync(request.AdvancedFilter, cancellationToken);
        if (advancedIds is not null)
        {
            q = q.Where(x => advancedIds.Contains(x.ReturnRequestId));
        }

        var sort = request.Sort.FirstOrDefault() ?? new GridSortRequest("createdAt", "desc");
        return await EfGridQuery.PageAsync(
            q,
            request,
            filtered => Order(filtered, sort),
            MapPageAsync,
            cancellationToken);
    }

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
            case "queueFilter":
            {
                var raw = (filter.Values?.FirstOrDefault() ?? filter.Value ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(raw) || raw.Equals(AdminReturnQueueFilters.All, StringComparison.OrdinalIgnoreCase))
                {
                    return source;
                }

                // Predicates must be EF-translatable; do not call Matches() inside Where.
                return raw.ToLowerInvariant() switch
                {
                    AdminReturnQueueFilters.PendingReview =>
                        source.Where(x => x.Status == ReturnRequestStatus.Requested),
                    AdminReturnQueueFilters.Approved =>
                        source.Where(x =>
                            x.Status == ReturnRequestStatus.Approved
                            || x.Status == ReturnRequestStatus.RefundProcessing),
                    AdminReturnQueueFilters.ReceivedAwaitingRefund =>
                        source.Where(x => x.Status == ReturnRequestStatus.Approved),
                    AdminReturnQueueFilters.RefundPending =>
                        source.Where(x => x.Status == ReturnRequestStatus.RefundProcessing),
                    AdminReturnQueueFilters.RefundFailed =>
                        source.Where(x => x.Status == ReturnRequestStatus.RefundFailed),
                    AdminReturnQueueFilters.Completed =>
                        source.Where(x => x.Status == ReturnRequestStatus.Completed),
                    AdminReturnQueueFilters.Rejected =>
                        source.Where(x => x.Status == ReturnRequestStatus.Rejected),
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

    private IQueryable<ReturnRequest> Order(IQueryable<ReturnRequest> source, GridSortRequest sort)
    {
        var asc = sort.Direction == "asc";
        return sort.Field switch
        {
            "status" or "returnStatus" => asc
                ? source.OrderBy(x => x.Status).ThenBy(x => x.ReturnRequestId)
                : source.OrderByDescending(x => x.Status).ThenBy(x => x.ReturnRequestId),
            "updatedAt" => asc
                ? source.OrderBy(x => x.UpdatedAt).ThenBy(x => x.ReturnRequestId)
                : source.OrderByDescending(x => x.UpdatedAt).ThenBy(x => x.ReturnRequestId),
            _ => asc
                ? source.OrderBy(x => x.CreatedAt).ThenBy(x => x.ReturnRequestId)
                : source.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.ReturnRequestId),
        };
    }

    private async Task<IReadOnlyList<AdminReturnWorkQueueRow>> MapPageAsync(
        List<ReturnRequest> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var ids = rows.Select(x => x.ReturnRequestId).ToList();
        var sellerOrderIds = rows.Select(x => x.SellerOrderId).Distinct().ToList();
        var checkoutIds = rows.Select(x => x.CheckoutId).Distinct().ToList();
        var sellerIds = rows.Select(x => x.SellerPartyId).Distinct().ToList();

        var items = await _db.ReturnItems.AsNoTracking()
            .Where(x => ids.Contains(x.ReturnRequestId))
            .ToListAsync(cancellationToken);
        var attempts = await _db.RefundAttempts.AsNoTracking()
            .Where(x => ids.Contains(x.ReturnRequestId))
            .ToListAsync(cancellationToken);
        var orderBy = await _orders.GetOrderNumbersAsync(sellerOrderIds, cancellationToken);
        var recipientBy = await _orders.GetRecipientNamesAsync(checkoutIds, cancellationToken);
        var sellerBy = await _parties.GetDisplayNamesAsync(sellerIds, cancellationToken);

        var lineIds = items.Select(x => x.OrderLineId).Distinct().ToList();
        var lineBy = await _orders.GetLinesAsync(lineIds, cancellationToken);
        var variantIds = lineBy.Values.Select(x => x.CatalogVariantId).Distinct().ToList();
        var titles = await _catalog.GetVariantTitlesAsync(variantIds, cancellationToken);
        var deliveredBySeller = await _fulfillment.GetLastDeliveredAtBySellerOrderIdsAsync(sellerOrderIds, cancellationToken);

        var itemsBy = items.GroupBy(x => x.ReturnRequestId).ToDictionary(g => g.Key, g => g.ToList());
        var attemptsBy = attempts.GroupBy(x => x.ReturnRequestId).ToDictionary(g => g.Key, g => g.ToList());

        return rows.Select(request =>
        {
            itemsBy.TryGetValue(request.ReturnRequestId, out var itemList);
            attemptsBy.TryGetValue(request.ReturnRequestId, out var attemptList);
            itemList ??= [];
            attemptList ??= [];
            var firstItem = itemList.FirstOrDefault();
            lineBy.TryGetValue(firstItem?.OrderLineId ?? Guid.Empty, out var line);
            orderBy.TryGetValue(request.SellerOrderId, out var orderNumber);
            recipientBy.TryGetValue(request.CheckoutId, out var recipientName);
            sellerBy.TryGetValue(request.SellerPartyId, out var sellerName);
            var qty = itemList.Sum(x => x.Quantity);
            var product = "کالای سفارش";
            if (line is not null && titles.TryGetValue(line.CatalogVariantId, out var title) && !string.IsNullOrWhiteSpace(title))
            {
                product = title;
            }

            deliveredBySeller.TryGetValue(request.SellerOrderId, out var lastDeliveredAt);
            var eligibility = line is null
                ? "—"
                : AdminReturnQueueFilters.ComposeEligibilitySummary(
                    line.IsReturnableSnapshot,
                    line.ReturnWindowDaysSnapshot,
                    line.ReturnPolicyLabelSnapshot,
                    lastDeliveredAt,
                    _clock.UtcNow);
            var returnRef = !string.IsNullOrWhiteSpace(orderNumber)
                ? $"RET-{orderNumber}"
                : "مرجوعی";
            return new AdminReturnWorkQueueRow(
                request.ReturnRequestId,
                request.SellerOrderId,
                request.CheckoutId,
                request.SellerPartyId,
                returnRef,
                string.IsNullOrWhiteSpace(orderNumber) ? "" : orderNumber!,
                string.IsNullOrWhiteSpace(recipientName) ? "مشتری" : recipientName!,
                string.IsNullOrWhiteSpace(sellerName) ? "فروشنده" : sellerName!,
                product,
                qty,
                string.IsNullOrWhiteSpace(line?.UnitCodeSnapshot) ? "واحد" : line!.UnitCodeSnapshot!,
                AdminReturnQueueFilters.ComposeReturnStatus(request.Status),
                AdminReturnQueueFilters.ComposeRefundStatus(
                    request.Status,
                    attemptList.Select(x => x.Status).ToArray()),
                eligibility,
                request.CreatedAt,
                request.UpdatedAt,
                AdminReturnQueueFilters.ProjectActionCodes(request.Status));
        }).ToList();
    }
}
