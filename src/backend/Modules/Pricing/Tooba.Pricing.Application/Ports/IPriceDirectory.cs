using Tooba.Offer.Contracts.Dtos;
using Tooba.Pricing.Contracts;

namespace Tooba.Pricing.Application;

/// <summary>
/// درز نگهبان مجوز موردکاربرد Pricing. ماتریس ادمین قیمت اینجا نیست.
/// </summary>
public interface IPricingUseCaseGuard
{
    /// <summary>
    /// اجازهٔ نوشتن قیمت را بررسی می‌کند. پیاده‌سازی فعلی فقط درز است.
    /// </summary>
    Task EnsureCanMutateAsync(CancellationToken cancellationToken);
}

/// <summary>
/// نوشتن foundation قیمت. پروموشن، مالیات، FX و UI اینجا نیستند.
/// </summary>
public interface IPriceDirectory
{
    /// <summary>
    /// قیمت نوشته‌شده می‌سازد پس از تأیید Offer از قرارداد Lookup نه از DbContext Offer.
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
    /// قیمت کمپین مرچندایزینگ می‌سازد (QualifierKind=MerchandisingCampaign، QualifierKey=CampaignId).
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

    /// <summary>
    /// قیمت را برای انتخاب پایه فعال می‌کند.
    /// </summary>
    Task ActivateAsync(Guid priceId, CancellationToken cancellationToken);

    /// <summary>
    /// مبلغ نوشته‌شده را عوض می‌کند. نتیجهٔ FX را جای حقیقت نمی‌گذارد.
    /// </summary>
    Task ChangeAmountAsync(Guid priceId, decimal amount, string currency, CancellationToken cancellationToken);

    /// <summary>
    /// قیمت را از انتخاب خارج می‌کند.
    /// </summary>
    Task ExpireAsync(Guid priceId, CancellationToken cancellationToken);
}
