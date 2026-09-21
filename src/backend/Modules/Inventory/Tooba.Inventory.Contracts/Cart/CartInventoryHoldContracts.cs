namespace Tooba.Inventory.Contracts.Cart;

/// <summary>Minimal Cart-facing inventory hold release without Inventory.Application leakage.</summary>
public interface ICartInventoryHoldPort
{
    /// <summary>Releases a Held reservation.</summary>
    Task ReleaseAsync(Guid reservationId, CancellationToken cancellationToken);

    /// <summary>Releases expired Held reservations in batches.</summary>
    Task<int> ReleaseExpiredHoldsAsync(DateTimeOffset utcNow, int batchSize, CancellationToken cancellationToken);
}
