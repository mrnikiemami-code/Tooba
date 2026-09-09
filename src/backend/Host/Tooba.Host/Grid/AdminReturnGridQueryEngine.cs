using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Grid;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Host.Admin;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Returns.Domain;
using Tooba.Returns.Infrastructure.Persistence;

namespace Tooba.Host.Grid;

/// <summary>پرس‌وجوی DB-native صف کار مرجوعی Admin با batch map و بدون N+1.</summary>
public sealed class AdminReturnGridQueryEngine
{
    private readonly ReturnsDbContext _db;
    private readonly OrderDbContext _orders;
    private readonly PartyDbContext _parties;
    private readonly CatalogDbContext _catalog;
    private readonly FulfillmentDbContext _fulfillment;

    /// <summary>موتور گرید صف کار مرجوعی را با contextهای لازم می‌سازد.</summary>
    public AdminReturnGridQueryEngine(
        ReturnsDbContext db,
        OrderDbContext orders,
        PartyDbContext parties,
        CatalogDbContext catalog,
        FulfillmentDbContext fulfillment)
    {
        _db = db;
        _orders = orders;
        _parties = parties;
        _catalog = catalog;
        _fulfillment = fulfillment;
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
            var sellerIds = await _parties.Parties.AsNoTracking()
                .Where(p => p.DisplayName.ToLower().Contains(term))
                .Select(p => p.PartyId)
                .Take(200)
                .ToListAsync(cancellationToken);
            var matchingOrderIds = await _orders.SellerOrders.AsNoTracking()
                .Where(o => o.OrderNumber.ToLower().Contains(term))
                .Select(o => o.SellerOrderId)
                .Take(200)
                .ToListAsync(cancellationToken);
            var matchingCheckoutIds = await _orders.Checkouts.AsNoTracking()
                .Where(c => c.RecipientName.ToLower().Contains(term))
                .Select(c => c.CheckoutId)
                .Take(200)
                .ToListAsync(cancellationToken);
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
        return await AdminEfGridQuery.PageAsync(
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
                var orders = _orders.SellerOrders.AsNoTracking().AsQueryable();
                orders = AdminEfGridQuery.ApplyTextFilter(orders, x => x.OrderNumber, filter);
                var ids = await orders.Select(x => x.SellerOrderId).Take(500).ToListAsync(cancellationToken);
                return source.Where(x => ids.Contains(x.SellerOrderId));
            }
            case "customerDisplayName":
            {
                var checkouts = _orders.Checkouts.AsNoTracking().AsQueryable();
                checkouts = AdminEfGridQuery.ApplyTextFilter(checkouts, x => x.RecipientName, filter);
                var ids = await checkouts.Select(x => x.CheckoutId).Take(500).ToListAsync(cancellationToken);
                return source.Where(x => ids.Contains(x.CheckoutId));
            }
            case "sellerDisplayName":
            {
                var parties = _parties.Parties.AsNoTracking().AsQueryable();
                parties = AdminEfGridQuery.ApplyTextFilter(parties, x => x.DisplayName, filter);
                var ids = await parties.Select(x => x.PartyId).Take(500).ToListAsync(cancellationToken);
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

                return AdminEfGridQuery.ApplyEnumFilter(source, x => x.Status, filter);
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
                    ? AdminEfGridQuery.ApplyDateFilter(source, x => x.UpdatedAt, filter)
                    : AdminEfGridQuery.ApplyDateFilter(source, x => x.CreatedAt, filter);
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
        var orderNumbers = await _orders.SellerOrders.AsNoTracking()
            .Where(x => sellerOrderIds.Contains(x.SellerOrderId))
            .Select(x => new { x.SellerOrderId, x.OrderNumber })
            .ToListAsync(cancellationToken);
        var recipients = await _orders.Checkouts.AsNoTracking()
            .Where(x => checkoutIds.Contains(x.CheckoutId))
            .Select(x => new { x.CheckoutId, x.RecipientName })
            .ToListAsync(cancellationToken);
        var sellers = await _parties.Parties.AsNoTracking()
            .Where(x => sellerIds.Contains(x.PartyId))
            .Select(x => new { x.PartyId, x.DisplayName })
            .ToListAsync(cancellationToken);

        var lineIds = items.Select(x => x.OrderLineId).Distinct().ToList();
        var lines = lineIds.Count == 0
            ? []
            : await _orders.Lines.AsNoTracking()
                .Where(x => lineIds.Contains(x.LineId))
                .Select(x => new
                {
                    x.LineId,
                    x.CatalogVariantId,
                    x.UnitCodeSnapshot,
                    x.QuantityDecimalPlacesSnapshot,
                    x.ReturnPolicyLabelSnapshot,
                    x.IsReturnableSnapshot,
                    x.ReturnWindowDaysSnapshot,
                })
                .ToListAsync(cancellationToken);
        var variantIds = lines.Select(x => x.CatalogVariantId).Distinct().ToList();
        var titles = await LoadVariantTitlesAsync(variantIds, cancellationToken);
        var deliveredBySeller = await LoadLastDeliveredAtAsync(sellerOrderIds, cancellationToken);

        var itemsBy = items.GroupBy(x => x.ReturnRequestId).ToDictionary(g => g.Key, g => g.ToList());
        var attemptsBy = attempts.GroupBy(x => x.ReturnRequestId).ToDictionary(g => g.Key, g => g.ToList());
        var orderBy = orderNumbers.ToDictionary(x => x.SellerOrderId, x => x.OrderNumber);
        var recipientBy = recipients.ToDictionary(x => x.CheckoutId, x => x);
        var sellerBy = sellers.ToDictionary(x => x.PartyId, x => x.DisplayName);
        var lineBy = lines.ToDictionary(x => x.LineId);

        return rows.Select(request =>
        {
            itemsBy.TryGetValue(request.ReturnRequestId, out var itemList);
            attemptsBy.TryGetValue(request.ReturnRequestId, out var attemptList);
            itemList ??= [];
            attemptList ??= [];
            var firstItem = itemList.FirstOrDefault();
            lineBy.TryGetValue(firstItem?.OrderLineId ?? Guid.Empty, out var line);
            orderBy.TryGetValue(request.SellerOrderId, out var orderNumber);
            recipientBy.TryGetValue(request.CheckoutId, out var checkout);
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
                    DateTimeOffset.UtcNow);
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
                string.IsNullOrWhiteSpace(checkout?.RecipientName) ? "مشتری" : checkout!.RecipientName,
                string.IsNullOrWhiteSpace(sellerName) ? "فروشنده" : sellerName,
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

    private async Task<Dictionary<Guid, DateTimeOffset?>> LoadLastDeliveredAtAsync(
        IReadOnlyCollection<Guid> sellerOrderIds,
        CancellationToken cancellationToken)
    {
        if (sellerOrderIds.Count == 0)
        {
            return [];
        }

        var units = await _fulfillment.Fulfillments.AsNoTracking()
            .Where(x => sellerOrderIds.Contains(x.SellerOrderId))
            .Select(x => new { x.SellerOrderId, x.FulfillmentId })
            .ToListAsync(cancellationToken);
        if (units.Count == 0)
        {
            return [];
        }

        var fulfillmentIds = units.Select(x => x.FulfillmentId).ToList();
        var deliveries = await _fulfillment.Shipments.AsNoTracking()
            .Where(x => fulfillmentIds.Contains(x.FulfillmentId) && x.DeliveredAt != null)
            .Select(x => new { x.FulfillmentId, x.DeliveredAt })
            .ToListAsync(cancellationToken);
        var deliveredByFulfillment = deliveries
            .GroupBy(x => x.FulfillmentId)
            .ToDictionary(g => g.Key, g => g.Max(x => x.DeliveredAt));
        var result = new Dictionary<Guid, DateTimeOffset?>();
        foreach (var unit in units)
        {
            deliveredByFulfillment.TryGetValue(unit.FulfillmentId, out var at);
            if (!result.TryGetValue(unit.SellerOrderId, out var existing) || (at is not null && (existing is null || at > existing)))
            {
                result[unit.SellerOrderId] = at;
            }
        }

        return result;
    }

    private async Task<Dictionary<Guid, string>> LoadVariantTitlesAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken)
    {
        if (variantIds.Count == 0)
        {
            return [];
        }

        var variants = await _catalog.Variants.AsNoTracking()
            .Where(x => variantIds.Contains(x.VariantId))
            .Select(x => new { x.VariantId, x.ProductId })
            .ToListAsync(cancellationToken);
        var productIds = variants.Select(x => x.ProductId).Distinct().ToList();
        var names = await _catalog.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Product
                && productIds.Contains(x.OwnerId)
                && x.FieldKey == "name")
            .ToListAsync(cancellationToken);
        var productNames = names.GroupBy(x => x.OwnerId).ToDictionary(
            x => x.Key,
            x => x.OrderBy(row => row.Locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase) ? 0 : 1).First().Value);
        return variants
            .Where(x => productNames.ContainsKey(x.ProductId))
            .ToDictionary(x => x.VariantId, x => productNames[x.ProductId]);
    }
}
