using Tooba.Cart.Application.Ports;
using Tooba.Catalog.Contracts.Reservation;

namespace Tooba.Host;

/// <summary>
/// Host composition seam: exposes the Catalog-owned store Cart persistence override to Cart.
/// No Cart persistence policy or platform fallback is computed here.
/// </summary>
internal sealed class HostCartPersistenceHoursResolver : ICartPersistenceHoursResolver
{
    private readonly IStoreCartPersistenceHoursReader _store;

    /// <summary>Binds the Catalog-owned store settings reader.</summary>
    public HostCartPersistenceHoursResolver(IStoreCartPersistenceHoursReader store) => _store = store;

    /// <inheritdoc />
    public int? ResolveOverrideHours() =>
        _store.GetStoreCartPersistenceHoursAsync(CancellationToken.None).GetAwaiter().GetResult();
}
