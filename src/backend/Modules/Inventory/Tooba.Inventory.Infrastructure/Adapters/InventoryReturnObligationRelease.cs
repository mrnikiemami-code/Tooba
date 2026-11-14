using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Inventory.Application.Ports;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Contracts.Returns;
using Tooba.Inventory.Domain.ValueObjects;
using Tooba.Inventory.Infrastructure.Persistence;

namespace Tooba.Inventory.Infrastructure.Adapters;

/// <summary>
/// TB-265: quantity-safe release of an unconsumed order hold when an approved courier-origin return closes the
/// corresponding order-line delivery obligation.
/// <para>
/// Consume is not release: a Consumed reservation returns 0. A Released reservation returns 0. Only the still-Held
/// quantity can be released, and only that quantity — a sibling line's hold is a different reservation and is never
/// touched. Release restores AVAILABILITY by lowering <c>Reserved</c> exactly like the owning
/// <c>InventoryDirectory.ReleaseAsync</c>; it never raises <c>OnHand</c>, because the physical unit's saleable fate
/// belongs to the return disposition (damaged units are never made saleable here). The release is recorded in the
/// existing idempotent inbox so a retried approval cannot release twice.
/// </para>
/// </summary>
public sealed class InventoryReturnObligationRelease : IInventoryReturnObligationRelease
{
    private readonly InventoryDbContext _db;
    private readonly IInventoryUseCaseGuard _guard;
    private readonly IClock _clock;

    /// <summary>Wires the seam to the owning Inventory schema.</summary>
    public InventoryReturnObligationRelease(
        InventoryDbContext db,
        IInventoryUseCaseGuard guard,
        IClock clock)
    {
        _db = db;
        _guard = guard;
        ArgumentNullException.ThrowIfNull(clock);
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<InventoryHeldReleaseResult> ReleaseUnconsumedHeldAsync(
        Guid reservationId,
        decimal quantity,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (reservationId == Guid.Empty || quantity <= 0m || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ContractOperationException(InventoryErrorCodes.ReturnRestockInvalid);
        }

        var key = idempotencyKey.Trim();

        // Durable idempotency: the recorded row remembers exactly how much was released for this key.
        var recorded = await _db.ReturnRestockInbox.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == key, cancellationToken);
        if (recorded is not null)
        {
            return new InventoryHeldReleaseResult(reservationId, quantity, recorded.Quantity, "Replayed");
        }

        await _guard.EnsureCanMutateAsync(cancellationToken);

        var reservation = await _db.Reservations.SingleOrDefaultAsync(x => x.ReservationId == reservationId, cancellationToken);
        if (reservation is null)
        {
            throw new ContractOperationException(InventoryErrorCodes.ReservationNotFound);
        }

        var now = _clock.UtcNow;
        var released = reservation.ReleasePartially(quantity, now);
        if (released > 0m)
        {
            // Same invariant as the owning release: only Reserved moves, so availability grows by exactly the
            // released quantity and OnHand stays the physical stock the warehouse already holds.
            var updated = await _db.Positions
                .Where(x => x.StockItemId == reservation.StockItemId && x.Reserved >= released)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.Reserved, x => x.Reserved - released)
                        .SetProperty(x => x.UpdatedAt, now),
                    cancellationToken);
            if (updated != 1)
            {
                throw new ContractOperationException(InventoryErrorCodes.ReservationReleaseMismatch);
            }

            var position = await _db.Positions.SingleAsync(
                x => x.StockItemId == reservation.StockItemId, cancellationToken);
            position.RecordReleased(reservation.ReservationId, released);
            position.SyncQuantities(position.OnHand, position.Reserved, now);
        }

        _db.ReturnRestockInbox.Add(new ReturnRestockInboxRecord
        {
            IdempotencyKey = key,
            ReservationId = reservationId,
            Quantity = released,
            ProcessedAt = now,
        });
        await _db.SaveChangesAsync(cancellationToken);

        return new InventoryHeldReleaseResult(
            reservationId,
            quantity,
            released,
            reservation.Status == StockReservationStatus.Held ? "PartiallyHeld" : "Released");
    }
}
