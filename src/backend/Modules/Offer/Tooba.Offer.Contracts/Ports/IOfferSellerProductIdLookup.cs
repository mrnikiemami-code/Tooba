namespace Tooba.Offer.Contracts.Ports;

/// <summary>
/// Narrow seller-scoped product id lookup for cross-module composition (e.g. Reviews seller list).
/// </summary>
public interface IOfferSellerProductIdLookup
{
    /// <summary>
    /// Distinct Catalog product ids for non-archived offers owned by the seller party.
    /// </summary>
    Task<IReadOnlyList<Guid>> ListDistinctProductIdsForSellerAsync(
        Guid sellerPartyId,
        CancellationToken cancellationToken);
}
