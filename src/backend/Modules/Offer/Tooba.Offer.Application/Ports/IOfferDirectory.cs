using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Domain;

namespace Tooba.Offer.Application.Ports;

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
