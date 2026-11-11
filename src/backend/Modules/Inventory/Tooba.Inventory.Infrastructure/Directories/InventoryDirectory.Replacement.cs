using Microsoft.EntityFrameworkCore;

using Tooba.BuildingBlocks;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Contracts.Replacement;
using Tooba.Inventory.Domain.Aggregates;
using Tooba.Inventory.Domain.ValueObjects;

namespace Tooba.Inventory.Infrastructure.Directories;

/// <summary>
/// TB-262: the MINIMAL Inventory-owned capability a damaged-goods replacement needs.
///
/// It reuses the existing atomic reservation primitives: the offer identity comes ONLY from the original committed
/// sale reservation (never from a client), availability is read from the owning position rows, and the hold is created
/// with the caller's durable purpose-bound idempotency key so a retried reviewed intent returns the SAME reservation.
///
/// It deliberately does NOT:
/// <list type="bullet">
/// <item>restock or otherwise make damaged goods saleable (the refund-only return restock path stays untouched),</item>
/// <item>release, re-consume or re-commit the original sale reservation,</item>
/// <item>introduce a quarantine / bin / warehouse-transfer model.</item>
/// </list>
/// </summary>
public sealed partial class InventoryDirectory : IDamagedReplacementInventoryPort
{
    /// <inheritdoc />
    public async Task<DamagedReplacementReservationResult> ReserveForDamagedReplacementAsync(
        DamagedReplacementReservationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.OriginalReservationId == Guid.Empty)
        {
            throw new ContractOperationException(InventoryErrorCodes.ReservationIdRequired);
        }

        if (request.Quantity <= 0m)
        {
            throw new ContractOperationException(InventoryErrorCodes.ReservationQuantityInvalid);
        }

        var reservationKey = string.IsNullOrWhiteSpace(request.ReservationKey)
            ? throw new ContractOperationException(InventoryErrorCodes.ReservationIdRequired)
            : request.ReservationKey.Trim();

        // A retried reviewed intent answers its OWN hold, so a lost response can never reserve a second unit.
        var prior = await _db.Reservations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == reservationKey, cancellationToken);
        if (prior is not null)
        {
            if (prior.Status != StockReservationStatus.Held || prior.Quantity != request.Quantity)
            {
                throw new ContractOperationException(InventoryErrorCodes.ReservationConflict);
            }

            var known = await _db.Positions.AsNoTracking()
                .SingleAsync(x => x.StockItemId == prior.StockItemId, cancellationToken);
            return new DamagedReplacementReservationResult(
                prior.ReservationId, prior.StockItemId, known.OfferId, prior.Quantity, prior.Status.ToString());
        }

        // The ordered item identity is committed evidence: reservation -> position -> offer. A client cannot supply it.
        var original = await _db.Reservations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ReservationId == request.OriginalReservationId, cancellationToken)
            ?? throw new ContractOperationException(InventoryErrorCodes.ReservationNotFound);
        var originalPosition = await _db.Positions.AsNoTracking()
            .SingleAsync(x => x.StockItemId == original.StockItemId, cancellationToken);

        var candidates = await _db.Positions.AsNoTracking()
            .Where(x => x.OfferId == originalPosition.OfferId && x.OnHand - x.Reserved >= request.Quantity)
            .OrderByDescending(x => x.StockItemId == originalPosition.StockItemId)
            .ThenByDescending(x => x.OnHand - x.Reserved)
            .Select(x => x.StockItemId)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            // Typed, precise and leak-free: no hold is created, so a retry of the same unresolved intent is safe.
            throw new ContractOperationException(InventoryErrorCodes.SupplyUnavailable);
        }

        var receipt = await ReserveAsync(
            candidates[0],
            request.Quantity,
            request.ExternalReference,
            reservationKey,
            // TB-262-R1: ATOMIC paid-order retention. The purpose-bound replacement hold is created with NO expiry
            // from its first durable commit, so a crash before the Fulfillment link cannot leave active-order stock
            // expirable and no default/caller TTL can reintroduce one for this purpose.
            expiresAt: null,
            cancellationToken);

        return new DamagedReplacementReservationResult(
            receipt.ReservationId, receipt.StockItemId, receipt.OfferId, receipt.Quantity, receipt.Status.ToString());
    }

    /// <inheritdoc />
    public async Task<DamagedReplacementReservationResult?> FindDamagedReplacementReservationAsync(
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        if (reservationId == Guid.Empty)
        {
            return null;
        }

        var receipt = await FindReservationAsync(reservationId, cancellationToken);
        return receipt is null
            ? null
            : new DamagedReplacementReservationResult(
                receipt.ReservationId, receipt.StockItemId, receipt.OfferId, receipt.Quantity, receipt.Status.ToString());
    }
}
