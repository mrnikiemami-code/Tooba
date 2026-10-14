using Tooba.Geography.Contracts.Places;

namespace Tooba.Order.Application.Storefront.Shipping.RateAuthority;

/// <summary>
/// ورودی یک ارزیابی سایه: واقعیت‌های مسیر قدیمی (قیمت و مجازبودن) به‌همراه هویت مقصد در صورت وجود.
/// </summary>
/// <param name="MethodCode">کد متد فروشگاهی (همان کد خدمت/گزینهٔ موجود).</param>
/// <param name="LegacyPriceAmount">قیمتی که مسیر قدیمی امروز به مشتری می‌دهد؛ مبنای مقایسه و مرجع نهایی.</param>
/// <param name="LegacyDestinationAllowed">آیا مسیر قدیمی این مقصد را مجاز می‌داند.</param>
/// <param name="Destination">هویت پایدار مقصد؛ تهی یعنی امروز قابل استخراج نیست (مرحلهٔ ۲ ADR-036 اجرا نشده).</param>
/// <param name="CartSubtotalExclusive">جمع سبد بدون هزینهٔ ارسال.</param>
/// <param name="CurrencyCode">ارز سبد در صورت وجود.</param>
public sealed record StorefrontShippingRateShadowInput(
    string MethodCode,
    decimal LegacyPriceAmount,
    bool LegacyDestinationAllowed,
    DestinationIdentifiers? Destination,
    decimal CartSubtotalExclusive,
    string? CurrencyCode);

/// <summary>نتیجهٔ مقایسهٔ سایه. هیچ‌کدام از این مقادیر قیمت مشتری را تعیین نمی‌کند.</summary>
public enum StorefrontShippingRateShadowOutcome
{
    /// <summary>دروازه اجازه نداد؛ مقایسه‌ای انجام نشد.</summary>
    NotEvaluated = 0,

    /// <summary>هویت پایدار مقصد در دسترس نیست، پس مقایسه بی‌معنا است (نه «موفق»، نه «شکست»).</summary>
    NotComparable = 1,

    /// <summary>مرجع جدید هم همان قیمت و هم همان مجازبودن را می‌گوید.</summary>
    Match = 2,

    /// <summary>مسیر قدیمی مجاز می‌داند اما مرجع جدید پوشش‌نشده می‌گوید (تا زمان seed مجوزها محتمل است).</summary>
    LegacyAllowedShippingDenied = 3,

    /// <summary>مرجع جدید مجاز می‌داند اما مسیر قدیمی نه؛ یک اختلاف پوشش که باید بررسی شود.</summary>
    ShippingAllowedLegacyDenied = 4,

    /// <summary>هر دو مجاز می‌دانند اما مبلغ یکسان نیست.</summary>
    AmountMismatch = 5,

    /// <summary>مرجع جدید در دسترس نبود (خطا، نبود جدول، نبود داده).</summary>
    ShippingUnavailable = 6,
}

/// <summary>
/// گزارش سایه: فقط برای مشاهده/تلمتری. این شیء هیچ‌گاه به قیمت، پیش‌نویس یا snapshot سفارش راه نمی‌یابد.
/// </summary>
/// <param name="MethodCode">کد متد.</param>
/// <param name="Gate">وضعیت دروازه.</param>
/// <param name="Outcome">نتیجهٔ مقایسه.</param>
/// <param name="LegacyPriceAmount">قیمت مرجع مسیر قدیمی (همان چیزی که به مشتری داده می‌شود).</param>
/// <param name="ShippingChargeAmount">مبلغ اعلامی مرجع جدید؛ فقط مشاهده‌ای.</param>
/// <param name="ShippingOutcome">وضعیت خدمت‌رسانی مرجع جدید؛ فقط مشاهده‌ای.</param>
/// <param name="ShippingReasonCode">کد دلیل پایدار مرجع جدید؛ فقط مشاهده‌ای.</param>
public sealed record StorefrontShippingRateShadowReport(
    string MethodCode,
    StorefrontShippingRateAuthorityGate Gate,
    StorefrontShippingRateShadowOutcome Outcome,
    decimal LegacyPriceAmount,
    decimal? ShippingChargeAmount,
    string? ShippingOutcome,
    string? ShippingReasonCode);
