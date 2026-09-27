namespace Tooba.Cart.Application.Ports;

/// <summary>
/// Cart-internal alias of <see cref="Tooba.Cart.Contracts.Lifetime.ICartPersistenceHoursSource"/>.
/// Cross-module consumers should prefer the Contracts port.
/// </summary>
public interface ICartPersistenceHoursSource : Tooba.Cart.Contracts.Lifetime.ICartPersistenceHoursSource
{
}
