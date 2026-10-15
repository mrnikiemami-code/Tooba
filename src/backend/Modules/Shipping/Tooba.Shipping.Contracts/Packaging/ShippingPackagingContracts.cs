using Tooba.Geography.Contracts.Places;
using Tooba.Shipping.Contracts.Admin;
using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Shipping.Contracts.Packaging;

/// <summary>
/// چه کسی هزینهٔ حمل حامل را می‌پردازد و کِی. این یک <em>واقعیت نرخ</em> است، نه یک تراکنش
/// پرداخت: Shipping می‌گوید «این خدمت پیش‌پرداخت است یا هنگام تحویل وصول می‌شود»، و Order/Payment
/// همچنان مالک انحصاری پول، تسویه و وصول واقعی باقی می‌ماند.
/// </summary>
/// <remarks>
/// <para>
/// <b>چرا enum و نه متن فارسی.</b> مقدار ذخیره‌شده باید پایدار، مستقل از زبان و قابل مقایسه در
/// SQL باشد. برچسب فارسی یک تصمیم لایهٔ نمایش است و در دیتابیس نگه‌داری نمی‌شود.
/// </para>
/// <para>
/// <b>هر حالت یک طرف مسئول دارد.</b> هیچ مقداری بدون طرف مسئول مبلغ ذخیره نمی‌شود:
/// <see cref="Prepaid"/> یعنی مشتری هنگام تسویه، <see cref="CollectOnDelivery"/> یعنی حامل/گیرنده
/// هنگام تحویل.
/// </para>
/// </remarks>
public enum ShippingFreightPayerMode
{
    /// <summary>حمل پیش‌پرداخت است: مشتری هنگام تسویه آن را می‌پردازد (رفتار پیش‌فرض و قدیمی).</summary>
    Prepaid = 0,

    /// <summary>حمل هنگام تحویل وصول می‌شود: حامل آن را از گیرنده می‌گیرد.</summary>
    CollectOnDelivery = 1,
}

/// <summary>
/// طرف پرداخت‌کنندهٔ <em>هزینهٔ بسته‌بندی</em>، جدا از طرف پرداخت‌کنندهٔ حمل.
/// </summary>
/// <remarks>
/// <b>چرا یک مقدار.</b> بسته‌بندی در این برش فقط می‌تواند هنگام تسویه به مشتری محاسبه شود؛
/// بسته‌بندی هرگز توسط حامل وصول‌شدنی نیست، چون حامل هیچ قراردادی برای مبلغ بسته‌بندی ندارد.
/// یک enum تک‌مقداری عمداً انتخاب شده تا اگر یک Task بعدی (پس از بازبینی Architect) طرف دیگری
/// را مجاز کند، افزودن آن یک مقدار قراردادی باشد نه یک رشتهٔ آزاد.
/// </remarks>
public enum ShippingPackagingChargePayer
{
    /// <summary>هزینهٔ بسته‌بندی هنگام تسویهٔ سفارش محاسبه می‌شود.</summary>
    Checkout = 0,
}

/// <summary>بدنهٔ ایجاد/ویرایش یک سیاست هزینهٔ بسته‌بندی برای یک کد خدمت.</summary>
/// <param name="ServiceCode">کد خدمت Fulfillment؛ فقط کلید سازگاری، بدون تکرار ردیف کاتالوگ.</param>
/// <param name="Scope">دامنهٔ مقصد سیاست.</param>
/// <param name="FeeAmount">هزینهٔ ثابت بسته‌بندی؛ نباید منفی باشد. صفر یعنی «بدون هزینهٔ بسته‌بندی».</param>
/// <param name="CurrencyCode">کد ارز سه‌حرفی (مثل IRR).</param>
/// <param name="FreightPayerMode">
/// طرف پرداخت‌کنندهٔ حمل؛ تهی یعنی پیش‌فرض <see cref="ShippingFreightPayerMode.Prepaid"/> که
/// رفتار قدیمی checkout را دست‌نخورده نگه می‌دارد.
/// </param>
/// <param name="OptionCode">
/// گزینهٔ روش ارسال در صورت وجود؛ تهی یعنی سیاست در سطح خود خدمت است. این مقدار <em>هویت گزینه</em>
/// است، نه کد خدمت.
/// </param>
public sealed record ShippingPackagingPolicyWriteRequest(
    string ServiceCode,
    ShippingDestinationScopeWrite Scope,
    decimal FeeAmount,
    string CurrencyCode,
    ShippingFreightPayerMode? FreightPayerMode = null,
    string? OptionCode = null);

