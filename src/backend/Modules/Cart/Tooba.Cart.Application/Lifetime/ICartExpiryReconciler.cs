namespace Tooba.Cart.Application.Lifetime;

/// <summary>
/// Cart-owned expiry reconciliation entry point. Host background workers stay thin execution shells
/// and must resolve this reconciler instead of Cart internals.
/// </summary>
public interface ICartExpiryReconciler
{
    /// <summary>
    /// Expires due carts for the current commerce context in bounded batches and releases any
    /// expired inventory holds. Business time comes from <c>IClock</c> inside the implementation.
    /// Returns the number of expired carts.
    /// </summary>
    Task<int> ReconcileAsync(int batchSize, CancellationToken cancellationToken);
}
