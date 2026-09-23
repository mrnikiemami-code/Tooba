namespace Tooba.Cart.Application.Ports;

/// <summary>
/// Cart persistence-hours read on the Cart side; TTL of cart state, not inventory reservation.
/// The platform fallback lives in Cart; only a store override arrives through this seam.
/// The foreign store-settings contract is async, so this seam is async and carries cancellation.
/// </summary>
public interface ICartPersistenceHoursResolver
{
    /// <summary>
    /// Configured store override in hours; null means inherit the Cart platform value.
    /// </summary>
    /// <param name="cancellationToken">Cancellation propagated to the foreign store-settings read.</param>
    Task<int?> ResolveOverrideHoursAsync(CancellationToken cancellationToken);
}
