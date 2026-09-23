using Tooba.Cart.Application.Ports;
using Tooba.Catalog.Contracts.Reservation;

namespace Tooba.Cart.Infrastructure.Lifetime;

/// <summary>
/// Cart-owned async adapter over the Catalog-owned store Cart persistence override.
/// No Cart persistence policy or platform fallback is computed here, and the foreign
/// async contract is awaited with the caller's cancellation token.
/// </summary>
public sealed class CatalogCartPersistenceHoursResolver : ICartPersistenceHoursResolver
{
    private readonly IStoreCartPersistenceHoursReader _store;

    /// <summary>Binds the Catalog-owned store settings reader.</summary>
    /// <param name="store">Catalog-owned store override reader.</param>
    public CatalogCartPersistenceHoursResolver(IStoreCartPersistenceHoursReader store) => _store = store;

    /// <inheritdoc />
    public Task<int?> ResolveOverrideHoursAsync(CancellationToken cancellationToken) =>
        _store.GetStoreCartPersistenceHoursAsync(cancellationToken);
}
