namespace Tooba.Inventory.Contracts.Replacement;

/// <summary>
/// TB-262: a requested healthy-stock reservation for a damaged-goods replacement.
///
/// The replacement is for the SAME ordered item: Inventory resolves the offer from the ORIGINAL committed sale
/// reservation, so a client can never supply a product, seller or location identity. The reservation is purpose-bound
/// and idempotent per <paramref name="ReservationKey"/>, so a retried reviewed intent returns the exact same hold
/// instead of reserving a second unit.
/// </summary>
/// <param name="OriginalReservationId">The committed sale reservation of the ordered line (evidence only, never reused).</param>
/// <param name="Quantity">Explicit healthy quantity to reserve.</param>
/// <param name="ReservationKey">Durable purpose-bound idempotency key of THIS replacement decision (stable across attempts, so a retry rebinds the same held allocation).</param>
/// <param name="ExternalReference">Audit reference, e.g. the damaged-goods decision identity.</param>
/// <param name="ExpiresAt">
/// Safety-net expiry for the window BEFORE the hold is linked to an active order. Fulfillment clears it on attach
/// (<c>ExpiresAt = null</c>), exactly like other paid-order reservations, so the expired-hold reclaimer can never
/// release stock that the active order still owes; only an authorized order cancellation releases it, once.
/// </param>
public sealed record DamagedReplacementReservationRequest(
    Guid OriginalReservationId,
    decimal Quantity,
    string ReservationKey,
    string ExternalReference,
    DateTimeOffset ExpiresAt);

/// <summary>TB-262: the exact healthy-stock reservation acquired for one replacement decision.</summary>
public sealed record DamagedReplacementReservationResult(
    Guid ReservationId,
    Guid StockItemId,
    Guid OfferId,
    decimal Quantity,
    string Status);

/// <summary>
/// TB-262: the MINIMAL Inventory-owned capability for a linked paid-order replacement. It reuses Inventory's existing
/// atomic availability/reservation primitives; it never converts damaged goods into saleable stock, never touches the
/// original consumed sale reservation, and never invokes the refund-only return restock path. RETENTION RULE: a hold it
/// creates belongs to the ACTIVE order until dispatch consumes it or an authorized order cancellation releases it —
/// cancelling a replacement parcel, retrying the decision or a hold timeout never returns it to saleable availability.
/// </summary>
public interface IDamagedReplacementInventoryPort
{
    /// <summary>
    /// Reserve healthy stock of the SAME offer as the original committed sale reservation. Idempotent per
    /// <see cref="DamagedReplacementReservationRequest.ReservationKey"/>. Throws the typed
    /// <c>inventory.replacement.unavailable</c> when no location can supply the quantity — with no hold left behind.
    /// </summary>
    Task<DamagedReplacementReservationResult> ReserveForDamagedReplacementAsync(
        DamagedReplacementReservationRequest request,
        CancellationToken cancellationToken);

    /// <summary>Read one replacement reservation by identity (recovery of an uncertain reserve).</summary>
    Task<DamagedReplacementReservationResult?> FindDamagedReplacementReservationAsync(
        Guid reservationId,
        CancellationToken cancellationToken);
}
