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
/// belongs to the return disposition (damaged units are never made saleable here).
/// </para>
/// <para>
/// TB-265-R1: the three writes (position <c>Reserved</c> decrease, reservation quantity/status change and the
/// idempotency inbox row) are ONE atomic unit. The reservation row is locked first, so two independent contexts
/// cannot both read the same Held quantity and release it twice; the inbox primary key is the durable replay answer,
/// and a lost commit is answered from that row instead of releasing again.
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

        // TB-265-R1: serialize on the reservation row before reading its Held quantity. Without this lock two
        // concurrent approvals would both see the same remaining quantity and release it twice.
        var ownsTransaction = _db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        if (ownsTransaction)
        {
            await _db.Database.ExecuteSqlAsync(
                $"SELECT 1 FROM inventory.reservations WHERE reservation_id = {reservationId} FOR UPDATE",
                cancellationToken);
        }

        // Re-read inside the lock: a racing context may have just released or consumed it.
        var replayInsideLock = await _db.ReturnRestockInbox.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == key, cancellationToken);
        if (replayInsideLock is not null)
        {
            await CommitAsync(transaction, ownsTransaction, cancellationToken);
            return new InventoryHeldReleaseResult(reservationId, quantity, replayInsideLock.Quantity, "Replayed");
        }

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

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            await CommitAsync(transaction, ownsTransaction, cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A concurrent attempt with the SAME key won the inbox primary key: the effect is one release, and the
            // recorded row is the answer. Nothing from this context was committed.
            if (ownsTransaction && transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            _db.ChangeTracker.Clear();
            var winner = await _db.ReturnRestockInbox.AsNoTracking()
                .SingleOrDefaultAsync(x => x.IdempotencyKey == key, cancellationToken);
            if (winner is not null)
            {
                return new InventoryHeldReleaseResult(reservationId, quantity, winner.Quantity, "Replayed");
            }

            throw;
        }

        return new InventoryHeldReleaseResult(
            reservationId,
            quantity,
            released,
            reservation.Status == StockReservationStatus.Held ? "PartiallyHeld" : "Released");
    }

    private static async Task CommitAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction,
        bool ownsTransaction,
        CancellationToken cancellationToken)
    {
        if (ownsTransaction && transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
    }
}
