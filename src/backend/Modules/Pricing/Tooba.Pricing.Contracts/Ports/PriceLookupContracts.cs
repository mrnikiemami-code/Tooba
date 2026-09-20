using Tooba.Offer.Domain;

namespace Tooba.Pricing.Contracts;

/// <summary>
/// نتیجهٔ انتخاب قیمت پایه. مالیات محاسبه‌شده و نرخ FX نیست و قابل‌خرید بودن را تضمین نمی‌کند.
/// </summary>
public sealed record PriceQuote(
    Guid PriceId,
    Guid OfferId,
    string Market,
    SalesChannel Channel,
    decimal Amount,
    string Currency,
    bool TaxExclusive,
    bool IsAuthored);

/// <summary>
/// زمینهٔ انتخاب قیمت. فیلدهای اختیاری درز B2B آینده‌اند و امروز در DB اجباری نیستند.
/// </summary>
public sealed record PriceResolutionQuery(
    Guid OfferId,
    string Market,
    SalesChannel Channel,
    string Currency,
    DateTimeOffset At,
    Guid? CustomerPartyId,
    Guid? OrganizationPartyId,
    decimal? Quantity);

/// <summary>
/// درز خواندن قیمت برای ماژول‌های بعدی بدون نشت EF.
/// </summary>
public interface IPriceLookupGateway
{
    /// <summary>
    /// قیمت پایهٔ نوشته‌شده را در پایگاه Tenant/Marketplace جاری انتخاب می‌کند.
    /// </summary>
    Task<PriceQuote?> ResolvePriceAsync(PriceResolutionQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// قیمت پایهٔ مؤثر چند Offer را در یک خواندن برای Market/Channel/Currency/At برمی‌گرداند.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, PriceQuote>> ResolvePricesBatchAsync(
        IReadOnlyCollection<Guid> offerIds,
        string market,
        SalesChannel channel,
        string currency,
        DateTimeOffset at,
        CancellationToken cancellationToken);

    /// <summary>
    /// قیمت کمپین مرچندایزینگ مؤثر چند Offer را در یک خواندن برای CampaignId + ابعاد قیمت برمی‌گرداند.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, PriceQuote>> ResolveCampaignPricesBatchAsync(
        IReadOnlyCollection<Guid> offerIds,
        Guid campaignId,
        string market,
        SalesChannel channel,
        string currency,
        DateTimeOffset at,
        CancellationToken cancellationToken);
}
