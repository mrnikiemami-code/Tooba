using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;

namespace Tooba.Returns.Infrastructure.Directories;

/// <summary>
/// TB-279 (E): the shared refund-budget critical section of ONE owning payment.
/// <para>
/// Payment Contracts publish the payment and its amount but no remaining refundable balance, so the Returns module
/// keeps its OWN reservation arithmetic over the attempt rows it owns. Two seller cases of the same checkout payment
/// therefore serialize on the same transaction-scoped advisory lock while the budget is checked and the new attempt
/// (its reservation) is persisted, and the lock is released when that short transaction ends. No Payment/Wallet table
/// is locked and no cross-module SQL is issued.
/// </para>
/// <para>
/// The lock is deliberately separate from the seller-order lock: two cases of one checkout may belong to different
/// seller orders, so ordering is seller order → case → payment budget everywhere money is touched.
/// </para>
/// </summary>
public static class ReturnBudgetSerialization
{
    /// <summary>
    /// Stable namespace folded into the advisory-lock key. The pair (namespace, payment) is this invariant's identity
    /// in the database; it must never be reused for another invariant.
    /// </summary>
    public const long AdvisoryLockNamespace = unchecked((long)0x544F4F4241000279UL);

    /// <summary>The exact lock key of one owning payment's refund budget.</summary>
    public static long AdvisoryLockKey(Guid paymentId)
    {
        var bytes = paymentId.ToByteArray();
        var value = 0L;
        for (var index = 0; index < 8; index++)
        {
            value = (value << 8) ^ bytes[index];
        }

        return value ^ AdvisoryLockNamespace;
    }

    /// <summary>
    /// Takes the transaction-scoped advisory lock of the payment budget. It MUST run inside an open transaction; the
    /// lock is released automatically when that transaction commits, rolls back or the connection is lost, which is why
    /// no manual release path exists.
    /// </summary>
    public static async Task AcquireAsync(
        DatabaseFacade database,
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(database);
        if (!database.IsRelational())
        {
            // A controlled unit-test provider cannot serialize; the owning paths are proven relationally instead.
            return;
        }

        if (database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("returns.budget_lock.transaction_required");
        }

        var key = AdvisoryLockKey(paymentId);
        await database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({key})", cancellationToken);
    }
}
