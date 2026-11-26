using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Domain.Aggregates;
using Tooba.Inventory.Domain.ValueObjects;
using Tooba.Inventory.Infrastructure.Persistence;

namespace Tooba.Inventory.Infrastructure.Directories;

/// <summary>Reservation release/consumption lifecycle split from the cohesive directory.</summary>
public sealed partial class InventoryDirectory
{
    /// <inheritdoc />
    public async Task ReleaseAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        await using var releaseTransaction = _db.Database.CurrentTransaction is null
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        await _db.Database.ExecuteSqlAsync(
            $"SELECT 1 FROM inventory.reservations WHERE reservation_id = {reservationId} FOR UPDATE",
            cancellationToken);
        var reservation = await _db.Reservations.SingleOrDefaultAsync(x => x.ReservationId == reservationId, cancellationToken)
            ?? throw new ContractOperationException(InventoryErrorCodes.ReservationNotFound);
        await _db.Entry(reservation).ReloadAsync(cancellationToken);
        if (reservation.Status is StockReservationStatus.Released or StockReservationStatus.Consumed)
            return;

        var knownPosition = await _db.Positions.AsNoTracking()
            .SingleAsync(x => x.StockItemId == reservation.StockItemId, cancellationToken);
        await LockOfferAllocationAsync(knownPosition.OfferId, cancellationToken);
        var allocation = await _db.OfferAllocations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OfferId == knownPosition.OfferId, cancellationToken);
        var now = _clock.UtcNow;
        if (allocation is not null && (reservation.Purpose is null or StockReservationPurpose.NewSale))
        {
            var releaseRevision = _ids.NewId();
            var allocationReleased = await _db.OfferAllocations
                .Where(x => x.OfferId == knownPosition.OfferId && x.Held >= reservation.Quantity)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.Held, x => x.Held - reservation.Quantity)
                        .SetProperty(x => x.Revision, releaseRevision)
                        .SetProperty(x => x.UpdatedAt, now),
                    cancellationToken);
            if (allocationReleased != 1)
                throw new ContractOperationException(InventoryErrorCodes.ReservationReleaseMismatch);
        }

        var released = await _db.Positions
            .Where(x => x.StockItemId == reservation.StockItemId && x.Reserved >= reservation.Quantity)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Reserved, x => x.Reserved - reservation.Quantity)
                    .SetProperty(x => x.UpdatedAt, now),
                cancellationToken);
        if (released != 1)
            throw new ContractOperationException(InventoryErrorCodes.ReservationReleaseMismatch);


        reservation.MoveTo(StockReservationStatus.Released, now);
        var position = await _db.Positions.SingleAsync(x => x.StockItemId == reservation.StockItemId, cancellationToken);
        position.RecordReleased(reservation.ReservationId, reservation.Quantity);
        position.SyncQuantities(position.OnHand, position.Reserved, now);
        await _db.SaveChangesAsync(cancellationToken);
        if (releaseTransaction is not null)
            await releaseTransaction.CommitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task ConsumeAsync(Guid reservationId, CancellationToken cancellationToken) =>
        ConsumeReservationAsync(reservationId, consumerKey: null, cancellationToken);

    /// <summary>
    /// مصرف رزرو با هویت پایدار مصرف‌کننده؛ replay اثبات‌شدهٔ همان مصرف‌کننده idempotent است و
    /// مصرف‌کنندهٔ متفاوت با خطای نوع‌دار رد می‌شود.
    /// </summary>
    /// <param name="reservationId">شناسهٔ رزرو.</param>
    /// <param name="consumerKey">هویت پایدار و غیرتهی مصرف‌کننده.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    public async Task ConsumeAsync(Guid reservationId, string consumerKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(consumerKey))
        {
            throw new ContractOperationException(InventoryErrorCodes.ReservationConsumeMismatch);
        }

        await ConsumeReservationAsync(reservationId, consumerKey.Trim(), cancellationToken);
    }

    /// <summary>
    /// مصرف اتمی رزرو: کسر قطعی موجودی، گذار وضعیت رزرو، ردیف ژورنال مصرف و outbox همان ماژول
    /// در یک تراکنش محلی Inventory. وضعیت رزرو با CAS محافظت می‌شود تا مصرف‌کننده‌های هم‌زمان
    /// سریال شوند و کسر دوباره رخ ندهد. replay فقط با تطابق ژورنال + هویت مصرف‌کننده اثبات می‌شود.
    /// </summary>
    private async Task ConsumeReservationAsync(
        Guid reservationId,
        string? consumerKey,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var now = _clock.UtcNow;

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var reservation = await _db.Reservations
            .SingleOrDefaultAsync(x => x.ReservationId == reservationId, cancellationToken)
            ?? throw new ContractOperationException(InventoryErrorCodes.ReservationNotFound);

        var journal = await FindConsumptionAsync(reservationId, cancellationToken);

        if (consumerKey is not null)
        {
            if (journal is not null)
            {
                if (IsProvenConsumption(journal, consumerKey, reservation.StockItemId, reservation.Quantity))
                {
                    return;
                }

                throw new ContractOperationException(InventoryErrorCodes.ReservationConsumeMismatch);
            }

            if (reservation.Status == StockReservationStatus.Consumed)
            {
                // رزرو مصرف‌شدهٔ قدیمی بدون ردیف ژورنال: provenance اثبات‌شدنی نیست، پس موفقیت مصنوعی ممنوع.
                throw new ContractOperationException(InventoryErrorCodes.ReservationConsumeMismatch);
            }
        }

        if (reservation.Status != StockReservationStatus.Held)
        {
            throw new ContractOperationException(InventoryErrorCodes.ReservationNotActive);
        }

        var claimed = await _db.Reservations
            .Where(x => x.ReservationId == reservationId && x.Status == StockReservationStatus.Held)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Status, StockReservationStatus.Consumed)
                    .SetProperty(x => x.UpdatedAt, now),
                cancellationToken);
        if (claimed != 1)
        {
            if (consumerKey is not null
                && await FindConsumptionAsync(reservationId, cancellationToken) is { } raced
                && IsProvenConsumption(raced, consumerKey, reservation.StockItemId, reservation.Quantity))
            {
                return;
            }

            throw new ContractOperationException(InventoryErrorCodes.ReservationConsumeMismatch);
        }

        var consumePosition = await _db.Positions.AsNoTracking()
            .SingleAsync(x => x.StockItemId == reservation.StockItemId, cancellationToken);
        await LockOfferAllocationAsync(consumePosition.OfferId, cancellationToken);
        var consumeAllocation = await _db.OfferAllocations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OfferId == consumePosition.OfferId, cancellationToken);

        if (consumeAllocation is not null && (reservation.Purpose is null or StockReservationPurpose.NewSale))
        {
            var consumeRevision = _ids.NewId();
            var allocationConsumed = await _db.OfferAllocations
                .Where(x => x.OfferId == consumePosition.OfferId
                    && x.Quantity >= reservation.Quantity && x.Held >= reservation.Quantity)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.Quantity, x => x.Quantity - reservation.Quantity)
                        .SetProperty(x => x.Held, x => x.Held - reservation.Quantity)
                        .SetProperty(x => x.Revision, consumeRevision)
                        .SetProperty(x => x.UpdatedAt, now),
                    cancellationToken);
            if (allocationConsumed != 1)
                throw new ContractOperationException(InventoryErrorCodes.ReservationConsumeMismatch);
        }

        if (consumeAllocation is not null && reservation.Purpose == StockReservationPurpose.PaidOrderRecovery)
        {
            // An unpaid/new sale cannot enter this path. An existing paid obligation may use physical
            // stock even if the remaining quota is zero. Debit only unheld commercial quota, never
            // steal another customer's reserved allocation. A replacement never debits a second sale.
            var debit = Math.Min(reservation.Quantity, Math.Max(0m, consumeAllocation.Quantity - consumeAllocation.Held));
            if (debit > 0)
            {
                var recoveryRevision = _ids.NewId();
                var updated = await _db.OfferAllocations
                    .Where(x => x.OfferId == consumePosition.OfferId && x.Quantity - x.Held >= debit)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.Quantity, x => x.Quantity - debit)
                        .SetProperty(x => x.Revision, recoveryRevision)
                        .SetProperty(x => x.UpdatedAt, now), cancellationToken);
                if (updated != 1)
                    throw new ContractOperationException(InventoryErrorCodes.ReservationConsumeMismatch);
            }
        }

        var consumed = await _db.Positions
            .Where(x => x.StockItemId == reservation.StockItemId
                        && x.Reserved >= reservation.Quantity
                        && x.OnHand >= reservation.Quantity)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.OnHand, x => x.OnHand - reservation.Quantity)
                    .SetProperty(x => x.Reserved, x => x.Reserved - reservation.Quantity)
                    .SetProperty(x => x.UpdatedAt, now),
                cancellationToken);
        if (consumed != 1)
        {
            throw new ContractOperationException(InventoryErrorCodes.ReservationConsumeMismatch);
        }

        _db.ReservationConsumptions.Add(new ReservationConsumptionRecord
        {
            ReservationId = reservationId,
            StockItemId = reservation.StockItemId,
            Quantity = reservation.Quantity,
            ConsumerKey = consumerKey,
            ConsumedAt = now,
        });

        reservation.MoveTo(StockReservationStatus.Consumed, now);
        var position = await _db.Positions.SingleAsync(x => x.StockItemId == reservation.StockItemId, cancellationToken);
        position.RecordConsumed(reservation.ReservationId, reservation.Quantity);
        position.SyncQuantities(position.OnHand, position.Reserved, now);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsReservationConsumptionConflict(ex))
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ContractOperationException(InventoryErrorCodes.ReservationConsumeMismatch);
        }
    }

}
