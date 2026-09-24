using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Cart.Contracts;

/// <summary>
/// وضعیت عمر سبد (قرارداد پایدار؛ بدون نشت Domain entity).
/// </summary>
public enum CartStatus
{
    /// <summary>سبد باز است و خط می‌پذیرد.</summary>
    Active = 0,

    /// <summary>سبد به درز تبدیل سفارش رفته.</summary>
    Converted = 1,

    /// <summary>مهلت UTC سبد گذشته.</summary>
    Expired = 2,

    /// <summary>مالک سبد را رها کرده.</summary>
    Abandoned = 3,
}

/// <summary>
/// گونهٔ دسترسی به سبد. CartId به‌تنهایی مجوز نیست.
/// </summary>
public enum CartAccessKind
{
    /// <summary>مالک با هویت پایدار User.</summary>
    Authenticated = 0,

    /// <summary>مهمان با راز.</summary>
    Guest = 1,
}

/// <summary>
/// مسیر تبدیل. هر دو مدل سفارش از همین سبد شروع می‌شوند.
/// </summary>
public enum CartConversionIntent
{
    /// <summary>هنوز تبدیل نشده.</summary>
    None = 0,

    /// <summary>مبدأ سفارش Request-to-Reserve.</summary>
    RequestToReserve = 1,

    /// <summary>مبدأ خرید آنلاین.</summary>
    OnlinePurchase = 2,
}

/// <summary>
/// دسترسی به سبد. CartId به‌تنهایی Bearer نیست.
/// </summary>
public sealed record CartAccess(Guid? UserId, string? GuestSecret);

/// <summary>
/// دسترس‌پذیری کسب‌وکاری خط سبد. مفاهیم تأمین سفارش نیست.
/// </summary>
public enum CartLineAvailabilityKind
{
    /// <summary>موجودی خط را پوشش می‌دهد.</summary>
    Available = 0,

    /// <summary>موجودی مثبت است ولی کمتر از تعداد سبد.</summary>
    LimitedQuantity = 1,

    /// <summary>موجودی قابل‌فروش صفر است.</summary>
    Unavailable = 2,
}

/// <summary>
/// خط سبد برای خواندن و درز Checkout. موجودیت EF نیست.
/// </summary>
public sealed record CartLineSnapshot(
    Guid LineId,
    Guid OfferId,
    Guid CatalogVariantId,
    Guid SellerPartyId,
    decimal Quantity,
    Guid? ReservationId,
    decimal? QuotedAmount,
    string? QuotedCurrency,
    bool QuotedTaxExclusive,
    Guid? PriceId,
    DateTimeOffset QuotedAt,
    CartLineAvailabilityKind Availability = CartLineAvailabilityKind.Available,
    Guid? MerchandisingCampaignId = null);

/// <summary>
/// نمای سبد بدون نشت EF. حقیقت تسویه یا سفارش نیست.
/// <c>DefaultCurrency</c> فقط انتخاب پیش‌فرض خط تازه است، نه ارز همهٔ خطوط یا سفارش.
/// </summary>
public sealed record CartSnapshot(
    Guid CartId,
    CartStatus Status,
    CartAccessKind AccessKind,
    Guid? OwnerUserId,
    string Market,
    string DefaultCurrency,
    SalesChannel Channel,
    DateTimeOffset? ExpiresAt,
    CartConversionIntent ConversionIntent,
    int Version,
    IReadOnlyList<CartLineSnapshot> Lines);

/// <summary>
/// درز خواندن سبد برای Checkout بدون نشت EF.
/// </summary>
public interface ICartQueryGateway
{
    /// <summary>
    /// سبد را پس از احراز دسترسی برمی‌گرداند. CartId تنها کافی نیست.
    /// </summary>
    Task<CartSnapshot?> GetCartAsync(Guid cartId, CartAccess access, CancellationToken cancellationToken);
}

/// <summary>درخواست تبدیل سبد برای هماهنگی checkout.</summary>
public sealed record CartConversionRequest(
    Guid CartId,
    CartAccess Access,
    int ExpectedVersion,
    CartConversionIntent Intent,
    Guid? ProcessId,
    string CorrelationId);

/// <summary>نتیجهٔ تبدیل سبد.</summary>
public sealed record CartConversionResult(
    Guid CartId,
    int Version,
    CartStatus Status,
    CartConversionIntent ConversionIntent);

/// <summary>
/// درز پایدار تبدیل سبد برای Process Manager.
/// معنا و ماندگاری تبدیل در Cart می‌ماند؛ آمادهٔ آداپتر آینده.
/// </summary>
public interface ICartConversionPort
{
    /// <summary>سبد Active را به Converted تبدیل می‌کند.</summary>
    Task<CartConversionResult> ConvertForCheckoutAsync(
        CartConversionRequest request,
        CancellationToken cancellationToken);
}
