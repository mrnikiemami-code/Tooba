using System.Data;
using System.Data.Common;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using Tooba.Inventory.Contracts.Orders;

namespace Tooba.Inventory.Infrastructure.Directories;

/// <summary>
/// Atomic hold extension for <see cref="InventoryDirectory"/>: in exactly one Inventory-owned
/// PostgreSQL transaction and connection it proves — under row locks, with the database clock — that
/// every supplied reservation is still Held and unexpired, and only then raises each finite expiry to
/// <c>max(existing, requested)</c>. All-or-none: one unproven id commits nothing. Read of no other
/// module's data, no PSP call, no wallet debit, no Order/Cart write, no ambient scope. Extracted into
/// a cohesive partial of the same class.
///
/// The row locks live only for this call's transaction and are gone once the method returns; the
/// connection is opened and closed by the DbContext itself, so no connection ownership leaks.
/// This primitive alone does not remove expiry during PSP processing and does not make payment
/// initiation race-free.
/// </summary>
public sealed partial class InventoryDirectory
{
    /// <inheritdoc />
    public async Task<InventoryPaymentHoldExtensionResult> ExtendHeldForPaymentAsync(
        IReadOnlyCollection<Guid> reservationIds,
        DateTimeOffset payWindowDeadlineUtc,
        CancellationToken cancellationToken)
    {
        var requested = NormalizeHoldExtensionIds(reservationIds);
        if (requested.Length == 0)
        {
            // Nothing nameable was supplied, so nothing can be extended.
            return InventoryPaymentHoldExtensionResult.NotHeld(0, []);
        }

        if (payWindowDeadlineUtc == default)
        {
            return InventoryPaymentHoldExtensionResult.DeadlineInvalid(requested.Length);
        }

        // This seam owns its own single transaction; an ambient multi-module scope would silently turn
        // the operation into a distributed transaction. Fail closed instead.
        if (Transaction.Current is not null)
        {
            return InventoryPaymentHoldExtensionResult.AmbientTransactionUnsupported(requested.Length);
        }

        var deadlineUtc = payWindowDeadlineUtc.ToUniversalTime();
        await _guard.EnsureCanMutateAsync(cancellationToken).ConfigureAwait(false);

        await using var transaction = await _db.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        var connection = _db.Database.GetDbConnection();
        var dbTransaction = transaction.GetDbTransaction();

        // Deterministic lock order (reservation_id) keeps this blocking FOR UPDATE free of deadlock with
        // the reclaimer's FOR UPDATE SKIP LOCKED, which only ever targets already-expired rows.
        var liveReservationIds = await LockLiveHeldReservationIdsAsync(
            connection,
            dbTransaction,
            requested,
            cancellationToken).ConfigureAwait(false);
        if (liveReservationIds.Count != requested.Length)
        {
            var live = new HashSet<Guid>(liveReservationIds);
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return InventoryPaymentHoldExtensionResult.NotHeld(
                requested.Length,
                requested.Where(id => !live.Contains(id)).ToArray());
        }

        // The deadline is judged against the database clock, never a caller-supplied "now".
        var databaseNowUtc = await ReadDatabaseNowUtcAsync(connection, dbTransaction, cancellationToken)
            .ConfigureAwait(false);
        if (deadlineUtc <= databaseNowUtc)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return InventoryPaymentHoldExtensionResult.DeadlineInvalid(requested.Length);
        }

        var effectiveExpiries = await ExtendLockedHoldsAsync(
            connection,
            dbTransaction,
            requested,
            deadlineUtc,
            _clock.UtcNow,
            cancellationToken).ConfigureAwait(false);
        if (effectiveExpiries.Count != requested.Length)
        {
            // Defensive: the locked rows cannot change under us, so this is unreachable by design.
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return InventoryPaymentHoldExtensionResult.NotHeld(requested.Length, requested);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return InventoryPaymentHoldExtensionResult.Extended(
            requested.Length,
            ComputeGuaranteedUntil(effectiveExpiries));
    }

