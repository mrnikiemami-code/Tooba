using Tooba.BuildingBlocks;
using Tooba.Cart.Application.Lifetime;
using Tooba.Cart.Application.Ports;

namespace Tooba.Cart.Infrastructure.Lifetime;

/// <summary>
/// Cart-owned expiry reconciliation: resolves the business clock and drives the Cart directory.
/// Host never supplies business time or batch policy.
/// </summary>
public sealed class CartExpiryReconciler : ICartExpiryReconciler
{
    private readonly ICartDirectory _carts;
    private readonly IClock _clock;

    /// <summary>Binds the Cart directory and the shared clock.</summary>
    public CartExpiryReconciler(ICartDirectory carts, IClock clock)
    {
        _carts = carts;
        _clock = clock;
    }

    /// <inheritdoc />
    public Task<int> ReconcileAsync(int batchSize, CancellationToken cancellationToken) =>
        _carts.ExpireDueCartsAsync(_clock.UtcNow, Math.Max(1, batchSize), cancellationToken);
}
