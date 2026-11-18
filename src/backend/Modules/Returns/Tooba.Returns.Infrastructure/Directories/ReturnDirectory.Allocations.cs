using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.Aggregates;
using Tooba.Returns.Infrastructure.Persistence;

namespace Tooba.Returns.Infrastructure.Directories;

/// <summary>
/// TB-279 (D): the ONE Returns-owned per-delivery allocation authority.
/// <para>
/// A new claim is bound to the exact owning DELIVERED slices it may consume, so capacity is a durable per-slice fact
/// instead of "everything delivered, minus every returned quantity". That subtraction was the defect: a return bound to
/// an EXPIRED slice used to consume a newly delivered slice of the same SKU, and two sibling cases could each believe
/// the same units were theirs.
/// </para>
/// <para>
/// Rows are written only under the existing per-seller-order serialization; a rejection/cancellation RELEASES them (the
/// row stays as history with <see cref="ReturnDeliveryAllocation.ReleasedAt"/>), and reopening a rejection re-acquires
/// the same capacity under the same lock — refusing with a typed conflict when another case already took it.
/// </para>
/// </summary>
public sealed partial class ReturnDirectory
{
    /// <summary>
    /// Binds every line of a NEWLY created ordinary case to its owning slices, using the SAME eligibility read the
    /// creation decision used. A line whose eligible slices cannot cover the requested quantity is a typed refusal.
    /// </summary>
    private IReadOnlyList<ReturnDeliveryAllocation> BuildDeliveryAllocations(
        ReturnRequest request,
        ReturnEligibilityResult eligibility)
    {
        var allocations = new List<ReturnDeliveryAllocation>(request.Items.Count);
        foreach (var item in request.Items)
        {
            var line = eligibility.Lines.FirstOrDefault(x => x.OrderLineId == item.OrderLineId);
            if (line?.Slices is not { Count: > 0 })
            {
                // No owning delivered slice answered: the claim cannot be bound to a real window, so it is refused
                // instead of borrowing a sibling parcel's date or a browser "today".
                throw new ContractOperationException(ReturnsErrorCodes.AllocationRequired);
            }

            var bound = ReturnAllocationPolicy.Allocate(line.Slices, item.Quantity);
            if (bound is null)
            {
                // TB-279-R1 (F5): when the leftover capacity is unknowable because an earlier claim predates per-delivery
                // allocation, the honest answer is a typed refusal — never a newest/oldest slice pick.
                throw new ContractOperationException(
                    ReturnAllocationPolicy.HasAmbiguousLegacy(line.Slices)
                        ? ReturnsErrorCodes.AllocationLegacyAmbiguous
                        : ReturnsErrorCodes.AllocationExceeded);
            }
            foreach (var (sliceKey, deliveredAt, quantity) in bound)
            {
                allocations.Add(new ReturnDeliveryAllocation
                {
                    ReturnAllocationId = _ids.NewId(),
                    ReturnRequestId = request.ReturnRequestId,
                    ReturnItemId = item.ReturnItemId,
                    OrderLineId = item.OrderLineId,
                    SliceKey = sliceKey,
                    Quantity = quantity,
                    DeliveredAt = deliveredAt,
                    ReleasedAt = null,
                    RecordedAt = _clock.UtcNow,
                });
            }
        }

        return allocations;
    }

    /// <summary>
    /// TB-279-R1 (F3): releases the capacity of a REJECTED/CANCELLED case down to the goods it physically holds.
    /// <para>
    /// The claimed quantity was the whole ORIGINAL claim, but the customer keeps the never-received remainder, so that
    /// remainder returns to the pool at once. Everything that actually arrived stays under the seller-order
    /// serialization until its real handback is recorded: a sibling case can therefore never claim goods the seller
    /// still holds for another customer. A case with no recorded receipt releases its entire claim (its previous
    /// behaviour); a legacy case without allocation rows re-acquires exactly the held quantity from its own eligible
    /// slices instead of guessing an older delivery.
    /// </para>
    /// </summary>
    private async Task ReleaseRejectedDeliveriesAsync(ReturnRequest request, CancellationToken cancellationToken)
    {
        var receiving = await ReadReceivingAsync(request, cancellationToken);
        foreach (var item in request.Items)
        {
            var held = receiving is { IsCoherent: true }
                ? receiving.UsableOf(item.ReturnItemId) + receiving.DamagedOf(item.ReturnItemId)
                : 0m;
            await TrimActiveAllocationsToHeldAsync(
                request.ReturnRequestId,
                item,
                held,
                allocateWhenMissing: true,
                cancellationToken);
        }
    }

