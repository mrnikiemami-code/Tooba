using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Inventory.Application.Orders;
using Tooba.Inventory.Application.Ports;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Domain.ValueObjects;

namespace Tooba.Inventory.Infrastructure.Directories;

/// <summary>
/// Order-supply engine for <see cref="InventoryDirectory"/>: the canonical reuse-or-reacquire
/// decision matrix (<c>CheckOnly</c>/<c>EnsureReviewHold</c>/<c>EnsurePaidDurable</c>/
/// <c>EnsureFulfillmentSupply</c>/<c>EnsureUnpaidRetryHold</c>), per-line evaluation and the
/// rollback of partially acquired reservations. Extracted into a cohesive partial of the same class
/// so the persistence seam keeps a single reason to change and behavior is byte-for-byte unchanged.
/// </summary>
public sealed partial class InventoryDirectory
{
    /// <inheritdoc />
    public async Task<EnsureOrderSupplyResult> EnsureOrderSupplyAsync(
        EnsureOrderSupplyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Lines is null || request.Lines.Count == 0)
        {
            return new EnsureOrderSupplyResult(
                OrderSupplyOutcome.NotApplicable,
                OrderSupplyStatusKind.NotApplicable,
                [],
                new Dictionary<Guid, Guid>());
        }

        if (request.Lines.All(x => x.RemainingQuantity <= 0))
        {
            return new EnsureOrderSupplyResult(
                OrderSupplyOutcome.AlreadyReserved,
                OrderSupplyStatusKind.Fulfilled,
                [],
                new Dictionary<Guid, Guid>());
        }

        var requireDurable = request.Mode is OrderSupplyMode.EnsurePaidDurable or OrderSupplyMode.EnsureFulfillmentSupply;
        var evaluation = await EvaluateLinesAsync(request.Lines, requireDurable, cancellationToken);

        if (request.Mode == OrderSupplyMode.CheckOnly)
        {
            var checkOutcome = evaluation.Status switch
            {
                OrderSupplyStatusKind.Reserved or OrderSupplyStatusKind.Fulfilled => OrderSupplyOutcome.AlreadyReserved,
                OrderSupplyStatusKind.Unavailable => OrderSupplyOutcome.Unavailable,
                OrderSupplyStatusKind.PartiallyUnavailable => OrderSupplyOutcome.PartiallyUnavailable,
                OrderSupplyStatusKind.AvailableForReacquire => OrderSupplyOutcome.AlreadyReserved,
                _ => OrderSupplyOutcome.NotApplicable,
            };
            return new EnsureOrderSupplyResult(
                checkOutcome,
                evaluation.Status,
                evaluation.Lines,
                new Dictionary<Guid, Guid>());
        }

        if (evaluation.Status is OrderSupplyStatusKind.Reserved or OrderSupplyStatusKind.Fulfilled)
        {
            // Promote/commit in place when needed without new reservation.
            if (IsTimedHold(request.Mode) && request.ReviewExpiresAt is { } reviewAt)
            {
                foreach (var line in evaluation.Lines.Where(x => x.BoundReservationId is not null && x.LineStatus == OrderSupplyStatusKind.Reserved))
                {
                    await PromoteReservationForManualPaymentReviewAsync(line.BoundReservationId!.Value, reviewAt, cancellationToken);
                }
            }

            if (requireDurable)
            {
                foreach (var line in evaluation.Lines.Where(x => x.BoundReservationId is not null && x.LineStatus == OrderSupplyStatusKind.Reserved))
                {
                    var boundId = line.BoundReservationId!.Value;
                    var bound = await FindReservationAsync(boundId, cancellationToken);
                    if (bound is { Status: StockReservationStatus.Held })
                    {
                        await CommitReservationForPaidOrderAsync(boundId, cancellationToken);
                    }
                }
            }

            evaluation = await EvaluateLinesAsync(request.Lines, requireDurable, cancellationToken);
            if (evaluation.Status is OrderSupplyStatusKind.Reserved or OrderSupplyStatusKind.Fulfilled)
            {
                return new EnsureOrderSupplyResult(
                    OrderSupplyOutcome.AlreadyReserved,
                    evaluation.Status,
                    evaluation.Lines,
                    new Dictionary<Guid, Guid>());
            }
        }

