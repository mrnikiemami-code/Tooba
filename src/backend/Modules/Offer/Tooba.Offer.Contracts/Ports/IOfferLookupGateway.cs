using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Contracts.Ports;

/// <summary>
/// Offer identity lookup port for Pricing, Inventory, Cart, Order, and Promotion.
/// </summary>
public interface IOfferLookupGateway
{
    /// <summary>Finds a single offer by id.</summary>
    Task<OfferReference?> FindOfferAsync(Guid offerId, CancellationToken cancellationToken);

    /// <summary>Batch-resolves offers by id.</summary>
    Task<IReadOnlyDictionary<Guid, OfferReference>> FindOffersBatchAsync(
        IReadOnlyCollection<Guid> offerIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Counts non-archived offers for each catalog variant id.
    /// Missing keys are returned with count 0.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, int>> CountOffersByCatalogVariantIdsAsync(
        IReadOnlyCollection<Guid> catalogVariantIds,
        CancellationToken cancellationToken);
}