/// <summary>بدنهٔ تغییر وضعیت فعال/غیرفعال یک سیاست هزینهٔ بسته‌بندی.</summary>
/// <param name="IsActive">وضعیت هدف.</param>
public sealed record ShippingPackagingPolicyActiveRequest(bool IsActive);

/// <summary>یک سیاست هزینهٔ بسته‌بندی همان‌طور که ادمین می‌بیند.</summary>
/// <param name="PackagingPolicyId">شناسهٔ پایدار سیاست.</param>
/// <param name="ServiceCode">کد خدمت Fulfillment.</param>
/// <param name="CountryId">شناسهٔ کشور دامنه.</param>
/// <param name="AdministrativeAreaId">ناحیهٔ اداری دامنه؛ تهی یعنی سطح بالاتر.</param>
/// <param name="CityId">شهر دامنه؛ تهی یعنی سطح بالاتر.</param>
/// <param name="ServiceAreaId">ناحیهٔ خدمت دامنه؛ تهی یعنی سطح بالاتر.</param>
/// <param name="Level">عمیق‌ترین سطح دامنه.</param>
/// <param name="FeeAmount">هزینهٔ ثابت بسته‌بندی.</param>
/// <param name="CurrencyCode">کد ارز سه‌حرفی.</param>
/// <param name="FreightPayerMode">طرف پرداخت‌کنندهٔ حمل.</param>
/// <param name="IsActive">آیا سیاست فعال است.</param>
/// <param name="CreatedAt">زمان ایجاد UTC.</param>
/// <param name="UpdatedAt">زمان آخرین ویرایش UTC.</param>
/// <param name="OptionCode">گزینهٔ روش ارسال؛ تهی یعنی سیاست سطح‌خدمت.</param>
public sealed record ShippingPackagingPolicyAdminItem(
    Guid PackagingPolicyId,
    string ServiceCode,
    Guid CountryId,
    Guid? AdministrativeAreaId,
    Guid? CityId,
    Guid? ServiceAreaId,
    ShippingDestinationLevel Level,
    decimal FeeAmount,
    string CurrencyCode,
    ShippingFreightPayerMode FreightPayerMode,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? OptionCode = null);

/// <summary>
/// نمای کامل نویسندگی سیاست‌های بسته‌بندی یک کد خدمت. مجموعهٔ خالی عمداً صریح است: نبود سیاست
/// یعنی «هزینهٔ بسته‌بندی صفر»، نه «هزینهٔ پنهان».
/// </summary>
/// <param name="ServiceCode">کد خدمت این نما.</param>
/// <param name="PackagingPolicies">سیاست‌های بسته‌بندی این خدمت.</param>
public sealed record ShippingPackagingPoliciesView(
    string ServiceCode,
    IReadOnlyList<ShippingPackagingPolicyAdminItem> PackagingPolicies);

/// <summary>کدهای دلیل پایدار برای مؤلفهٔ هزینهٔ بسته‌بندی؛ مستقل از زبان و مناسب لاگ/تلمتری.</summary>
public static class ShippingPackagingReasonCodes
{
    /// <summary>یک سیاست فعال و یکتا برای این هویت/دامنه پیدا شد و هزینه قابل اعلام است.</summary>
    public const string Charged = "shipping.packaging.charged";

    /// <summary>هیچ سیاست بسته‌بندی‌ای برای این خدمت/گزینه/مقصد وجود ندارد؛ هزینه صفر است.</summary>
    public const string NotConfigured = "shipping.packaging.not_configured";

    /// <summary>
    /// بیش از یک سیاست هم‌سطح برای این هویت/دامنه فعال است. این ابهام یک تصمیم تجاری است و
    /// به‌جای انتخاب سطر اول صریحاً گزارش می‌شود؛ هزینه‌ای اعلام نمی‌شود.
    /// </summary>
    public const string Ambiguous = "shipping.packaging.ambiguous";
}

