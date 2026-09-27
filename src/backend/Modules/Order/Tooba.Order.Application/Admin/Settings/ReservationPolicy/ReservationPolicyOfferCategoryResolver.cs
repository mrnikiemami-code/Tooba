using Tooba.Catalog.Contracts;
using Tooba.Offer.Contracts.Ports;

namespace Tooba.Order.Application.Admin.Settings.ReservationPolicy;

/// <summary>Resolves primary Catalog category for Offer via Contracts only.</summary>
internal static class ReservationPolicyOfferCategoryResolver
{
    public static async Task<Guid?> ResolveAsync(
        IOfferQueryGateway offers,
        ICatalogVariantLookup variants,
        Guid offerId,
        CancellationToken cancellationToken)
    {
        var offer = await offers.FindOfferAsync(offerId, cancellationToken);
        if (offer is null)
        {
            return null;
        }

        var map = await variants.GetPrimaryCategoryIdsByVariantIdsAsync(
            [offer.CatalogVariantId],
            cancellationToken);
        return map.GetValueOrDefault(offer.CatalogVariantId);
    }

    public static async Task<IReadOnlyDictionary<Guid, Guid?>> ResolveManyAsync(
        IOfferQueryGateway offers,
        ICatalogVariantLookup variants,
        IReadOnlyList<Guid> offerIds,
        CancellationToken cancellationToken)
    {
        var map = offerIds.Distinct().ToDictionary(x => x, _ => (Guid?)null);
        if (map.Count == 0)
        {
            return map;
        }

        var offerRows = await offers.FindOffersBatchAsync(map.Keys.ToArray(), cancellationToken);
        var variantIds = offerRows.Values.Select(x => x.CatalogVariantId).Distinct().ToArray();
        var categories = await variants.GetPrimaryCategoryIdsByVariantIdsAsync(variantIds, cancellationToken);
        foreach (var row in offerRows.Values)
        {
            if (categories.TryGetValue(row.CatalogVariantId, out var categoryId))
            {
                map[row.OfferId] = categoryId;
            }
        }

        return map;
    }
}
