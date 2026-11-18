using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Contracts.Returns;
using Tooba.Order.Contracts.Returns;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Application.ReturnRequests.Ports;
using Tooba.Returns.Domain.Aggregates;
using Tooba.Returns.Domain.ValueObjects;
using Tooba.Returns.Infrastructure.Persistence;

namespace Tooba.Returns.Infrastructure.Evaluators;

/// <summary>
/// SoT eligibility مرجوعی: Paid + Delivered + پنجرهٔ snapshot خط + باقیمانده؛ تسویه فروشنده هرگز چک نمی‌شود.
/// </summary>
public sealed class ReturnEligibilityEvaluator : IReturnEligibilityEvaluator
{
    /// <summary>پنجرهٔ پیش‌فرض وقتی snapshot خط در دسترس نباشد (legacy).</summary>
    public static readonly TimeSpan ReturnWindow = TimeSpan.FromDays(7);

    private readonly IOrderReturnReader _orders;
    private readonly IFulfillmentReturnReader _fulfillment;
    private readonly ReturnsDbContext _db;
    private readonly IClock _clock;

    /// <summary>ارزیاب را به درز Order/Fulfillment و schema returns وصل می‌کند.</summary>
    public ReturnEligibilityEvaluator(
        IOrderReturnReader orders,
        IFulfillmentReturnReader fulfillment,
        ReturnsDbContext db,
        IClock clock)
    {
        _orders = orders;
        _fulfillment = fulfillment;
        _db = db;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<ReturnEligibilityResult> EvaluateAsync(
        Guid sellerOrderId,
        CancellationToken cancellationToken,
        Guid? excludeReturnRequestId = null)
    {
        var orderContext = await _orders.GetReturnContextAsync(sellerOrderId, cancellationToken);
        if (orderContext is null)
        {
            return new ReturnEligibilityResult(
                sellerOrderId,
                Guid.Empty,
                false,
                ReturnEligibilityReasonCodes.OrderMissing,
                null,
                null,
                []);
        }

        if (!orderContext.IsPaid)
        {
            return Empty(
                orderContext,
                ReturnEligibilityReasonCodes.NotPaid,
                null,
                null);
        }

        var fulfillment = await _fulfillment.GetEligibilityAsync(sellerOrderId, cancellationToken);
        if (fulfillment is null)
        {
            return Empty(
                orderContext,
                ReturnEligibilityReasonCodes.FulfillmentMissing,
                null,
                null);
        }

        if (fulfillment.LastDeliveredAt is null)
        {
            return Empty(
                orderContext,
                ReturnEligibilityReasonCodes.NotDelivered,
                null,
                null);
        }

        var lastDeliveredAt = fulfillment.LastDeliveredAt.Value;
        var occupancy = await ReadOccupancyAsync(sellerOrderId, excludeReturnRequestId, cancellationToken);
        var now = _clock.UtcNow;
        var lines = BuildLines(orderContext, fulfillment, occupancy, lastDeliveredAt, now);
        var anyReturnablePolicy = orderContext.Lines.Any(x => x.IsReturnableSnapshot);
        if (!anyReturnablePolicy)
        {
            return new ReturnEligibilityResult(
                orderContext.SellerOrderId,
                orderContext.CheckoutId,
                false,
                ReturnEligibilityReasonCodes.NonReturnable,
                null,
                lastDeliveredAt,
                lines);
        }

        var eligibleUntil = ComputeEligibleUntil(orderContext, fulfillment, lastDeliveredAt);

        if (now > eligibleUntil)
        {
            return new ReturnEligibilityResult(
                orderContext.SellerOrderId,
                orderContext.CheckoutId,
                false,
                ReturnEligibilityReasonCodes.WindowExpired,
                eligibleUntil,
                lastDeliveredAt,
                lines);
        }

        if (lines.All(x => x.RemainingReturnableQuantity <= 0))
        {
            return new ReturnEligibilityResult(
                orderContext.SellerOrderId,
                orderContext.CheckoutId,
                false,
                ReturnEligibilityReasonCodes.NothingReturnable,
                eligibleUntil,
                lastDeliveredAt,
                lines);
        }

        return new ReturnEligibilityResult(
            orderContext.SellerOrderId,
            orderContext.CheckoutId,
            true,
            ReturnEligibilityReasonCodes.Eligible,
            eligibleUntil,
            lastDeliveredAt,
            lines);
    }

    /// <summary>تعداد مرجوعی فعال/تکمیل‌شده به ازای هر خط.</summary>
    public async Task<Dictionary<Guid, decimal>> GetAlreadyReturnedQuantitiesAsync(
        Guid sellerOrderId,
        CancellationToken cancellationToken)
    {
        var occupancy = await ReadOccupancyAsync(sellerOrderId, null, cancellationToken);
        return occupancy.Returned.ToDictionary(x => x.Key, x => x.Value);
    }

    /// <summary>
    /// TB-272 (C) / TB-279 (D): ONE read of the active occupying facts of a seller order, split into
    /// <list type="bullet">
    /// <item><description>the requested quantity per line (diagnostic fact, unchanged by this task);</description></item>
    /// <item><description>the durable per-slice allocations of active cases (the ONLY capacity a sibling may not take);</description></item>
    /// <item><description>the quantity of active cases that predate per-delivery allocation, which is subtracted
    /// conservatively from the oldest in-window slice instead of being ignored.</description></item>
    /// </list>
    /// </summary>
    private async Task<LineOccupancy> ReadOccupancyAsync(
        Guid sellerOrderId,
        Guid? excludeReturnRequestId,
        CancellationToken cancellationToken)
    {
        // TB-272 (C): a FAILED money attempt is not a cancelled physical intent. RefundFailed still occupies its
        // requested line quantities, so the same units can never fund a second, over-capacity request. Only
        // Rejected/Cancelled release capacity (they are absent from this set on purpose).
        var activeStatuses = new[]
        {
            ReturnRequestStatus.Requested,
            ReturnRequestStatus.Approved,
            ReturnRequestStatus.RefundProcessing,
            ReturnRequestStatus.RefundFailed,
            ReturnRequestStatus.Completed,
        };
        var requestIds = await _db.ReturnRequests.AsNoTracking()
            .Where(x => x.SellerOrderId == sellerOrderId && activeStatuses.Contains(x.Status))
            .Select(x => x.ReturnRequestId)
            .ToListAsync(cancellationToken);
        if (excludeReturnRequestId is { } excluded)
        {
            // TB-279: re-acquiring a case's OWN capacity must not be blocked by that same case's recorded quantity.
            requestIds = requestIds.Where(x => x != excluded).ToList();
        }

        // TB-279-R2 (C): a case whose status is no longer active can still physically HOLD goods, and its live
        // allocation rows are a real capacity claim. The allocation read below therefore covers every case of the
        // seller order, so an empty active-status set must never be answered as "nothing is occupied".
        var allCases = await _db.ReturnRequests.AsNoTracking()
            .Where(x => x.SellerOrderId == sellerOrderId)
            .Select(x => new { x.ReturnRequestId, x.Status })
            .ToListAsync(cancellationToken);
        if (allCases.Count == 0)
        {
            return LineOccupancy.Empty;
        }

        var allRequestIds = allCases.Select(x => x.ReturnRequestId).ToList();

        var items = requestIds.Count == 0
            ? new List<OccupiedItemRow>()
            : await _db.ReturnItems.AsNoTracking()
                .Where(x => requestIds.Contains(x.ReturnRequestId))
                .Select(x => new OccupiedItemRow(x.ReturnRequestId, x.OrderLineId, x.Quantity))
                .ToListAsync(cancellationToken);

        // Active (not released) allocations are the real capacity claim; a released row returned its quantity to the
        // pool when the case was rejected/cancelled. TB-279-R1 (F3): rejected/cancelled cases can still own HELD rows —
        // the goods they physically received stay claimed until a real handback — so the allocation read covers every
        // case of the seller order and lets the rows themselves (not the status) decide occupancy.
        var allocations = await _db.ReturnDeliveryAllocations.AsNoTracking()
            .Where(x => allRequestIds.Contains(x.ReturnRequestId) && x.ReleasedAt == null)
            .Select(x => new { x.ReturnRequestId, x.OrderLineId, x.SliceKey, x.Quantity })
            .ToListAsync(cancellationToken);
        if (excludeReturnRequestId is { } reacquiring)
        {
            allocations = allocations.Where(x => x.ReturnRequestId != reacquiring).ToList();
        }

        var allocatedRequestIds = allocations.Select(x => x.ReturnRequestId).ToHashSet();
        var returned = items
            .GroupBy(x => x.OrderLineId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        var legacyUnallocated = items
            .Where(x => !allocatedRequestIds.Contains(x.ReturnRequestId))
            .GroupBy(x => x.OrderLineId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        var allocatedElsewhere = allocations
            .GroupBy(x => x.SliceKey, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity), StringComparer.Ordinal);

        // TB-279-R3 (B2): a REJECTED/CANCELLED case owns whatever it physically received until the custody authority
        // records its actual handback. Allocation rows only answer that while they are still live; a legacy rejection
        // with real received goods and NO rows used to vanish from every read, letting the same units fund a second
        // claim. Its held quantity is therefore read from its own stored receiving/resolution/custody facts and added
        // to the line's unallocated legacy occupancy — which is NEVER guessed onto a slice.
        var releasedCaseIds = allCases
            .Where(x => x.Status is ReturnRequestStatus.Rejected or ReturnRequestStatus.Cancelled)
            .Select(x => x.ReturnRequestId)
            .Where(x => excludeReturnRequestId != x)
            .ToArray();
        if (releasedCaseIds.Length > 0)
        {
            var allocatedPairs = allocations
                .Select(x => (x.ReturnRequestId, x.OrderLineId))
                .ToHashSet();
            var heldByLine = await ReadRejectedHeldByLineAsync(
                releasedCaseIds,
                allocatedPairs,
                cancellationToken);
            foreach (var (orderLineId, quantity) in heldByLine)
            {
                if (quantity <= 0m)
                {
                    continue;
                }

                legacyUnallocated[orderLineId] =
                    (legacyUnallocated.TryGetValue(orderLineId, out var existing) ? existing : 0m) + quantity;
            }
        }

        return new LineOccupancy(returned, legacyUnallocated, allocatedElsewhere);
    }

    /// <summary>
    /// TB-279-R3 (B2): the outstanding held quantity of every REJECTED/CANCELLED case line whose delivery origin is not
    /// already claimed by one of its own LIVE allocation rows.
    /// <para>
    /// The quantity comes from the SAME receiving/resolution/custody authorities the write boundary uses, per item and
    /// condition. Contradictory stored receiving rows are never read as "nothing arrived": the conservative claim of the
    /// line is kept and the new claim that depends on the missing attribution is refused by the allocation policy.
    /// </para>
    /// </summary>
    private async Task<Dictionary<Guid, decimal>> ReadRejectedHeldByLineAsync(
        IReadOnlyList<Guid> releasedRequestIds,
        IReadOnlySet<(Guid ReturnRequestId, Guid OrderLineId)> allocatedPairs,
        CancellationToken cancellationToken)
    {
        var held = new Dictionary<Guid, decimal>();
        if (releasedRequestIds.Count == 0)
        {
            return held;
        }

        var items = await _db.ReturnItems.AsNoTracking()
            .Where(x => releasedRequestIds.Contains(x.ReturnRequestId))
            .Select(x => new { x.ReturnRequestId, x.ReturnItemId, x.OrderLineId, x.Quantity })
            .ToListAsync(cancellationToken);
        if (items.Count == 0)
        {
            return held;
        }

        var receiptHeaders = await _db.ReturnReceipts.AsNoTracking()
            .Where(x => releasedRequestIds.Contains(x.ReturnRequestId))
            .Select(x => new { x.ReturnRequestId, x.ReturnReceiptId })
            .ToListAsync(cancellationToken);
        var receiptLines = await _db.ReturnReceiptLines.AsNoTracking()
            .Where(x => releasedRequestIds.Contains(x.ReturnRequestId))
            .Select(x => new { x.ReturnRequestId, x.ReturnReceiptId, x.ReturnItemId, x.Quantity, x.Condition })
            .ToListAsync(cancellationToken);
        var planRows = await _db.ReturnItemResolutions.AsNoTracking()
            .Where(x => releasedRequestIds.Contains(x.ReturnRequestId))
            .Select(x => new
            {
                x.ReturnRequestId,
                x.ReturnItemId,
                x.Version,
                x.AcceptedUsableQuantity,
                x.AcceptedDamagedQuantity,
                x.RejectedReceivedUsableQuantity,
                x.RejectedReceivedDamagedQuantity,
            })
            .ToListAsync(cancellationToken);
        var custodyRows = await _db.ReturnItemCustodies.AsNoTracking()
            .Where(x => releasedRequestIds.Contains(x.ReturnRequestId))
            .Select(x => new { x.ReturnRequestId, x.ReturnItemId, x.Quantity, x.Condition })
            .ToListAsync(cancellationToken);

        foreach (var caseGroup in items.GroupBy(x => x.ReturnRequestId))
        {
            var caseItems = caseGroup.ToArray();
            var owningReceiptIds = receiptHeaders
                .Where(x => x.ReturnRequestId == caseGroup.Key)
                .Select(x => x.ReturnReceiptId)
                .ToArray();
            var summary = ReturnReceivingSummary.Describe(
                caseItems.Select(x => (x.ReturnItemId, x.Quantity)),
                receiptLines
                    .Where(x => x.ReturnRequestId == caseGroup.Key)
                    .Select(x => ((Guid?)x.ReturnReceiptId, x.ReturnItemId, x.Quantity, x.Condition)),
                owningReceiptIds);

            foreach (var item in caseItems)
            {
                if (allocatedPairs.Contains((caseGroup.Key, item.OrderLineId)))
                {
                    // This case line still owns a LIVE allocation row: the allocation read is its capacity claim, and
                    // counting its held quantity again would occupy the same goods twice.
                    continue;
                }

                var receivedLine = summary.Lines.FirstOrDefault(x => x.ReturnItemId == item.ReturnItemId);
                var received = new List<(decimal, ReturnItemCondition)>(2);
                if (receivedLine is { Usable: > 0m })
                {
                    received.Add((receivedLine.Usable, ReturnItemCondition.Usable));
                }

                if (receivedLine is { Damaged: > 0m })
                {
                    received.Add((receivedLine.Damaged, ReturnItemCondition.Damaged));
                }

                var handedBack = custodyRows
                    .Where(x => x.ReturnRequestId == caseGroup.Key && x.ReturnItemId == item.ReturnItemId)
                    .Select(x => (
                        x.Quantity,
                        string.Equals(x.Condition, nameof(ReturnItemCondition.Damaged), StringComparison.Ordinal)
                            ? ReturnItemCondition.Damaged
                            : ReturnItemCondition.Usable))
                    .ToArray();

                var planRow = planRows
                    .Where(x => x.ReturnRequestId == caseGroup.Key
                        && x.ReturnItemId == item.ReturnItemId
                        && x.AcceptedUsableQuantity is not null
                        && x.AcceptedDamagedQuantity is not null
                        && x.RejectedReceivedUsableQuantity is not null
                        && x.RejectedReceivedDamagedQuantity is not null)
                    .OrderByDescending(x => x.Version)
                    .FirstOrDefault();
                var plan = planRow is null
                    ? null
                    : new ReturnHeldGoodsPlanPortion(
                        planRow.AcceptedUsableQuantity!.Value,
                        planRow.AcceptedDamagedQuantity!.Value,
                        planRow.RejectedReceivedUsableQuantity!.Value,
                        planRow.RejectedReceivedDamagedQuantity!.Value);

                var description = ReturnHeldGoodsPolicy.Describe(new ReturnHeldGoodsLineEvidence(
                    item.ReturnItemId,
                    item.Quantity,
                    received,
                    summary.IsCoherent,
                    plan,
                    handedBack));
                if (description.OutstandingHeld <= 0m)
                {
                    continue;
                }

                held[item.OrderLineId] =
                    (held.TryGetValue(item.OrderLineId, out var existing) ? existing : 0m)
                    + description.OutstandingHeld;
            }
        }

        return held;
    }

    private sealed record OccupiedItemRow(Guid ReturnRequestId, Guid OrderLineId, decimal Quantity);

    private sealed record LineOccupancy(
        IReadOnlyDictionary<Guid, decimal> Returned,
        IReadOnlyDictionary<Guid, decimal> LegacyUnallocated,
        IReadOnlyDictionary<string, decimal> AllocatedElsewhere)
    {
        public static LineOccupancy Empty { get; } = new(
            new Dictionary<Guid, decimal>(),
            new Dictionary<Guid, decimal>(),
            new Dictionary<string, decimal>(StringComparer.Ordinal));
    }

    private static ReturnEligibilityResult Empty(
        OrderReturnContextSnapshot orderContext,
        string reasonCode,
        DateTimeOffset? eligibleUntil,
        DateTimeOffset? lastDeliveredAt) =>
        new(
            orderContext.SellerOrderId,
            orderContext.CheckoutId,
            false,
            reasonCode,
            eligibleUntil,
            lastDeliveredAt,
            []);

    private static DateTimeOffset ComputeEligibleUntil(
        OrderReturnContextSnapshot orderContext,
        FulfillmentReturnEligibilitySnapshot fulfillment,
        DateTimeOffset lastDeliveredAt)
    {
        var deadlines = new List<DateTimeOffset>();
        foreach (var line in orderContext.Lines.Where(x => x.IsReturnableSnapshot))
        {
            var windowDays = line.ReturnWindowDaysSnapshot > 0
                ? line.ReturnWindowDaysSnapshot
                : (int)ReturnWindow.TotalDays;
            var slices = SlicesForLine(fulfillment, line.OrderLineId, lastDeliveredAt);
            if (slices.Count == 0)
            {
                continue;
            }

            foreach (var slice in slices)
            {
                deadlines.Add(slice.DeliveredAt.AddDays(windowDays));
            }
        }

        if (deadlines.Count == 0)
        {
            return lastDeliveredAt + ReturnWindow;
        }

        return deadlines.Max();
    }

    private static IReadOnlyList<LineDeliverySlice> SlicesForLine(
        FulfillmentReturnEligibilitySnapshot fulfillment,
        Guid orderLineId,
        DateTimeOffset fallbackDeliveredAt)
    {
        if (fulfillment.DeliverySlices is { Count: > 0 } slices)
        {
            return slices.Where(x => x.OrderLineId == orderLineId && x.Quantity > 0).ToArray();
        }

        fulfillment.DeliveredQuantities.TryGetValue(orderLineId, out var delivered);
        if (delivered <= 0)
        {
            return [];
        }

        var at = fallbackDeliveredAt;
        if (fulfillment.LineDeliveredAt is not null
            && fulfillment.LineDeliveredAt.TryGetValue(orderLineId, out var specific))
        {
            at = specific;
        }

        return [new LineDeliverySlice(orderLineId, delivered, at)];
    }

    private static IReadOnlyList<ReturnLineEligibility> BuildLines(
        OrderReturnContextSnapshot orderContext,
        FulfillmentReturnEligibilitySnapshot fulfillment,
        LineOccupancy occupancy,
        DateTimeOffset lastDeliveredAt,
        DateTimeOffset now)
    {
        return orderContext.Lines.Select(line =>
        {
            fulfillment.DeliveredQuantities.TryGetValue(line.OrderLineId, out var delivered);
            occupancy.Returned.TryGetValue(line.OrderLineId, out var returned);
            if (!line.IsReturnableSnapshot)
            {
                return new ReturnLineEligibility(
                    line.OrderLineId,
                    delivered,
                    returned,
                    0,
                    null,
                    ReturnEligibilityReasonCodes.NonReturnable);
            }

            if (delivered <= 0)
            {
                // تعداد تحویل‌نشده ساعت مرجوعی ندارد.
                return new ReturnLineEligibility(
                    line.OrderLineId,
                    delivered,
                    returned,
                    0,
                    null,
                    ReturnEligibilityReasonCodes.NotDelivered);
            }

            var windowDays = line.ReturnWindowDaysSnapshot > 0
                ? line.ReturnWindowDaysSnapshot
                : (int)ReturnWindow.TotalDays;
            var slices = SlicesForLine(fulfillment, line.OrderLineId, lastDeliveredAt);
            var returnableUntil = slices.Count == 0
                ? lastDeliveredAt.AddDays(windowDays)
                : slices.Max(slice => slice.DeliveredAt.AddDays(windowDays));

            // TB-279 (D): the ONE per-delivery availability. A returned quantity bound to an EXPIRED slice never eats
            // into a newer delivered slice, because only the still-open slices contribute remaining capacity.
            occupancy.LegacyUnallocated.TryGetValue(line.OrderLineId, out var legacyUnallocated);
            var availability = ReturnAllocationPolicy.Describe(
                line.OrderLineId,
                slices.Select(x => new ReturnDeliverySliceSource(x.OrderLineId, x.Quantity, x.DeliveredAt)).ToArray(),
                windowDays,
                now,
                occupancy.AllocatedElsewhere,
                legacyUnallocated);
            var remaining = Math.Max(0m, availability.Where(x => x.IsInWindow).Sum(x => x.AvailableQuantity));
            if (now > returnableUntil)
            {
                // TB-278: an expired line is honestly "expired", not a generic exhausted quantity.
                return new ReturnLineEligibility(
                    line.OrderLineId,
                    delivered,
                    returned,
                    0,
                    returnableUntil,
                    ReturnEligibilityReasonCodes.WindowExpired,
                    availability);
            }

            if (remaining <= 0)
            {
                return new ReturnLineEligibility(
                    line.OrderLineId,
                    delivered,
                    returned,
                    remaining,
                    returnableUntil,
                    ReturnEligibilityReasonCodes.NothingReturnable,
                    availability);
            }

            return new ReturnLineEligibility(
                line.OrderLineId,
                delivered,
                returned,
                remaining,
                returnableUntil,
                null,
                availability);
        }).ToArray();
    }
}