    /// <summary>
    /// Locks and returns the subset of <paramref name="requestedReservationIds"/> that is Held and
    /// unexpired right now. Rows that are missing, Released, Consumed or expired are simply absent,
    /// which is what makes the verdict fail closed.
    /// </summary>
    private static async Task<List<Guid>> LockLiveHeldReservationIdsAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid[] requestedReservationIds,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT r.reservation_id
            FROM inventory.reservations AS r
            WHERE r.reservation_id = ANY(@reservationIds)
              AND r.status = 'Held'
              AND (r.expires_at IS NULL OR r.expires_at > clock_timestamp())
            ORDER BY r.reservation_id
            FOR UPDATE
            """;
        command.Parameters.Add(UuidArrayParameter("reservationIds", requestedReservationIds));
        var ids = new List<Guid>(requestedReservationIds.Length);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            ids.Add(reader.GetFieldValue<Guid>(0));
        }

        return ids;
    }

    /// <summary>Database "now" for deadline validation.</summary>
    private static async Task<DateTimeOffset> ReadDatabaseNowUtcAsync(
        DbConnection connection,
        DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT clock_timestamp()";
        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return AsUtcInstant(value);
    }

    /// <summary>
    /// Raises the expiry of the already-locked Held rows: <c>expires_at IS NULL</c> (durable) stays
    /// NULL, finite expiries become <c>max(existing, deadline)</c> so replay can never shorten a hold
    /// and a Released/Expired hold is never revived (the status/expiry predicate is re-checked).
    /// Returns the resulting expiry per row.
    /// </summary>
    private static async Task<List<DateTimeOffset?>> ExtendLockedHoldsAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid[] requestedReservationIds,
        DateTimeOffset deadlineUtc,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            UPDATE inventory.reservations
            SET expires_at = CASE WHEN expires_at IS NULL THEN NULL ELSE GREATEST(expires_at, @deadline) END,
                updated_at = @updatedAt
            WHERE reservation_id = ANY(@reservationIds)
              AND status = 'Held'
              AND (expires_at IS NULL OR expires_at > clock_timestamp())
            RETURNING expires_at
            """;
        command.Parameters.Add(UuidArrayParameter("reservationIds", requestedReservationIds));
        command.Parameters.Add(new NpgsqlParameter("deadline", NpgsqlDbType.TimestampTz)
        {
            Value = deadlineUtc,
        });
        command.Parameters.Add(new NpgsqlParameter("updatedAt", NpgsqlDbType.TimestampTz)
        {
            Value = updatedAtUtc.ToUniversalTime(),
        });

        var expiries = new List<DateTimeOffset?>(requestedReservationIds.Length);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            expiries.Add(reader.IsDBNull(0) ? null : AsUtcInstant(reader.GetValue(0)));
        }

        return expiries;
    }

    /// <summary>
    /// Weakest guarantee of the whole set: the earliest finite expiry after the extension, or
    /// <c>null</c> when every reservation is durable (no time expiry).
    /// </summary>
    private static DateTimeOffset? ComputeGuaranteedUntil(IReadOnlyCollection<DateTimeOffset?> effectiveExpiries)
    {
        DateTimeOffset? weakest = null;
        foreach (var effective in effectiveExpiries)
        {
            if (effective is not { } finite)
            {
                continue;
            }

            if (weakest is null || finite < weakest)
            {
                weakest = finite;
            }
        }

        return weakest;
    }

    /// <summary>Deduplicates and deterministically orders the requested ids; empty ids are kept on purpose.</summary>
    private static Guid[] NormalizeHoldExtensionIds(IReadOnlyCollection<Guid> reservationIds)
    {
        ArgumentNullException.ThrowIfNull(reservationIds);
        return reservationIds
            .Distinct()
            .OrderBy(id => id)
            .ToArray();
    }

    /// <summary>Provider-safe uuid[] parameter so the array binding cannot silently degrade.</summary>
    private static NpgsqlParameter UuidArrayParameter(string name, Guid[] value) =>
        new(name, NpgsqlDbType.Array | NpgsqlDbType.Uuid) { Value = value };

    /// <summary>Normalizes a provider timestamp (timestamptz) to an absolute UTC instant.</summary>
    private static DateTimeOffset AsUtcInstant(object? value) => value switch
    {
        DateTimeOffset offset => offset.ToUniversalTime(),
        DateTime dateTime => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)),
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };
}
