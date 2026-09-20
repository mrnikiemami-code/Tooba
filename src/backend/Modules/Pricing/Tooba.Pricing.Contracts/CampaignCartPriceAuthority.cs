using Tooba.Offer.Domain;

namespace Tooba.Pricing.Contracts;

/// <summary>
/// مرجع قیمت کمپین مرچندایزینگ برای Cart/Checkout؛ مبلغ را از AuthoredPrice کمپین می‌خواند نه از کلاینت.
/// اگر کمپین واجد شرایط نباشد null برمی‌گرداند تا فراخواننده به قیمت Base برگردد.
/// </summary>
public interface ICampaignCartPriceAuthority
{
    /// <summary>
    /// قیمت کمپین واجد شرایط را برای Offer در Market/Channel/Currency/At برمی‌گرداند.
    /// </summary>
    Task<PriceQuote?> TryResolveEligibleCampaignPriceAsync(
        Guid merchandisingCampaignId,
        Guid offerId,
        string market,
        SalesChannel channel,
        string currency,
        DateTimeOffset at,
        CancellationToken cancellationToken);
}
