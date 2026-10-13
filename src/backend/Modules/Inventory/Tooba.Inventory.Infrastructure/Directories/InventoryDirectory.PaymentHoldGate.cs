using System.Data;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tooba.Inventory.Contracts.Orders;

namespace Tooba.Inventory.Infrastructure.Directories;

/// <summary>
/// Payment-boundary hold gate for <see cref="InventoryDirectory"/>: proves, inside the caller-owned
/// ambient transaction and under PostgreSQL row locks, that every supplied reservation is still Held
/// and unexpired. Read-only: it changes no reservation state, writes no journal and calls no external
/// provider. Extracted into a cohesive partial of the same class.
///
/// Verified against PostgreSQL 17.10 (isolated read-only harness, no Host involvement):
/// opening the connection inside the ambient scope enlists it, <c>FOR UPDATE</c> then holds the row
/// locks for the whole scope (a competing <c>FOR UPDATE</c> is blocked until the scope ends) and the
/// locks are released when the scope ends. A caller that enlists a second connection inside the same
/// scope promotes it to a distributed transaction, which needs <c>max_prepared_transactions</c>: keep
/// one connection per scope. This primitive alone does not prevent the hold from expiring after that
/// commit.
/// </summary>
public sealed partial class InventoryDirectory
{
    /// <inheritdoc />
    public async Task<InventoryPaymentHoldGateResult> VerifyHeldForPaymentAsync(
        IReadOnlyCollection<Guid> reservationIds,
        CancellationToken cancellationToken)
    {
        var requested = NormalizePaymentHoldGateIds(reservationIds);
        if (requested.Length == 0)
        {
            // Nothing nameable was supplied, so nothing was proven.
            return EvaluatePaymentHoldGate(requested, []);
        }

        // No caller-owned ambient transaction: locks taken here (or in an independent transaction)
        // would not outlive this method. Fail closed instead of faking a transaction boundary.
        if (Transaction.Current is null)
        {
            return InventoryPaymentHoldGateResult.AmbientTransactionRequired(requested.Length);
        }

        // An already-open connection cannot be proven enlisted in the ambient scope through a
        // provider-neutral API, so enlistment is refused rather than assumed.
        if (_db.Database.GetDbConnection().State == ConnectionState.Open)
        {
            return InventoryPaymentHoldGateResult.AmbientTransactionRequired(requested.Length);
        }

        // Opening inside the ambient scope is what enlists the connection in it. The connection is
        // deliberately left open so the row locks survive until the caller's transaction ends.
        await _db.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        // Deterministic lock order (reservation_id) keeps this blocking FOR UPDATE free of deadlock
        // with the reclaimer's FOR UPDATE SKIP LOCKED, which only ever targets already-expired rows.
        // Validity uses the database clock: caller-supplied time is never trusted. A NULL expires_at
        // is the existing durable-hold policy (no time expiry).
        var liveReservationIds = await _db.Database
            .SqlQuery<Guid>(
                $"""
                 SELECT r.reservation_id AS "Value"
                 FROM inventory.reservations AS r
                 WHERE r.reservation_id = ANY({requested})
                   AND r.status = 'Held'
                   AND (r.expires_at IS NULL OR r.expires_at > clock_timestamp())
                 ORDER BY r.reservation_id
                 FOR UPDATE
                 """)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (liveReservationIds.Count > 0
            && !await HasOpenTransactionBlockAsync(cancellationToken).ConfigureAwait(false))
        {
            return InventoryPaymentHoldGateResult.AmbientTransactionRequired(requested.Length);
        }

        return EvaluatePaymentHoldGate(requested, liveReservationIds);
    }

    /// <summary>
    /// Confirms on this very connection that the locking statement really ran inside an open
    /// transaction block. <c>FOR UPDATE</c> forces an xid assignment that survives to the next
    /// statement only while that block is open; without the confirmation the row locks would already
    /// be gone (implicit single-statement transaction), so the guarantee is refused instead of assumed.
    /// The xid is a read-only observation: no state, no journal, no external call.
    /// </summary>
    private async Task<bool> HasOpenTransactionBlockAsync(CancellationToken cancellationToken)
    {
        try
        {
            var xidAssigned = await _db.Database
                .SqlQuery<int>(
                    $"""
                     SELECT (CASE WHEN txid_current_if_assigned() IS NOT NULL THEN 1 ELSE 0 END)::int AS "Value"
                     """)
                .SingleAsync(cancellationToken)
                .ConfigureAwait(false);
            return xidAssigned == 1;
        }
        catch (PostgresException)
        {
            // Transaction-block evidence is unreadable (permissions/provider): refuse the guarantee.
            return false;
        }
    }

    /// <summary>
    /// Pure, database-free verdict over already-proven liveness: every requested DISTINCT reservation
    /// must be live, <see cref="Guid.Empty"/> is never live, and an empty request proves nothing.
    /// </summary>
    /// <param name="requestedReservationIds">شناسه‌های درخواستی؛ تکراری‌ها و ترتیب بی‌اثرند.</param>
    /// <param name="liveReservationIds">شناسه‌هایی که در همان تراکنش زنده اثبات شدند.</param>
    public static InventoryPaymentHoldGateResult EvaluatePaymentHoldGate(
        IReadOnlyCollection<Guid> requestedReservationIds,
        IReadOnlyCollection<Guid> liveReservationIds)
    {
        ArgumentNullException.ThrowIfNull(requestedReservationIds);
        ArgumentNullException.ThrowIfNull(liveReservationIds);

        var requested = NormalizePaymentHoldGateIds(requestedReservationIds);
        if (requested.Length == 0)
        {
            return InventoryPaymentHoldGateResult.NotHeld(0, []);
        }

        var live = new HashSet<Guid>(liveReservationIds);
        live.Remove(Guid.Empty);
        var unavailable = requested.Where(id => !live.Contains(id)).ToArray();
        return unavailable.Length == 0
            ? InventoryPaymentHoldGateResult.Verified(requested.Length)
            : InventoryPaymentHoldGateResult.NotHeld(requested.Length, unavailable);
    }

    /// <summary>
    /// Deduplicates and deterministically orders the requested ids so neither duplicates nor caller
    /// ordering can change the verdict. Empty ids are kept on purpose so they can never verify.
    /// </summary>
    private static Guid[] NormalizePaymentHoldGateIds(IReadOnlyCollection<Guid> reservationIds)
    {
        ArgumentNullException.ThrowIfNull(reservationIds);
        return reservationIds
            .Distinct()
            .OrderBy(id => id)
            .ToArray();
    }
}
