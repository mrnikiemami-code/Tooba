using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Domain.Aggregates;

namespace Tooba.Offer.Application;

/// <summary>Maps Offer domain types to public contract types explicitly.</summary>
public static class OfferContractMapping
{
    /// <summary>Maps a domain aggregate to its public contract.</summary>
    public static OfferReference ToReference(this SellerOffer offer) => new(
        offer.OfferId, offer.CatalogVariantId, offer.SellerPartyId,
        (SalesChannel)(int)offer.Channel, (OfferStatus)(int)offer.Status,
        offer.SellerSku, offer.ReturnPolicyChoice, offer.CustomReturnWindowDays,
        offer.MinimumOrderQuantity, offer.MaximumOrderQuantity);
}
