using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Contracts.Ports;

/// <summary>
/// Cohesive Offer read port for Host admin, grid, storefront, merchandising, and development seeds.
/// Extends identity lookup with batch metrics and filtered projections. No Pricing/Inventory fields.
/// </summary>
public interface IOfferQueryGateway : IOfferLookupGateway
{
    /// <summary>Counts offers with Active status.</summary>
    Task<int> CountActiveOffersAsync(CancellationToken cancellationToken);

    /// <summary>Distinct seller party ids that own at least one offer row.</summary>
    Task<IReadOnlyList<Guid>> ListDistinctSellerPartyIdsAsync(CancellationToken cancellationToken);

    /// <summary>Seller/status pairs for all offers (admin seller listing metrics).</summary>
    Task<IReadOnlyList<OfferSellerStatusRow>> ListSellerStatusRowsAsync(CancellationToken cancellationToken);

    /// <summary>Active offer counts keyed by seller party id.</summary>
    Task<IReadOnlyDictionary<Guid, int>> CountActiveOffersBySellerAsync(CancellationToken cancellationToken);

    /// <summary>Counts every offer row per catalog variant (all statuses) for product-grid metrics.</summary>
    Task<IReadOnlyDictionary<Guid, int>> CountAllOffersGroupedByCatalogVariantAsync(
        CancellationToken cancellationToken);

    /// <summary>Maps every offer id to its catalog variant id.</summary>
    Task<IReadOnlyDictionary<Guid, Guid>> MapAllOfferIdsToCatalogVariantIdsAsync(
        CancellationToken cancellationToken);

    /// <summary>All offers for the given catalog variants (any status).</summary>
    Task<IReadOnlyList<OfferReference>> ListOffersByCatalogVariantIdsAsync(
        IReadOnlyCollection<Guid> catalogVariantIds,
        CancellationToken cancellationToken);

    /// <summary>Active offers for the given catalog variants.</summary>
    Task<IReadOnlyList<OfferReference>> ListActiveOffersByCatalogVariantIdsAsync(
        IReadOnlyCollection<Guid> catalogVariantIds,
        CancellationToken cancellationToken);

    /// <summary>All active offers (storefront public seller discovery).</summary>
    Task<IReadOnlyList<OfferReference>> ListActiveOffersAsync(CancellationToken cancellationToken);

    /// <summary>True when any offer row references one of the catalog variants.</summary>
    Task<bool> AnyOffersForCatalogVariantIdsAsync(
        IReadOnlyCollection<Guid> catalogVariantIds,
        CancellationToken cancellationToken);

    /// <summary>Most recently updated active offers, newest first.</summary>
    Task<IReadOnlyList<OfferListItem>> ListRecentActiveOffersAsync(
        int take,
        CancellationToken cancellationToken);

    /// <summary>Latest offer for a seller+variant pair, or null.</summary>
    Task<OfferReference?> FindLatestBySellerAndVariantAsync(
        Guid sellerPartyId,
        Guid catalogVariantId,
        CancellationToken cancellationToken);

    /// <summary>True when the seller already owns the given seller SKU.</summary>
    Task<bool> ExistsBySellerSkuAsync(
        Guid sellerPartyId,
        string sellerSku,
        CancellationToken cancellationToken);

    /// <summary>Offer ids whose seller SKU starts with the given prefix.</summary>
    Task<IReadOnlyList<Guid>> ListOfferIdsBySellerSkuPrefixAsync(
        string sellerSkuPrefix,
        CancellationToken cancellationToken);

    /// <summary>Finds an offer by exact seller SKU.</summary>
    Task<OfferReference?> FindBySellerSkuAsync(
        string sellerSku,
        CancellationToken cancellationToken);
}
