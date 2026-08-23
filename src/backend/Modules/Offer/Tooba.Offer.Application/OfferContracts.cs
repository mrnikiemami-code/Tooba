using Tooba.Offer.Domain;

namespace Tooba.Offer.Application;

/// <summary>
/// مرجع پایدار Offer بدون نشت EF. مبلغ و موجودی ندارد؛ هویت User ورود نیست.
/// </summary>
public sealed record OfferReference(
    Guid OfferId,
    Guid CatalogVariantId,
    Guid SellerPartyId,
    SalesChannel Channel,
    OfferStatus Status,
    string? SellerSku);

/// <summary>
/// درز خواندن Offer برای Pricing و Inventory آینده. Search منبع حقیقت Offer نمی‌شود.
/// </summary>
public interface IOfferLookupGateway
{
    /// <summary>
    /// Offer را در پایگاه Tenant یا Marketplace جاری پیدا می‌کند؛ Host parse نمی‌شود.
    /// </summary>
    Task<OfferReference?> FindOfferAsync(Guid offerId, CancellationToken cancellationToken);
}

/// <summary>
/// درز نگهبان مجوز موردکاربرد Offer. ماتریس نهایی Seller Portal و SDK اسپایس‌دی‌بی اینجا نیست.
/// </summary>
public interface IOfferUseCaseGuard
{
    /// <summary>
    /// اجازهٔ نوشتن listing را بررسی می‌کند. پیاده‌سازی فعلی فقط درز است.
    /// </summary>
    Task EnsureCanMutateAsync(CancellationToken cancellationToken);
}

/// <summary>
/// نوشتن foundation Offer. Pricing، Tax، Inventory و UI فروشنده اینجا نیستند.
/// </summary>
public interface IOfferDirectory
{
    /// <summary>
    /// listing می‌سازد پس از تأیید Variant از قرارداد Catalog و سازمان فروشنده از قرارداد Party؛ DbContext آن ماژول‌ها خوانده نمی‌شود.
    /// </summary>
    Task<OfferReference> CreateOfferAsync(
        Guid catalogVariantId,
        Guid sellerPartyId,
        SalesChannel channel,
        string? sellerSku,
        CancellationToken cancellationToken);

    /// <summary>
    /// listing را فعال می‌کند. اعتبار قیمت یا موجودی را اعلام نمی‌کند.
    /// </summary>
    Task ActivateAsync(Guid offerId, CancellationToken cancellationToken);

    /// <summary>
    /// listing را معلق می‌کند. موجودی صفر نیست.
    /// </summary>
    Task SuspendAsync(Guid offerId, CancellationToken cancellationToken);

    /// <summary>
    /// listing را بایگانی می‌کند تا جای همان فروشنده و گونه و کانال آزاد شود.
    /// </summary>
    Task ArchiveAsync(Guid offerId, CancellationToken cancellationToken);
}