/// <summary>درخواست مؤلفهٔ هزینهٔ بسته‌بندی. همان کلیدهای درز نرخ: کد خدمت، مقصد و گزینهٔ اختیاری.</summary>
/// <param name="ServiceCode">کد خدمت ارسال.</param>
/// <param name="Destination">شناسه‌های پایدار مقصد.</param>
/// <param name="OptionCode">گزینهٔ درخواست‌شده؛ تهی یعنی فراخوان فقط در سطح خدمت می‌پرسد.</param>
public sealed record ShippingPackagingFeeRequest(
    string ServiceCode,
    DestinationIdentifiers Destination,
    string? OptionCode = null);

/// <summary>
/// مؤلفهٔ هزینهٔ بسته‌بندی به‌صورت یک واقعیت مستقل از حمل. این مقدار هرگز داخل
/// <see cref="ShippingQuote.ChargeAmount"/> جمع نمی‌شود: مصرف‌کننده خودش تصمیم می‌گیرد که آیا
/// مؤلفه‌ها را با هم جمع کند، و Shipping هیچ هزینهٔ اضافه‌ای را پنهانی به مشتری تحمیل نمی‌کند.
/// </summary>
/// <param name="IsCharged">آیا هزینهٔ بسته‌بندی اعلام می‌شود.</param>
/// <param name="FeeAmount">مبلغ هزینهٔ بسته‌بندی؛ در نبود سیاست صفر است.</param>
/// <param name="CurrencyCode">ارز هزینه؛ تنها وقتی هزینه اعلام می‌شود معنا دارد.</param>
/// <param name="FreightPayerMode">طرف پرداخت‌کنندهٔ حمل برای همین هویت/دامنه.</param>
/// <param name="PackagingPayer">طرف پرداخت‌کنندهٔ بسته‌بندی؛ هرگز حامل نیست.</param>
/// <param name="PackagingPolicyId">شناسهٔ سیاست برنده؛ تهی یعنی هیچ سیاستی تطبیق نیافت.</param>
/// <param name="ReasonCode">کد دلیل پایدار.</param>
public sealed record ShippingPackagingFeeComponent(
    bool IsCharged,
    decimal FeeAmount,
    string? CurrencyCode,
    ShippingFreightPayerMode FreightPayerMode,
    ShippingPackagingChargePayer PackagingPayer,
    Guid? PackagingPolicyId,
    string ReasonCode)
{
    /// <summary>مؤلفهٔ صفر برای وقتی که هیچ سیاستی تطبیق نمی‌یابد یا داده مبهم است.</summary>
    /// <param name="reasonCode">کد دلیل پایدار.</param>
    public static ShippingPackagingFeeComponent Zero(string reasonCode) => new(
        false,
        0m,
        null,
        ShippingFreightPayerMode.Prepaid,
        ShippingPackagingChargePayer.Checkout,
        null,
        reasonCode);
}

/// <summary>
/// درز فقط-خواندنی Shipping برای مؤلفهٔ هزینهٔ بسته‌بندی. فقط-خواندنی است و عمداً از درز نرخ
/// جداست تا مصرف‌کننده نتواند قاعدهٔ بسته‌بندی را با قاعدهٔ حمل قاطی کند.
/// </summary>
public interface IShippingPackagingFeeLookup
{
    /// <summary>مؤلفهٔ هزینهٔ بسته‌بندی یک درخواست را قطعی و fail-closed حل می‌کند.</summary>
    /// <param name="request">درخواست بسته‌بندی.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    /// <returns>مؤلفهٔ هزینه؛ هرگز null.</returns>
    Task<ShippingPackagingFeeComponent> ResolveAsync(
        ShippingPackagingFeeRequest request,
        CancellationToken cancellationToken);
}

/// <summary>کدهای دلیل پایدار برای نتیجهٔ ترکیبی هزینهٔ مرسوله.</summary>
public static class ShippingShipmentCostReasonCodes
{
    /// <summary>هم حمل و هم بسته‌بندی قابل اعلام بودند و مبلغ نهایی محاسبه شد.</summary>
    public const string Quoted = "shipping.shipment_cost.quoted";

    /// <summary>حمل قیمت‌گذاری نشد (پوشش/پله/ارز)، پس مبلغ نهایی قابل اعلام نیست.</summary>
    public const string FreightNotQuoted = "shipping.shipment_cost.freight_not_quoted";

