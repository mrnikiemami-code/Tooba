using Tooba.Geography.Contracts.Places;
using Tooba.Order.Application.Storefront.Shipping.RateAuthority;

namespace Tooba.Order.Application.Storefront.Shipping.OperationalShadow;

/// <summary>
/// یک نامزد مشاهدهٔ عملیاتی: همان چیزی که مسیر قدیمی امروز به مشتری پیشنهاد می‌دهد، به‌همراه
/// هویت خدمت/گزینه که به مرجع عملیاتی Shipping داده می‌شود.
/// </summary>
/// <param name="ServiceCode">کد خدمت ارسال (کلید Fulfillment؛ همان چیزی که به مرجع جدید داده می‌شود).</param>
/// <param name="OptionCode">
/// گزینهٔ روش ارسال در صورت وجود. این هویت <em>جدا</em> از کد خدمت حمل می‌شود تا
/// «tipax:express» هرگز به‌عنوان کد خدمت خوانده نشود.
/// </param>
/// <param name="LegacyPriceAmount">مبلغ authoritative مسیر قدیمی؛ تنها مرجع قیمت مشتری و مبنای مقایسه.</param>
/// <param name="LegacyLeadDays">مهلت اعلامی مسیر قدیمی؛ فقط برای مقایسه و هرگز جایگزین نمی‌شود.</param>
public sealed record StorefrontShippingOperationalShadowCandidate(
    string ServiceCode,
    string? OptionCode,
    decimal LegacyPriceAmount,
    int LegacyLeadDays);

/// <summary>
/// واقعیت‌های مسیر واقعی فروشگاه که برای مشاهدهٔ عملیاتی لازم است.
/// </summary>
/// <remarks>
/// <para>
/// <b>هیچ اندازه‌ای از سبد ساخته نمی‌شود.</b> <see cref="Parcel"/> فقط وقتی پر می‌شود که فراخوان
/// یک اندازهٔ فیزیکی اندازه‌گیری‌شدهٔ واقعی داشته باشد؛ نبود آن به
/// <see cref="StorefrontShippingOperationalShadowCode.MeasurementUnavailable"/> می‌رسد و مقایسهٔ
/// پله‌ای انجام <em>نمی‌شود</em>. جمع سبد، تعداد خطوط یا تعداد کالا هرگز به وزن تبدیل نمی‌شوند.
/// </para>
/// <para>
/// <b>هیچ وعدهٔ تحویلی ساخته نمی‌شود.</b> <see cref="RequestedPickupAtUtc"/> فقط «لحظهٔ درخواستی
/// تحویل به حامل» است و از ساعت سیستم خوانده نمی‌شود. بازهٔ حامل هرگز به «تحویل در این بازه»
/// ترجمه نمی‌شود؛ مالک وعدهٔ تحویل مشتری Order است.
/// </para>
/// </remarks>
/// <param name="ProvinceName">تنها ورودی مقصدی مسیر فروشگاه امروز (کد یا برچسب استان).</param>
/// <param name="CartSubtotalExclusive">جمع سبد بدون هزینهٔ ارسال (از همان projection).</param>
/// <param name="CurrencyCode">ارز سبد (از همان projection).</param>
/// <param name="Candidates">روش‌های ارسالی که مسیر قدیمی مجاز کرده است.</param>
public sealed record StorefrontShippingOperationalShadowInput(
    string? ProvinceName,
    decimal CartSubtotalExclusive,
    string? CurrencyCode,
    IReadOnlyList<StorefrontShippingOperationalShadowCandidate> Candidates)
{
    /// <summary>شهر انتخاب‌شده در صورت وجود؛ دقت هویت مقصد را به شهر می‌رساند و هویت نمی‌سازد.</summary>
    public string? CityName { get; init; }

    /// <summary>
    /// شناسه‌های پایدار Geography وقتی فراخوان واقعاً آن‌ها را دارد؛ پیش از مصرف اعتبارسنجی می‌شوند.
    /// </summary>
    public DestinationIdentifiers? Destination { get; init; }

    /// <summary>
    /// اندازهٔ فیزیکی <em>واقعی</em> مرسوله در صورت وجود. تهی یعنی اندازه در این مسیر قابل
    /// استخراج نیست و پله‌های شرطی نباید مقایسه شوند.
    /// </summary>
    public Tooba.Shipping.Contracts.Rates.ShippingMeasuredParcel? Parcel { get; init; }

    /// <summary>
    /// منبع اندازهٔ اعلام‌شده، فقط برای ردیابی. تهی یعنی هیچ اندازهٔ معتبری وجود ندارد؛ یک
    /// اندازه بدون منبع معتبر هرگز به‌عنوان ورودی تجاری پذیرفته نمی‌شود.
    /// </summary>
    public string? ParcelProvenance { get; init; }

    /// <summary>لحظهٔ درخواستی تحویل به حامل به UTC؛ تهی یعنی ارزیابی زمانی انجام نشود.</summary>
    public DateTimeOffset? RequestedPickupAtUtc { get; init; }
}

