using Tooba.Inventory.Application.Ports;
using Microsoft.EntityFrameworkCore;

namespace Tooba.Inventory.Infrastructure.Directories;

/// <summary>
/// Expired-hold reclaimer for <see cref="InventoryDirectory"/>: batch-wise release of Held
/// reservations whose TTL elapsed, using PostgreSQL FOR UPDATE SKIP LOCKED inside one transaction
/// per batch. Extracted into a cohesive partial of the same class so behavior is unchanged.
/// </summary>
public sealed partial class InventoryDirectory
{
    /// <inheritdoc />
    public async Task<int> ReleaseExpiredHoldsAsync(DateTimeOffset utcNow, int batchSize, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var limit = Math.Max(1, batchSize);
        var total = 0;
        while (true)
        {
            var released = await ReleaseExpiredBatchAsync(utcNow, limit, cancellationToken).ConfigureAwait(false);
            total += released;
            if (released < limit)
            {
                break;
            }
        }

        return total;
    }

    private async Task<int> ReleaseExpiredBatchAsync(DateTimeOffset utcNow, int batchSize, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var reservationIds = await _db.Database
            .SqlQuery<Guid>(
                $"""
                 SELECT r.reservation_id AS "Value"
                 FROM inventory.reservations AS r
                 WHERE r.status = 'Held'
                   AND r.expires_at IS NOT NULL
                   AND r.expires_at <= {utcNow}
                 ORDER BY r.expires_at
                 LIMIT {batchSize}
                 FOR UPDATE SKIP LOCKED
                 """)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (reservationIds.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return 0;
        }

        foreach (var reservationId in reservationIds)
        {
            await ReleaseAsync(reservationId, cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return reservationIds.Count;
    }
}
