namespace Tooba.Offer.Contracts.Ports;

/// <summary>
/// Offer-owned schema migration entrypoint so Host bootstraps never type OfferDbContext.
/// </summary>
public interface IOfferSchemaMigrator
{
    /// <summary>Applies pending Offer EF migrations.</summary>
    Task MigrateAsync(CancellationToken cancellationToken = default);
}
