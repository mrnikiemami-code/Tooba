using Tooba.Offer.Application.Offers.Ports;
using Tooba.Offer.Application.Offers.ReadModels;
using Tooba.Offer.Contracts.Ports;

namespace Tooba.Offer.Infrastructure.Adapters;

/// <summary>Resolves distinct product ids for a seller from Offer aggregates + catalog enrichment.</summary>
public sealed class OfferSellerProductIdLookup(
    IOfferStore store,
    OfferReadModelComposer readModels) : IOfferSellerProductIdLookup
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> ListDistinctProductIdsForSellerAsync(
        Guid sellerPartyId,
        CancellationToken cancellationToken)
    {
        var offers = await store.ListBySellerAsync(sellerPartyId, cancellationToken);
        var items = await readModels.ListAsync(offers, cancellationToken);
        return items
            .Where(x => x.ProductId is not null)
            .Select(x => x.ProductId!.Value)
            .Distinct()
            .ToArray();
    }
}
