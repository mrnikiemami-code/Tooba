namespace Tooba.Offer.Contracts.Ports;

/// <summary>
/// Development-only Offer seed mutations owned by the Offer module.
/// Host orchestrators may invoke this without touching Offer persistence.
/// </summary>
public interface IOfferDevelopmentSeedGateway
{
    /// <summary>
    /// Ensures the offer is Active (no-op when already Active).
    /// </summary>
    Task EnsureActiveAsync(Guid offerId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates an Active offer cloned from any existing Active template for the given seller SKU,
    /// or activates the existing SKU row. Returns null when no template offer exists.
    /// </summary>
    Task<Guid?> EnsureActiveCloneFromAnyActiveAsync(
        string sellerSku,
        Guid sellerPartyId,
        CancellationToken cancellationToken);
}