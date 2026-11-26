using Microsoft.EntityFrameworkCore;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Inventory.Domain.ValueObjects;
using Tooba.Inventory.Infrastructure.Directories;
using Tooba.Inventory.Infrastructure.Persistence;

namespace Tooba.Inventory.Infrastructure.Adapters;

/// <summary>Serializes responsibility changes with Inventory's existing per-Offer allocation and hold lock.</summary>
public sealed class OfferFulfillmentChangeGate(InventoryDbContext db) : IOfferFulfillmentChangeGate
{
    /// <inheritdoc />
    public async Task<IAsyncDisposable?> TryAcquireAsync(Guid offerId, CancellationToken cancellationToken)
    {
        // Do not borrow or nest an externally owned transaction. Offer assignment uses its own
        // independent unit of work; this transaction is read-only and exists solely for exclusion.
        if (offerId == Guid.Empty || db.Database.CurrentTransaction is not null
            || System.Transactions.Transaction.Current is not null)
            return null;

        var lease = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await InventoryDirectory.LockOfferAllocationAsync(db, offerId, cancellationToken);
            // Even a zero allocation is an explicit commercial intent. No ownership-transfer
            // or allocation-retirement policy has been authorized for this path.
            var protectedAllocation = await db.OfferAllocations.AsNoTracking()
                .AnyAsync(x => x.OfferId == offerId, cancellationToken);
            var protectedPosition = await db.Positions.AsNoTracking()
                .AnyAsync(x => x.OfferId == offerId && x.Reserved > 0, cancellationToken);
            var protectedReservation = await (
                from reservation in db.Reservations.AsNoTracking()
                join position in db.Positions.AsNoTracking() on reservation.StockItemId equals position.StockItemId
                where position.OfferId == offerId
                    && (reservation.Status == StockReservationStatus.Held || reservation.Status == StockReservationStatus.Consumed)
                select reservation.ReservationId).AnyAsync(cancellationToken);
            if (protectedAllocation || protectedPosition || protectedReservation)
            {
                await lease.DisposeAsync();
                return null;
            }
            return lease;
        }
        catch
        {
            await lease.DisposeAsync();
            throw;
        }
    }
}
