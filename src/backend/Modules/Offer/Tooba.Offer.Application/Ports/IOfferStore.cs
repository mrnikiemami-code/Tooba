using Tooba.Offer.Domain.Aggregates;
using Tooba.Offer.Domain.ValueObjects;

namespace Tooba.Offer.Application.Ports;

/// <summary>Guards Offer mutation use cases.</summary>
public interface IOfferUseCaseGuard
{
    /// <summary>Ensures that the current context can mutate offers.</summary>
    Task EnsureCanMutateAsync(CancellationToken cancellationToken);
}

/// <summary>Persistence-only port for Offer aggregates.</summary>
public interface IOfferStore
{
    /// <summary>Gets a tracked aggregate by identifier.</summary>
    Task<SellerOffer?> GetByIdAsync(Guid offerId, CancellationToken cancellationToken);
    /// <summary>Lists seller aggregates for reads.</summary>
    Task<IReadOnlyList<SellerOffer>> ListBySellerAsync(Guid sellerPartyId, CancellationToken cancellationToken);
    /// <summary>Adds a new aggregate.</summary>
    Task AddAsync(SellerOffer offer, CancellationToken cancellationToken);
    /// <summary>Persists pending aggregate changes.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
    /// <summary>Checks for a non-archived listing.</summary>
    Task<bool> ExistsActiveListingAsync(Guid sellerPartyId, Guid catalogVariantId, SalesChannel channel, CancellationToken cancellationToken);
    /// <summary>Checks seller SKU uniqueness.</summary>
    Task<bool> ExistsSellerSkuAsync(Guid sellerPartyId, string sellerSku, Guid? excludingOfferId, CancellationToken cancellationToken);
}
