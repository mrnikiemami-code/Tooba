using Microsoft.EntityFrameworkCore;
using Tooba.Cart.Application.Ports;
using Tooba.Cart.Domain.Aggregates;
using Tooba.Inventory.Contracts.Cart;
using Tooba.Cart.Infrastructure.Persistence;

namespace Tooba.Cart.Infrastructure.Directories;

/// <summary>
/// Cart-owned expiry sweep. Owns the only raw <c>FOR UPDATE SKIP LOCKED</c> claim in the module and
/// its own transaction boundary. Holds are released through the caller-supplied Cart-owned release
/// seam so the reservation policy stays in one place.
/// </summary>
internal sealed class CartExpiryScanner(
    CartDbContext db,
    ICartInventoryHoldPort inventory,
    ICartUseCaseGuard guard)
{
    /// <summary>
    /// Expires every due cart in batches and then releases due Inventory holds.
    /// The release delegate is Cart-owned (<c>ReleaseAllAsync</c>).
    /// </summary>
    public async Task<int> ExpireDueCartsAsync(
        DateTimeOffset utcNow,
        int batchSize,
        Func<ShoppingCart, CancellationToken, Task> releaseHoldsAsync,
        Func<CancellationToken, Task> saveAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(releaseHoldsAsync);
        ArgumentNullException.ThrowIfNull(saveAsync);
        await guard.EnsureCanMutateAsync(cancellationToken);

        var limit = Math.Max(1, batchSize);
        var total = 0;
        while (true)
        {
            var expired = await ExpireDueBatchAsync(utcNow, limit, releaseHoldsAsync, saveAsync, cancellationToken)
                .ConfigureAwait(false);
            total += expired;
            if (expired < limit)
            {
                break;
            }
        }

        await inventory.ReleaseExpiredHoldsAsync(utcNow, limit, cancellationToken).ConfigureAwait(false);
        return total;
    }

    private async Task<int> ExpireDueBatchAsync(
        DateTimeOffset utcNow,
        int batchSize,
        Func<ShoppingCart, CancellationToken, Task> releaseHoldsAsync,
        Func<CancellationToken, Task> saveAsync,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var ids = await db.Database
            .SqlQuery<Guid>(
                $"""
                 SELECT c.cart_id AS "Value"
                 FROM cart.carts AS c
                 WHERE c.status = 'Active'
                   AND c.expires_at IS NOT NULL
                   AND c.expires_at <= {utcNow}
                 ORDER BY c.expires_at
                 LIMIT {batchSize}
                 FOR UPDATE SKIP LOCKED
                 """)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (ids.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return 0;
        }

        var due = await db.Carts
            .Include(x => x.Lines)
            .Where(x => ids.Contains(x.CartId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var cart in due)
        {
            await releaseHoldsAsync(cart, cancellationToken).ConfigureAwait(false);
            cart.Expire(utcNow);
        }

        await saveAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return due.Count;
    }
}
