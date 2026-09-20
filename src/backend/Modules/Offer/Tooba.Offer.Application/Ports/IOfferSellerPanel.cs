using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Application.Ports;

/// <summary>
/// Residual seller-panel read enrichment and price/inventory write boundary.
/// </summary>
public interface IOfferSellerPanel
{
    /// <summary>Lists enriched offers for a seller.</summary>
    Task<IReadOnlyList<SellerOfferListItem>> ListOffersAsync(Guid sellerPartyId, CancellationToken cancellationToken);

    /// <summary>Gets an enriched seller-owned offer.</summary>
    Task<SellerOfferDetailPage?> GetOfferAsync(Guid sellerPartyId, Guid offerId, CancellationToken cancellationToken);

    /// <summary>Writes price through Pricing for a seller-owned offer.</summary>
    Task<SellerOfferDetailPage> SetOfferPriceAsync(Guid sellerPartyId, Guid offerId, SellerOfferPriceWriteRequest request, CancellationToken cancellationToken);

    /// <summary>Writes inventory through Inventory for a seller-owned offer.</summary>
    Task<SellerOfferDetailPage> SetOfferInventoryAsync(Guid sellerPartyId, Guid offerId, SellerOfferInventoryWriteRequest request, CancellationToken cancellationToken);
}
