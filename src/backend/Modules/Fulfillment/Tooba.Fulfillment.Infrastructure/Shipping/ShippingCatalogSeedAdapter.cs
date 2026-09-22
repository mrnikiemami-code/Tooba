using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Contracts.Shipping;

namespace Tooba.Fulfillment.Infrastructure.Shipping;

/// <summary>Contracts seed port → Application shipping directory.</summary>
public sealed class ShippingCatalogSeedAdapter(IShippingServiceDirectory directory) : IShippingCatalogSeedPort
{
    /// <inheritdoc />
    public Task EnsureSeedAsync(CancellationToken cancellationToken) =>
        directory.EnsureSeedAsync(cancellationToken);
}
