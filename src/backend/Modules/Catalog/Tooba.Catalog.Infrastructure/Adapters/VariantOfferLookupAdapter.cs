using Tooba.Catalog.Application.Variants.Ports;
using Tooba.Offer.Contracts.Ports;

namespace Tooba.Catalog.Infrastructure.Adapters;

/// <summary>
/// Catalog Infrastructure adapter: Offer count lookup via Offer.Contracts only.
/// </summary>
public sealed class VariantOfferLookupAdapter(IOfferLookupGateway offers) : IVariantOfferLookup
{
    /// <inheritdoc />
    public Task<IReadOnlyDictionary<Guid, int>> CountOffersByCatalogVariantIdsAsync(
        IReadOnlyCollection<Guid> catalogVariantIds,
        CancellationToken cancellationToken) =>
        offers.CountOffersByCatalogVariantIdsAsync(catalogVariantIds, cancellationToken);
}
