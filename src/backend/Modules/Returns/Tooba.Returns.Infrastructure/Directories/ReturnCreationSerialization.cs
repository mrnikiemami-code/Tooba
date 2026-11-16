using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;

namespace Tooba.Returns.Infrastructure.Directories;

/// <summary>
/// TB-272 (D): the relational critical section of ONE ordinary creation decision for ONE canonical seller order,
/// plus the exact provider identities that make a concurrent same-key insert recoverable.
/// <para>
/// The per-request quantity check alone cannot protect two different keys: both requests read the same remaining
/// quantity before either inserts, so the sum of two accepted requests can exceed the delivered quantity. The
/// decision therefore runs inside Returns' own relational transaction and is serialized with a
/// <b>transaction-scoped advisory lock</b> whose key is derived from the seller order identity. The lock is taken
/// before the eligibility quantity read, so the read observes every creation committed by a previous lock holder,
/// and it is released automatically when that transaction ends (commit, rollback, cancellation or connection loss),
/// which is why no manual release path exists.
/// </para>
/// <para>
/// The lock covers ONLY Returns' own rows: no foreign Order/Fulfillment/Inventory table is locked and no
/// foreign-schema SQL is issued, so the owning modules keep their own transactions. External money/stock/provider
/// work never runs inside this transaction (creation performs none).
/// </para>
/// </summary>
public static class ReturnCreationSerialization
{
    /// <summary>
    /// Stable namespace folded into the advisory-lock key. The pair (namespace, seller order) is this invariant's
    /// identity in the database and must never be reused for another invariant. Changing it would stop existing
    /// writers from serializing against new ones, so it is a durable constant, not a tuning knob.
    /// </summary>
    public const long AdvisoryLockNamespace = unchecked((long)0x544F4F4241000272UL);

    /// <summary>
    /// The ONE unique index that makes two same-key creations collide. A conflict on any other constraint, table or
    /// SQLSTATE is a provider fault and must propagate to the canonical handling, never become a business answer.
    /// </summary>
    public const string IdempotencyUniqueIndex = "ix_return_requests_idempotency_key";

    /// <summary>The owning table of <see cref="IdempotencyUniqueIndex"/>.</summary>
    public const string IdempotencyTable = "return_requests";

    /// <summary>The owning schema of <see cref="IdempotencyUniqueIndex"/>.</summary>
    public const string IdempotencySchema = "returns";

    /// <summary>
    /// Deterministic advisory-lock key of one seller order. Distinct orders may in theory collide on the 64-bit fold,
    /// which can only over-serialize two unrelated creations (never under-serialize), so correctness does not depend
    /// on uniqueness here.
    /// </summary>
    public static long AdvisoryLockKey(Guid sellerOrderId)
    {
        var bytes = sellerOrderId.ToByteArray();
        return AdvisoryLockNamespace ^ BitConverter.ToInt64(bytes, 0);
    }

    /// <summary>
    /// Takes the transaction-scoped advisory lock for <paramref name="sellerOrderId"/>. It must be called inside an
    /// open transaction; PostgreSQL releases it when that transaction ends, so a thrown/cancelled request releases it
    /// without an explicit unlock.
    /// </summary>
    public static async Task AcquireAsync(
        DatabaseFacade database,
        Guid sellerOrderId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(database);
        var key = AdvisoryLockKey(sellerOrderId);
        await database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({key})", cancellationToken);
    }

    /// <summary>
    /// True ONLY for a unique violation of <see cref="IdempotencyUniqueIndex"/> on
    /// <see cref="IdempotencySchema"/>.<see cref="IdempotencyTable"/>. Every other provider failure (a different
    /// constraint, a different table, a serialization failure or a connection fault) is not this invariant's
    /// duplicate and is rethrown untouched.
    /// </summary>
    public static bool IsIdempotencyConflict(DbUpdateException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres
               && string.Equals(postgres.ConstraintName, IdempotencyUniqueIndex, StringComparison.Ordinal)
               && string.Equals(postgres.TableName, IdempotencyTable, StringComparison.Ordinal)
               && (postgres.SchemaName is null
                   || string.Equals(postgres.SchemaName, IdempotencySchema, StringComparison.Ordinal));
    }
}
