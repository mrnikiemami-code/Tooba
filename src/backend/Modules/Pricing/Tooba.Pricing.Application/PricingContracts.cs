using Tooba.Offer.Domain;
using Tooba.Pricing.Domain;

namespace Tooba.Pricing.Application;

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
}

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
