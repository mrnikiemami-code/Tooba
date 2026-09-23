using Microsoft.Extensions.Options;
using Tooba.Cart.Application.Lifetime;
using Tooba.Cart.Application.Ports;

namespace Tooba.Cart.Infrastructure.Lifetime;

/// <summary>
/// Cart-owned store persistence-hours resolution over Catalog store settings.
/// Cart keeps the platform fallback and clamping; only the store override is read here.
/// </summary>
public sealed class CartPersistenceHoursSource : ICartPersistenceHoursSource
{
    private readonly IOptions<CartLifetimeOptions> _options;
    private readonly ICartPersistenceHoursResolver? _storeOverride;

    /// <summary>Binds Cart platform options and an optional store override source.</summary>
    public CartPersistenceHoursSource(
        IOptions<CartLifetimeOptions> options,
        ICartPersistenceHoursResolver? storeOverride = null)
    {
        _options = options;
        _storeOverride = storeOverride;
    }

    /// <inheritdoc />
    public int ResolvePersistenceHours() => CartPersistenceHours.Resolve(_options.Value, _storeOverride);
}
