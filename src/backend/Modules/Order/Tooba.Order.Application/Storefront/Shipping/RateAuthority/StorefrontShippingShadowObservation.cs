using Tooba.Geography.Contracts.Places;

namespace Tooba.Order.Application.Storefront.Shipping.RateAuthority;

/// <summary>
/// یک نامزد مشاهدهٔ سایه: دقیقاً همان چیزی که مسیر قدیمی امروز به مشتری پیشنهاد می‌دهد.
/// </summary>
/// <param name="ServiceCode">کد خدمت ارسال (کلید موجود Fulfillment؛ همان چیزی که به مرجع جدید داده می‌شود).</param>
/// <param name="LegacyPriceAmount">مبلغ authoritative مسیر قدیمی؛ تنها مرجع قیمت و مبنای مقایسه.</param>
public sealed record StorefrontShippingShadowCandidate(
    string ServiceCode,
    decimal LegacyPriceAmount);

/// <summary>
/// واقعیت‌های مسیر واقعی فروشگاه که برای مشاهدهٔ سایه لازم است. همهٔ این‌ها از خروجی همان
/// projection قدیمی گرفته می‌شوند؛ هیچ مقدار مشتق‌شدهٔ جدیدی ساخته نمی‌شود.
/// </summary>
/// <param name="ProvinceName">تنها ورودی مقصدی که مسیر فروشگاه امروز می‌پذیرد (کد یا برچسب استان).</param>
/// <param name="CartSubtotalExclusive">جمع سبد بدون هزینهٔ ارسال (از همان projection).</param>
/// <param name="CurrencyCode">ارز سبد (از همان projection).</param>
/// <param name="Candidates">گزینه‌های ارسالی که مسیر قدیمی مجاز کرده است.</param>
public sealed record StorefrontShippingShadowObservationInput(
    string? ProvinceName,
    decimal CartSubtotalExclusive,
    string? CurrencyCode,
    IReadOnlyList<StorefrontShippingShadowCandidate> Candidates)
{
    /// <summary>
    /// شهر انتخاب‌شدهٔ مشتری در صورت وجود (نام یا کد). همراه استان، Geography را به دقت «شهر» می‌رساند.
    /// اختیاری و سازگار با مشتریان قدیمی که فقط استان می‌فرستند؛ خودش هویت نمی‌سازد.
    /// </summary>
    public string? CityName { get; init; }

    /// <summary>
    /// شناسه‌های پایدار Geography وقتی فراخوان واقعاً آن‌ها را در اختیار دارد. این مقدار پیش از استفاده
    /// با <see cref="Tooba.Geography.Contracts.Places.IGeographyLookup.GetByIdsAsync"/> اعتبارسنجی می‌شود و
    /// در صورت نامعتبر بودن، مشاهده «غیرقابل‌مقایسه» می‌ماند؛ هرگز شناسهٔ حدسی جایگزین نمی‌شود.
    /// </summary>
    public DestinationIdentifiers? Destination { get; init; }
}

/// <summary>
/// نتیجهٔ مشاهدهٔ سایه به‌صورت یک کد پایدار و کم‌کاردینال.
/// </summary>
/// <remarks>
/// این کد عمداً هیچ دادهٔ شخصی/آدرسی یا متن خام مشتری را حمل نمی‌کند و برای لاگ/تلمتری امن است.
/// «عدم امکان مقایسه» هرگز به «بدون محدودیت» یا «مجاز» ترجمه نمی‌شود.
/// </remarks>
public enum StorefrontShippingShadowObservationCode
{
    /// <summary>دروازه خاموش است؛ هیچ مقایسه و هیچ پرس‌وجوی اضافه‌ای انجام نشد.</summary>
    Disabled = 0,

    /// <summary>سایه روشن است اما پیش‌نیازهای واقعی تأیید نشده‌اند؛ مقایسه انجام نشد.</summary>
    PrerequisitesUnconfirmed = 1,

    /// <summary>درز مرجع جدید یا حل‌کنندهٔ مقصد در ترکیب موجود نیست؛ مقایسه ممکن نیست.</summary>
    ShadowUnavailable = 2,

    /// <summary>مقصد قدیمی به هویت پایدار یکتا نگاشت نشد (غایب/مبهم/نامنگاشت)؛ حدس زده نشد.</summary>
    DestinationNotComparable = 3,

    /// <summary>مقایسه انجام شد؛ جزئیات هر نامزد در گزارش‌های همراه است.</summary>
    Observed = 4,
}

/// <summary>
/// نتیجهٔ مشاهده: یک کد پایدار برای ثبت، و گزارش‌های فقط-مشاهده‌ای هر نامزد.
/// </summary>
/// <param name="Code">کد نتیجهٔ مشاهده.</param>
/// <param name="Reports">گزارش مقایسهٔ هر نامزد (ممکن است خالی باشد).</param>
public sealed record StorefrontShippingShadowObservationResult(
    StorefrontShippingShadowObservationCode Code,
    IReadOnlyList<StorefrontShippingRateShadowReport> Reports);