/// <summary>
/// کد پایدار نتیجهٔ مشاهدهٔ عملیاتی. عمداً کم‌کاردینال و بدون هیچ دادهٔ شخصی/آدرسی.
/// </summary>
public enum StorefrontShippingOperationalShadowCode
{
    /// <summary>دروازه خاموش است؛ هیچ پرس‌وجو و هیچ لاگی انجام نشد.</summary>
    Disabled = 0,

    /// <summary>سایه روشن است اما پیش‌نیازها تأیید نشده‌اند؛ مشاهده انجام نشد.</summary>
    PrerequisitesUnconfirmed = 1,

    /// <summary>درز مرجع عملیاتی یا حل‌کنندهٔ مقصد در ترکیب موجود نیست؛ مشاهده ممکن نیست.</summary>
    ShadowUnavailable = 2,

    /// <summary>مقصد قدیمی به هویت پایدار یکتا نگاشت نشد (غایب/مبهم/نامنگاشت)؛ حدس زده نشد.</summary>
    DestinationNotComparable = 3,

    /// <summary>
    /// اندازهٔ فیزیکی واقعی مرسوله در این مسیر در دسترس نیست. مقایسهٔ پله‌ای <b>انجام نشد</b> و
    /// هیچ وزنی از سبد ساخته نشد؛ این حالت هرگز «تطابق» یا «بدون محدودیت» نیست.
    /// </summary>
    MeasurementUnavailable = 4,

    /// <summary>مشاهده انجام شد؛ جزئیات هر نامزد در گزارش‌های همراه است.</summary>
    Observed = 5,
}

/// <summary>
/// گزارش مشاهدهٔ عملیاتی یک نامزد: همهٔ اجزای مرجع جدید، فقط برای مشاهده.
/// </summary>
/// <remarks>
/// هیچ‌کدام از این مقادیر قیمت مشتری، وعدهٔ تحویل یا snapshot سفارش را تعیین نمی‌کند. مبالغ
/// تهی (<c>null</c>) یعنی «اعلام‌نشده» و هرگز به صفر تبدیل نمی‌شوند.
/// </remarks>
/// <param name="ServiceCode">کد خدمت authoritative که به مرجع داده شد.</param>
/// <param name="OptionCode">گزینهٔ روش ارسال؛ فقط برای ردیابی.</param>
/// <param name="LegacyPriceAmount">مبلغ authoritative مسیر قدیمی؛ همان چیزی که به مشتری می‌رسد.</param>
/// <param name="LegacyLeadDays">مهلت اعلامی مسیر قدیمی.</param>
/// <param name="Gate">وضعیت دروازه.</param>
/// <param name="Outcome">نتیجهٔ مقایسه.</param>
/// <param name="LegacyFreightAmount">مؤلفهٔ حمل مرجع جدید؛ تهی یعنی اعلام‌نشده.</param>
/// <param name="PackagingAmount">مؤلفهٔ بسته‌بندی مرجع جدید؛ تهی یعنی اعلام‌نشده. صفر یک مقدار واقعی است.</param>
/// <param name="TotalServiceExpense">جمع حمل و بسته‌بندی مرجع جدید؛ تهی یعنی اعلام‌نشده.</param>
/// <param name="AmountPayableAtCheckout">بخشی که هنگام تسویه پرداخت می‌شود.</param>
/// <param name="AmountCollectOnDelivery">بخشی که هنگام تحویل وصول می‌شود.</param>
/// <param name="FreightPayerMode">طرف پرداخت‌کنندهٔ حمل (واقعیت نرخ، نه تراکنش).</param>
/// <param name="PackagingPayer">طرف پرداخت‌کنندهٔ بسته‌بندی؛ هرگز حامل نیست.</param>
/// <param name="PackagingReasonCode">کد دلیل پایدار مؤلفهٔ بسته‌بندی.</param>
/// <param name="ScheduleAvailability">وضعیت بازهٔ حامل؛ تهی یعنی هیچ برنامه‌ای ارزیابی نشد.</param>
/// <param name="ScheduleReasonCode">کد دلیل پایدار ارزیابی بازه.</param>
/// <param name="CapacityEnforcement">وضعیت اجرای ظرفیت؛ در این برش همیشه «اجرا نمی‌شود».</param>
/// <param name="ConfiguredCapacityTotal">ظرفیت پیکربندی‌شده؛ هرگز به‌عنوان «باقی‌مانده» خوانده نشود.</param>
/// <param name="ReasonCode">کد دلیل پایدار نتیجهٔ قیمت‌گذاری عملیاتی.</param>
/// <param name="CarrierReasonCode">کد دلیل پایدار مؤلفهٔ حمل؛ تهی یعنی نرخی اعلام نشد.</param>
public sealed record StorefrontShippingOperationalShadowReport(
    string ServiceCode,
    string? OptionCode,
    decimal LegacyPriceAmount,
    int LegacyLeadDays,
    StorefrontShippingRateAuthorityGate Gate,
    StorefrontShippingOperationalShadowOutcome Outcome,
    decimal? LegacyFreightAmount,
    decimal? PackagingAmount,
    decimal? TotalServiceExpense,
    decimal? AmountPayableAtCheckout,
    decimal? AmountCollectOnDelivery,
    string? FreightPayerMode,
    string? PackagingPayer,
    string? PackagingReasonCode,
    string? ScheduleAvailability,
    string? ScheduleReasonCode,
    string? CapacityEnforcement,
    int? ConfiguredCapacityTotal,
    string? ReasonCode,
    string? CarrierReasonCode = null);

