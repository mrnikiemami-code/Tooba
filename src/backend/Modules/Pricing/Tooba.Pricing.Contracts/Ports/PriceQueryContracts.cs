using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Pricing.Contracts;

/// <summary>One authored amount used by admin grids. Not a resolved quote.</summary>
public sealed record OfferAmountRow(Guid OfferId, decimal Amount);

/// <summary>
/// Authored price row for Host reads. Status and qualifier are stable names, not EF entities.
/// </summary>
public sealed record AuthoredPriceSnapshot(
    Guid PriceId,
    Guid OfferId,
    string Market,
    SalesChannel Channel,
    decimal Amount,
    string Currency,
    string Status,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    string QualifierKind,
    string? QualifierKey);

/// <summary>Pricing-owned read port so Host never opens PricingDbContext.</summary>
public interface IPriceQueryGateway
{
    /// <summary>Every stored offer/amount pair, including inactive rows.</summary>
    Task<IReadOnlyList<OfferAmountRow>> ListOfferAmountsAsync(CancellationToken cancellationToken);

    /// <summary>All authored rows for the given offers.</summary>
    Task<IReadOnlyList<AuthoredPriceSnapshot>> ListByOfferIdsAsync(
        IReadOnlyCollection<Guid> offerIds,
        CancellationToken cancellationToken);

    /// <summary>Offer ids that already have an active campaign price for the campaign key.</summary>
    Task<IReadOnlyList<Guid>> ListActiveCampaignOfferIdsAsync(
        IReadOnlyCollection<Guid> offerIds,
        string campaignKey,
        CancellationToken cancellationToken);

    /// <summary>Latest active base price for an offer, market, and currency, or null.</summary>
    Task<AuthoredPriceSnapshot?> FindLatestActiveBaseAsync(
        Guid offerId,
        string market,
        string currency,
        CancellationToken cancellationToken);
}