        if (!request.AllowReacquire
            || evaluation.Status is OrderSupplyStatusKind.Unavailable or OrderSupplyStatusKind.PartiallyUnavailable
            || evaluation.Status is OrderSupplyStatusKind.NotApplicable)
        {
            return new EnsureOrderSupplyResult(
                MapOutcome(evaluation.Status, mutated: false),
                evaluation.Status,
                evaluation.Lines,
                new Dictionary<Guid, Guid>());
        }

        // AvailableForReacquire (or mixed reserved+available): reacquire ALL remaining lines that are not already secured.
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var acquired = new List<Guid>();
        var bindings = new Dictionary<Guid, Guid>();
        try
        {
            foreach (var input in request.Lines.Where(x => x.RemainingQuantity > 0))
            {
                var existingEval = evaluation.Lines.FirstOrDefault(x => x.OrderLineId == input.OrderLineId);
                if (existingEval is { LineStatus: OrderSupplyStatusKind.Reserved, BoundReservationId: not null })
                {
                    if (requireDurable)
                    {
                        await CommitReservationForPaidOrderAsync(existingEval.BoundReservationId.Value, cancellationToken);
                    }
                    else if (IsTimedHold(request.Mode) && request.ReviewExpiresAt is { } reviewAt)
                    {
                        await PromoteReservationForManualPaymentReviewAsync(
                            existingEval.BoundReservationId.Value,
                            reviewAt,
                            cancellationToken);
                    }

                    bindings[input.OrderLineId] = existingEval.BoundReservationId.Value;
                    continue;
                }

                var stockItemId = await ResolveStockItemIdAsync(input, cancellationToken)
                    ?? throw new ContractOperationException(InventoryErrorCodes.ManualReviewUnavailable);
                DateTimeOffset? expiresAt = IsTimedHold(request.Mode)
                    ? request.ReviewExpiresAt
                    : null;
                // idempotency_key column is varchar(128); keep this compact.
                var idempotencyKey =
                    $"os-{(int)request.Mode}-{input.OrderLineId:N}-{request.CheckoutId:N}-{_clock.UtcNow.UtcTicks}";
                var receipt = await ReserveAsync(
                    stockItemId,
                    input.RemainingQuantity,
                    $"order-supply-{input.OrderLineId:N}",
                    idempotencyKey,
                    expiresAt,
                    cancellationToken);
                acquired.Add(receipt.ReservationId);
                if (requireDurable)
                {
                    await CommitReservationForPaidOrderAsync(receipt.ReservationId, cancellationToken);
                }

                bindings[input.OrderLineId] = receipt.ReservationId;
            }

            var refreshed = await EvaluateLinesAsync(
                request.Lines.Select(l => bindings.TryGetValue(l.OrderLineId, out var rid)
                    ? l with { CurrentReservationId = rid }
                    : l).ToArray(),
                requireDurable,
                cancellationToken);
            return new EnsureOrderSupplyResult(
                OrderSupplyOutcome.Reacquired,
                refreshed.Status,
                refreshed.Lines,
                bindings);
        }
        catch (InvalidOperationException)
        {
            foreach (var id in acquired)
            {
                await ReleaseAsync(id, cancellationToken);
            }

            var failed = await EvaluateLinesAsync(request.Lines, requireDurable, cancellationToken);
            return new EnsureOrderSupplyResult(
                OrderSupplyOutcome.Unavailable,
                OrderSupplyStatusKind.Unavailable,
                failed.Lines,
                new Dictionary<Guid, Guid>());
        }
    }

    private async Task<(OrderSupplyStatusKind Status, IReadOnlyList<OrderSupplyLineShortage> Lines)> EvaluateLinesAsync(
        IReadOnlyList<OrderSupplyLineInput> lines,
        bool requireDurable,
        CancellationToken cancellationToken)
    {
        var results = new List<OrderSupplyLineShortage>();
        foreach (var line in lines)
        {
            if (line.RemainingQuantity <= 0)
            {
                results.Add(new OrderSupplyLineShortage(
                    line.OrderLineId,
                    line.ItemTitle,
                    line.UnitCode,
                    0,
                    0,
                    0,
                    OrderSupplyStatusKind.Fulfilled,
                    line.CurrentReservationId));
                continue;
            }

            ReservationReceipt? existing = null;
            if (line.CurrentReservationId is { } reservationId)
            {
                existing = await FindReservationAsync(reservationId, cancellationToken);
            }

            var heldValid = existing is { Status: StockReservationStatus.Held }
                && existing.Quantity >= line.RemainingQuantity
                && (existing.ExpiresAt is null || existing.ExpiresAt > _clock.UtcNow);

            if (heldValid)
            {
                results.Add(new OrderSupplyLineShortage(
                    line.OrderLineId,
                    line.ItemTitle,
                    line.UnitCode,
                    line.RemainingQuantity,
                    line.RemainingQuantity,
                    0,
                    OrderSupplyStatusKind.Reserved,
                    existing!.ReservationId));
                continue;
            }

            var available = await ResolveAvailableAsync(line, cancellationToken);
            if (available >= line.RemainingQuantity)
            {
                results.Add(new OrderSupplyLineShortage(
                    line.OrderLineId,
                    line.ItemTitle,
                    line.UnitCode,
                    line.RemainingQuantity,
                    available,
                    0,
                    OrderSupplyStatusKind.AvailableForReacquire,
                    null));
            }
            else
            {
                results.Add(new OrderSupplyLineShortage(
                    line.OrderLineId,
                    line.ItemTitle,
                    line.UnitCode,
                    line.RemainingQuantity,
                    available,
                    line.RemainingQuantity - available,
                    OrderSupplyStatusKind.Unavailable,
                    null));
            }
        }

        var active = results.Where(x => x.LineStatus != OrderSupplyStatusKind.Fulfilled).ToList();
        if (active.Count == 0)
        {
            return (OrderSupplyStatusKind.Fulfilled, results);
        }

        if (active.All(x => x.LineStatus == OrderSupplyStatusKind.Reserved))
        {
            return (OrderSupplyStatusKind.Reserved, results);
        }

        if (active.All(x => x.LineStatus == OrderSupplyStatusKind.AvailableForReacquire))
        {
            return (OrderSupplyStatusKind.AvailableForReacquire, results);
        }

        if (active.All(x => x.LineStatus == OrderSupplyStatusKind.Unavailable))
        {
            return (OrderSupplyStatusKind.Unavailable, results);
        }

        return (OrderSupplyStatusKind.PartiallyUnavailable, results);
    }

    private async Task<decimal> ResolveAvailableAsync(OrderSupplyLineInput line, CancellationToken cancellationToken)
    {
        if (line.CurrentReservationId is { } rid)
        {
            var existing = await FindReservationAsync(rid, cancellationToken);
            if (existing is not null)
            {
                var position = await _db.Positions.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.StockItemId == existing.StockItemId, cancellationToken);
                if (position is not null)
                {
                    return Math.Max(0, position.OnHand - position.Reserved);
                }
            }
        }

        var availability = await GetAvailabilityAsync(line.OfferId, cancellationToken);
        return availability?.Available ?? 0m;
    }

    private async Task<Guid?> ResolveStockItemIdAsync(OrderSupplyLineInput line, CancellationToken cancellationToken)
    {
        if (line.CurrentReservationId is { } rid)
        {
            var existing = await FindReservationAsync(rid, cancellationToken);
            if (existing is not null)
            {
                return existing.StockItemId;
            }
        }

        var availability = await GetAvailabilityAsync(line.OfferId, cancellationToken);
        return availability?.Locations.OrderByDescending(x => x.Available).FirstOrDefault()?.StockItemId;
    }

    private static bool IsTimedHold(OrderSupplyMode mode) =>
        mode is OrderSupplyMode.EnsureReviewHold or OrderSupplyMode.EnsureUnpaidRetryHold;

    private static OrderSupplyOutcome MapOutcome(OrderSupplyStatusKind status, bool mutated) =>
        status switch
        {
            OrderSupplyStatusKind.Reserved or OrderSupplyStatusKind.Fulfilled =>
                mutated ? OrderSupplyOutcome.Reacquired : OrderSupplyOutcome.AlreadyReserved,
            OrderSupplyStatusKind.AvailableForReacquire => OrderSupplyOutcome.AlreadyReserved,
            OrderSupplyStatusKind.Unavailable => OrderSupplyOutcome.Unavailable,
            OrderSupplyStatusKind.PartiallyUnavailable => OrderSupplyOutcome.PartiallyUnavailable,
            _ => OrderSupplyOutcome.NotApplicable,
        };
}