/// <summary>نتیجهٔ مقایسهٔ سایهٔ عملیاتی. هیچ‌کدام قیمت مشتری را تعیین نمی‌کند.</summary>
public enum StorefrontShippingOperationalShadowOutcome
{
    /// <summary>دروازه اجازه نداد یا داده کافی نبود؛ مقایسه‌ای انجام نشد.</summary>
    NotEvaluated = 0,

    /// <summary>هویت پایدار مقصد در دسترس نیست، پس مقایسه بی‌معنا است.</summary>
    NotComparable = 1,

    /// <summary>مرجع جدید هم همان مبلغ مؤثر و هم همان مجازبودن را می‌گوید.</summary>
    Match = 2,

    /// <summary>مسیر قدیمی مجاز می‌داند اما مرجع جدید پوشش/قیمت نمی‌دهد.</summary>
    LegacyAllowedShippingDenied = 3,

    /// <summary>هر دو مجاز می‌دانند اما مبلغ مؤثر یکسان نیست.</summary>
    AmountMismatch = 4,

    /// <summary>
    /// مبلغ مؤثر یکسان است اما مرجع جدید یک هزینهٔ بسته‌بندی جدا اعلام می‌کند. این یک اختلاف
    /// تجاری واقعی است که <b>نباید</b> خودکار داخل مبلغ Checkout برود.
    /// </summary>
    PackagingNotReflected = 5,

    /// <summary>مرجع عملیاتی در دسترس نبود (خطا، نبود جدول، نبود داده).</summary>
    ShippingUnavailable = 6,

    /// <summary>
    /// اندازهٔ واقعی مرسوله در دسترس نبود، پس ارزیابی پله‌ای انجام نشد. یک قاعدهٔ شرط‌دار
    /// fail-closed قیمت نمی‌دهد؛ این حالت هرگز «تطابق» نیست.
    /// </summary>
    MeasurementUnavailable = 7,
}

/// <summary>نتیجهٔ مشاهده: یک کد پایدار برای ثبت، و گزارش‌های فقط-مشاهده‌ای هر نامزد.</summary>
/// <param name="Code">کد نتیجهٔ مشاهده.</param>
/// <param name="Reports">گزارش مقایسهٔ هر نامزد (ممکن است خالی باشد).</param>
public sealed record StorefrontShippingOperationalShadowResult(
    StorefrontShippingOperationalShadowCode Code,
    IReadOnlyList<StorefrontShippingOperationalShadowReport> Reports);