    /// <summary>ارز مؤلفهٔ حمل با ارز بسته‌بندی یکسان نیست؛ جمع خودکار انجام نمی‌شود.</summary>
    public const string CurrencyMismatch = "shipping.shipment_cost.currency_mismatch";

    /// <summary>سیاست‌های بسته‌بندی روی این هویت/دامنه مبهم‌اند؛ مبلغ نهایی اعلام نمی‌شود.</summary>
    public const string PackagingAmbiguous = "shipping.shipment_cost.packaging_ambiguous";
}

/// <summary>درخواست نمای ترکیبی هزینهٔ مرسوله؛ همان ورودی نرخ به‌علاوهٔ هیچ چیز تازه‌ای.</summary>
/// <param name="ServiceCode">کد خدمت ارسال.</param>
/// <param name="Destination">شناسه‌های پایدار مقصد.</param>
/// <param name="CartSubtotalExclusive">جمع سبد بدون هزینهٔ ارسال؛ برای آستانهٔ ارسال رایگان.</param>
/// <param name="CurrencyCode">ارز درخواست؛ تهی یعنی بررسی ارز انجام نشود.</param>
/// <param name="OptionCode">گزینهٔ روش ارسال؛ تهی یعنی در سطح خدمت.</param>
/// <param name="Parcel">اندازهٔ فیزیکی مرسوله؛ تهی یعنی اعلام‌نشده.</param>
public sealed record ShippingShipmentCostRequest(
    string ServiceCode,
    DestinationIdentifiers Destination,
    decimal CartSubtotalExclusive,
    string? CurrencyCode = null,
    string? OptionCode = null,
    ShippingMeasuredParcel? Parcel = null);

/// <summary>
/// نمای فقط-خواندنی و <em>آینده‌نگر</em> هزینهٔ مرسوله: مؤلفهٔ حمل، مؤلفهٔ بسته‌بندی، ارز و مبلغ
/// نهایی. این قرارداد امروز هیچ مسیر Checkout را تغییر نمی‌دهد و هیچ مصرف‌کنندهٔ زنده‌ای ندارد؛
/// هدفش این است که orchestration بعدی مجبور نشود قاعدهٔ نرخ یا بسته‌بندی را دوباره پیاده کند.
/// </summary>
/// <param name="CarrierFreight">نتیجهٔ قطعی موتور نرخ موجود؛ دست‌نخورده و بدون بازتفسیر.</param>
/// <param name="Packaging">مؤلفهٔ مستقل هزینهٔ بسته‌بندی.</param>
/// <param name="CurrencyCode">ارز مبلغ نهایی؛ تهی یعنی مبلغ نهایی اعلام نمی‌شود.</param>
/// <param name="CombinedAmount">
/// جمع حمل و بسته‌بندی؛ تهی یعنی جمع قابل اعلام نیست (حمل قیمت‌گذاری نشد، ارزها ناسازگارند، یا
/// بسته‌بندی مبهم است). هرگز به‌صورت خوش‌بینانه با یک مؤلفهٔ غایب جمع نمی‌شود.
/// </param>
/// <param name="ReasonCode">کد دلیل پایدار.</param>
public sealed record ShippingShipmentCost(
    ShippingQuote CarrierFreight,
    ShippingPackagingFeeComponent Packaging,
    string? CurrencyCode,
    decimal? CombinedAmount,
    string ReasonCode)
{
    /// <summary>آیا مبلغ نهایی قابل اعلام است.</summary>
    public bool IsCombined => CombinedAmount is not null;
}

/// <summary>
/// درز فقط-خواندنی Shipping برای نمای ترکیبی هزینهٔ مرسوله. هیچ نوشتنی، هیچ تراکنش و هیچ
/// قاعدهٔ تجاری تازه‌ای اضافه نمی‌کند: فقط دو مؤلفهٔ موجود را کنار هم می‌گذارد.
/// </summary>
public interface IShippingShipmentCostLookup
{
    /// <summary>هزینهٔ مرسوله را به‌صورت مؤلفه‌های جدا و مبلغ نهایی حل می‌کند.</summary>
    /// <param name="request">درخواست هزینهٔ مرسوله.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    /// <returns>نمای هزینه؛ هرگز null.</returns>
    Task<ShippingShipmentCost> ResolveAsync(
        ShippingShipmentCostRequest request,
        CancellationToken cancellationToken);
}
