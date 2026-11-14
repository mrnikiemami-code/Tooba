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
/// TB-265-R1: the three writes (position <c>Reserved</c> decrease, reservation quantity/status change and the durable
/// answer row) are ONE atomic unit. The reservation row is locked first, so two independent contexts cannot both read
/// the same Held quantity and release it twice.
/// </para>
/// <para>
/// TB-265-R2: the durable row now records the IMMUTABLE REQUEST (purpose, key, reservation, requested quantity) next to
/// the actual effect. Every replay branch — first lookup, inside-lock lookup and the concurrency winner — compares that
/// request before answering, so a changed reservation or a changed requested quantity is a typed conflict with zero
/// writes instead of being silently answered with another request's released amount. The answer is also truthful: a
/// Consumed no-op reports <c>Consumed</c>, never <c>Released</c>.
/// </para>
/// </summary>
public sealed class InventoryReturnObligationRelease : IInventoryReturnObligationRelease
{
    /// <summary>Owning purpose of this release. It is deliberately distinct from the return-restock inbox purpose.</summary>
    public const string ReleasePurpose = "order-line-obligation-hold-release";

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

        // Durable replay: the recorded row answers ITS OWN request exactly (requested identity, not just the key).
        var recorded = await _db.HeldObligationReleases.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == key, cancellationToken);
        if (recorded is not null)
        {
            return AnswerRecorded(recorded, reservationId, quantity);
        }

        await _guard.EnsureCanMutateAsync(cancellationToken);

        // Serialize on the reservation row before reading its Held quantity. Without this lock two concurrent
        // approvals would both see the same remaining quantity and release it twice. The row lock is taken in the
        // adapter's OWN transaction and, just as well, inside a caller-owned transaction (where it simply lives until
        // that caller commits), so serialization never depends on who opened the transaction.
        var ownsTransaction = _db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        await _db.Database.ExecuteSqlAsync(
            $"SELECT 1 FROM inventory.reservations WHERE reservation_id = {reservationId} FOR UPDATE",
            cancellationToken);

        // Re-read inside the lock: a racing context may have just answered this same request.
        var replayInsideLock = await _db.HeldObligationReleases.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == key, cancellationToken);
        if (replayInsideLock is not null)
        {
            var replayed = AnswerRecorded(replayInsideLock, reservationId, quantity);
            await CommitAsync(transaction, ownsTransaction, cancellationToken);
            return replayed;
        }

        var reservation = await _db.Reservations.SingleOrDefaultAsync(x => x.ReservationId == reservationId, cancellationToken);
        if (reservation is null)
        {
            throw new ContractOperationException(InventoryErrorCodes.ReservationNotFound);
        }

        var now = _clock.UtcNow;
        decimal released;
        string outcome;
        if (reservation.Status == StockReservationStatus.Consumed)
        {
            // A consumed original sale can never be released or resurrected. This is a truthful no-op, not "Released".
            released = 0m;
            outcome = ReleaseOutcome.Consumed;
        }
        else if (reservation.Status == StockReservationStatus.Released)
        {
            released = 0m;
            outcome = ReleaseOutcome.AlreadyReleased;
        }
        else
        {
            released = reservation.ReleasePartially(quantity, now);
            outcome = reservation.Status == StockReservationStatus.Held
                ? ReleaseOutcome.PartiallyHeld
                : ReleaseOutcome.Released;
        }

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

        _db.HeldObligationReleases.Add(new HeldObligationReleaseRecord
        {
            IdempotencyKey = key,
            Purpose = ReleasePurpose,
            ReservationId = reservationId,
            RequestedQuantity = quantity,
            ReleasedQuantity = released,
            Outcome = outcome,
            ProcessedAt = now,
        });

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            await CommitAsync(transaction, ownsTransaction, cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A concurrent attempt with the SAME key won the answer primary key: the effect is one release, and the
            // recorded row is the answer. Nothing from this context was committed.
            if (ownsTransaction && transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            _db.ChangeTracker.Clear();
            var winner = await _db.HeldObligationReleases.AsNoTracking()
                .SingleOrDefaultAsync(x => x.IdempotencyKey == key, cancellationToken);
            if (winner is not null)
            {
                return AnswerRecorded(winner, reservationId, quantity);
            }

            throw;
        }

        return new InventoryHeldReleaseResult(
            reservationId,
            quantity,
            released,
            outcome,
            outcome);
    }

    /// <summary>
    /// Answers a recorded durable request. A key that now carries a DIFFERENT purpose, reservation or requested amount
    /// is a changed intent: it must be refused before any write rather than answered with someone else's effect.
    /// </summary>
    private static InventoryHeldReleaseResult AnswerRecorded(
        HeldObligationReleaseRecord recorded,
        Guid reservationId,
        decimal requestedQuantity)
    {
        if (recorded.ReservationId != reservationId
            || recorded.RequestedQuantity != requestedQuantity
            || !string.Equals(recorded.Purpose, ReleasePurpose, StringComparison.Ordinal))
        {
            throw new ContractOperationException(InventoryErrorCodes.HeldReleaseIntentConflict);
        }

        return new InventoryHeldReleaseResult(
            recorded.ReservationId,
            recorded.RequestedQuantity,
            recorded.ReleasedQuantity,
            "Replayed",
            recorded.Outcome);
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

    /// <summary>Durable outcome values of one release request.</summary>
    private static class ReleaseOutcome
    {
        internal const string Released = "Released";
        internal const string PartiallyHeld = "PartiallyHeld";
        internal const string Consumed = "Consumed";
        internal const string AlreadyReleased = "AlreadyReleased";
    }
}