    /// <summary>
    /// TB-279-R1 (F3): trims one line's ACTIVE allocations to the exact quantity still physically held. Rows are
    /// released newest-delivery-first, so an older window keeps its claim; a straddling row is split and only the
    /// returned portion is released. Zero held releases the whole line.
    /// </summary>
    private async Task TrimActiveAllocationsToHeldAsync(
        Guid returnRequestId,
        ReturnItem item,
        decimal held,
        bool allocateWhenMissing,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var rows = await _db.ReturnDeliveryAllocations
            .Where(x => x.ReturnRequestId == returnRequestId
                && x.ReturnItemId == item.ReturnItemId
                && x.ReleasedAt == null)
            .OrderByDescending(x => x.DeliveredAt)
            .ThenBy(x => x.SliceKey)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            if (!allocateWhenMissing || held <= 0m)
            {
                return;
            }

            var eligibility = await _eligibility.EvaluateAsync(
                await OwningSellerOrderIdAsync(returnRequestId, cancellationToken),
                cancellationToken,
                excludeReturnRequestId: returnRequestId);
            var line = eligibility.Lines.FirstOrDefault(x => x.OrderLineId == item.OrderLineId);
            var bound = line?.Slices is { Count: > 0 } slices
                ? ReturnAllocationPolicy.Allocate(slices, held)
                : null;
            if (bound is null)
            {
                // A legacy claim whose slice cannot be attributed is refused by the caller as a typed ambiguity; the
                // held goods simply keep occupying through their own case until a real handback is recorded.
                return;
            }

            foreach (var (sliceKey, deliveredAt, quantity) in bound)
            {
                _db.ReturnDeliveryAllocations.Add(new ReturnDeliveryAllocation
                {
                    ReturnAllocationId = _ids.NewId(),
                    ReturnRequestId = returnRequestId,
                    ReturnItemId = item.ReturnItemId,
                    OrderLineId = item.OrderLineId,
                    SliceKey = sliceKey,
                    Quantity = quantity,
                    DeliveredAt = deliveredAt,
                    ReleasedAt = null,
                    RecordedAt = now,
                });
            }

            return;
        }

        var excess = rows.Sum(x => x.Quantity) - Math.Max(0m, held);
        if (excess <= 0m)
        {
            return;
        }

        foreach (var row in rows)
        {
            if (excess <= 0m)
            {
                break;
            }

            if (row.Quantity <= excess)
            {
                excess -= row.Quantity;
                row.ReleasedAt = now;
                continue;
            }

            var released = excess;
            excess = 0m;
            row.ReduceQuantity(released);
            _db.ReturnDeliveryAllocations.Add(new ReturnDeliveryAllocation
            {
                ReturnAllocationId = _ids.NewId(),
                ReturnRequestId = row.ReturnRequestId,
                ReturnItemId = row.ReturnItemId,
                OrderLineId = row.OrderLineId,
                SliceKey = row.SliceKey,
                Quantity = released,
                DeliveredAt = row.DeliveredAt,
                ReleasedAt = now,
                RecordedAt = now,
            });
        }
    }

    private Task<Guid> OwningSellerOrderIdAsync(Guid returnRequestId, CancellationToken cancellationToken) =>
        _db.ReturnRequests.AsNoTracking()
            .Where(x => x.ReturnRequestId == returnRequestId)
            .Select(x => x.SellerOrderId)
            .SingleAsync(cancellationToken);

    /// <summary>
    /// Releases (rejected/cancelled) or re-acquires (correction back to review) the delivery capacity of ONE case.
    /// <para>
    /// A case created before per-delivery allocation has no rows: re-acquiring allocates from the CURRENT eligibility
    /// read (which already discounts every other active case), so no ambiguous legacy row is rewritten and no new
    /// window is invented.
    /// </para>
    /// </summary>
    private async Task EnsureDeliveryAllocationsAsync(
        ReturnRequest request,
        bool release,
        CancellationToken cancellationToken)
    {
        var rows = await _db.ReturnDeliveryAllocations
            .Where(x => x.ReturnRequestId == request.ReturnRequestId)
            .ToListAsync(cancellationToken);
        var now = _clock.UtcNow;

        if (release)
        {
            foreach (var row in rows.Where(x => x.ReleasedAt is null))
            {
                row.ReleasedAt = now;
            }

            return;
        }

        if (rows.Count > 0)
        {
            if (rows.Any(x => x.ReleasedAt is not null))
            {
                // The exact capacity this case owned is re-acquired under the caller's seller-order serialization.
                foreach (var row in rows)
                {
                    row.ReleasedAt = null;
                }
            }

            return;
        }

        var eligibility = await _eligibility.EvaluateAsync(
            request.SellerOrderId,
            cancellationToken,
            excludeReturnRequestId: request.ReturnRequestId);
        var items = await _db.ReturnItems
            .Where(x => x.ReturnRequestId == request.ReturnRequestId)
            .ToListAsync(cancellationToken);
        foreach (var item in items)
        {
            var line = eligibility.Lines.FirstOrDefault(x => x.OrderLineId == item.OrderLineId);
            var bound = line?.Slices is { Count: > 0 }
                ? ReturnAllocationPolicy.Allocate(line.Slices, item.Quantity)
                : null;
            if (bound is null)
            {
                // TB-279-R1 (F5): an unattributable legacy occupancy is its own refusal, never a guessed slice.
                throw new ContractOperationException(
                    line?.Slices is { Count: > 0 } slices && ReturnAllocationPolicy.HasAmbiguousLegacy(slices)
                        ? ReturnsErrorCodes.AllocationLegacyAmbiguous
                        : ReturnsErrorCodes.CorrectionCapacityConflict);
            }

            foreach (var (sliceKey, deliveredAt, quantity) in bound)
            {
                _db.ReturnDeliveryAllocations.Add(new ReturnDeliveryAllocation
                {
                    ReturnAllocationId = _ids.NewId(),
                    ReturnRequestId = request.ReturnRequestId,
                    ReturnItemId = item.ReturnItemId,
                    OrderLineId = item.OrderLineId,
                    SliceKey = sliceKey,
                    Quantity = quantity,
                    DeliveredAt = deliveredAt,
                    ReleasedAt = null,
                    RecordedAt = now,
                });
            }
        }
    }
}
