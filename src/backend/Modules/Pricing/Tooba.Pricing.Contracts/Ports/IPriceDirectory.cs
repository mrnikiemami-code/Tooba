using Tooba.Offer.Contracts.Dtos;
using Tooba.Pricing.Contracts.Errors;

namespace Tooba.Pricing.Contracts.Ports;

/// <summary>
/// Pricing-owned authored-price write port. It is the supported cross-module boundary for creating,
/// activating, re-pricing, and expiring an authored price; Promotion and any future consumer reach
/// Pricing only through it (never through a Pricing Application type).
/// </summary>
public interface IPriceDirectory
{
    /// <summary>
    /// Creates an authored price after the Offer is confirmed through the Offer lookup contract,
    /// never through the Offer DbContext.
    /// </summary>
    Task<PriceQuote> CreatePriceAsync(
        Guid offerId,
        string market,
        SalesChannel channel,
        decimal amount,
        string currency,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates a merchandising campaign price (QualifierKind=MerchandisingCampaign, QualifierKey=CampaignId).
    /// </summary>
    Task<PriceQuote> CreateCampaignPriceAsync(
        Guid offerId,
        Guid campaignId,
        string market,
        SalesChannel channel,
        decimal amount,
        string currency,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo,
        CancellationToken cancellationToken);

    /// <summary>Activates an authored price for base selection.</summary>
    Task ActivateAsync(Guid priceId, CancellationToken cancellationToken);

    /// <summary>
    /// Changes the authored amount. An FX result is never stored in place of the authored truth.
    /// </summary>
    Task ChangeAmountAsync(Guid priceId, decimal amount, string currency, CancellationToken cancellationToken);

    /// <summary>Removes an authored price from selection.</summary>
    Task ExpireAsync(Guid priceId, CancellationToken cancellationToken);
}
