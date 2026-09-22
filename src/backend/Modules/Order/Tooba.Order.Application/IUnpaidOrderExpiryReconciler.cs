namespace Tooba.Order.Application;

/// <summary>Order-owned unpaid expiry + reservation release reconciliation.</summary>
public interface IUnpaidOrderExpiryReconciler
{
    /// <summary>
    /// Closes expired reservation cycles, expires due unpaid payments, releases reservations,
    /// and closes active cycles for expired checkouts. Returns expired payment count.
    /// </summary>
    Task<int> ReconcileAsync(int batchSize, CancellationToken cancellationToken);
}
