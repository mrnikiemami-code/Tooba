namespace Tooba.Pricing.Contracts.Seller;

/// <summary>Durable Pricing-owned fact for one price mutation.</summary>
public sealed record SellerPriceChangeFact(
    Guid FactId,
    Guid OfferId,
    Guid PriceId,
    Guid OperationId,
    Guid? ActorUserId,
    DateTimeOffset OccurredAt,
    decimal? OldAmount,
    decimal NewAmount,
    string Currency,
    string Revision);

/// <summary>Current owner revision and committed amount for one Offer.</summary>
public sealed record SellerPriceRevision(string Revision, Guid PriceId, decimal Amount, string Currency, DateTimeOffset OccurredAt);

/// <summary>Read-only Pricing boundary used by Offer history/detail; no foreign DbContext.</summary>
public interface IOfferPriceHistoryReader
{
    /// <summary>Lists durable price change facts for one Offer.</summary>
    Task<IReadOnlyList<SellerPriceChangeFact>> ListAsync(Guid offerId, CancellationToken cancellationToken);
    /// <summary>Returns the current owner revision and committed amount for one Offer.</summary>
    Task<SellerPriceRevision?> GetRevisionAsync(Guid offerId, CancellationToken cancellationToken);
}
