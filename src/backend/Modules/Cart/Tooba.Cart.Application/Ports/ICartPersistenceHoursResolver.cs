namespace Tooba.Cart.Application.Ports;

/// <summary>
/// Cart persistence-hours read on the Cart side; TTL of cart state, not inventory reservation.
/// The platform fallback lives in Cart; only a store override arrives through this seam.
/// </summary>
public interface ICartPersistenceHoursResolver
{
    /// <summary>Configured store override in hours; null means inherit the Cart platform value.</summary>
    int? ResolveOverrideHours();
}
