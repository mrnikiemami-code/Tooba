using Tooba.Geography.Contracts.Places;
using Tooba.Shipping.Contracts.Admin;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Shipping.Contracts.Operations;

/// <summary>
/// نتیجهٔ <em>قیمت‌گذاری</em> یک ارزیابی عملیاتی. این enum فقط دربارهٔ پول است؛ در دسترس بودن
/// بازهٔ حامل یک واقعیت جدا و در <see cref="ShippingScheduleEvaluation"/> گزارش می‌شود.
/// </summary>
public enum ShippingOperationalQuoteOutcome
{
    /// <summary>هم حمل و هم بسته‌بندی قابل اعلام بودند و مبالغ قطعی‌اند.</summary>
    Quoted = 0,

    /// <summary>حمل قیمت‌گذاری نشد (پوشش/پلهٔ اندازه/ارز/خدمت ناشناخته)؛ هیچ مبلغی اعلام نمی‌شود.</summary>
    FreightNotQuoted = 1,

    /// <summary>سیاست‌های بسته‌بندی روی این هویت/دامنه مبهم‌اند؛ هیچ مبلغی اعلام نمی‌شود.</summary>
    PackagingAmbiguous = 2,

    /// <summary>ارز مؤلفهٔ حمل با ارز بسته‌بندی یکسان نیست؛ تبدیل/جمع خودکار انجام نمی‌شود.</summary>
    CurrencyMismatch = 3,
}

/// <summary>
/// نتیجهٔ ارزیابی در دسترس بودن بازهٔ تحویل به حامل برای یک لحظهٔ درخواستی.
/// </summary>
/// <remarks>
/// <b>این وعدهٔ تحویل به مشتری نیست.</b> مالک وعده Order است؛ Shipping فقط می‌گوید حامل در این
/// بازهٔ محلی می‌تواند مرسوله را بپذیرد.
/// </remarks>
public enum ShippingScheduleAvailability
{
    /// <summary>هیچ برنامهٔ عملیاتی برای این هویت (خدمت/گزینه) ثبت نشده است؛ هیچ محدودیتی اعمال نمی‌شود.</summary>
    NotConfigured = 0,

    /// <summary>
    /// برنامهٔ ثبت‌شده غیرفعال است. معنای امن و مستند آن: «غیرفعال» یک محدودیت عملیاتی
    /// <em>نیست</em> و هرگز به «در دسترس نیست» تعبیر نمی‌شود.
    /// </summary>
    InactiveNotEnforced = 1,

    /// <summary>برنامهٔ فعال وجود دارد و لحظهٔ درخواستی داخل یکی از پنجره‌های همان روز هفته است.</summary>
    Available = 2,

    /// <summary>برنامهٔ فعال وجود دارد و لحظهٔ درخواستی خارج از همهٔ پنجره‌های همان روز هفته است.</summary>
    Unavailable = 3,

    /// <summary>
    /// هیچ لحظهٔ درخواستی اعلام نشده است، پس دربارهٔ پنجره‌ها هیچ ادعایی نمی‌شود. این حالت هرگز
    /// به «در دسترس» تعبیر نمی‌شود.
    /// </summary>
    NotEvaluated = 4,

    /// <summary>
    /// بیش از یک برنامهٔ فعال هم‌سطح برای همین هویت وجود دارد. این ابهام یک نقص داده است و به‌جای
    /// انتخاب سطر اول صریحاً گزارش می‌شود؛ هیچ در دسترس بودنی ادعا نمی‌شود.
    /// </summary>
    Ambiguous = 5,
}

/// <summary>
/// اجرای ظرفیت پنجره‌های عملیاتی. عمداً فقط یک مقدار دارد: در این برش هیچ رزرو/شمارش اتمی وجود
/// ندارد، پس هیچ مقداری نمی‌تواند «اجراشده» باشد و ظرفیت فقط یک پیکربندی است.
/// </summary>
public enum ShippingCapacityEnforcement
{
    /// <summary>ظرفیت پیکربندی‌شده اجرا <b>نمی‌شود</b>؛ عدد ظرفیت هرگز به‌عنوان «باقی‌مانده» خوانده نشود.</summary>
    NotEnforced = 0,
}

/// <summary>کدهای دلیل پایدار برای نتیجهٔ ارزیابی عملیاتی.</summary>
public static class ShippingOperationalQuoteReasonCodes
{
    /// <summary>مبالغ حمل و بسته‌بندی قطعی‌اند و تفکیک تسویه/وصول محاسبه شد.</summary>
    public const string Quoted = "shipping.operational_quote.quoted";

    /// <summary>حمل قیمت‌گذاری نشد؛ هیچ مبلغی اعلام نمی‌شود (کد دلیل دقیق در مؤلفهٔ حمل است).</summary>
    public const string FreightNotQuoted = "shipping.operational_quote.freight_not_quoted";

    /// <summary>سیاست‌های بسته‌بندی مبهم‌اند؛ هیچ مبلغی اعلام نمی‌شود.</summary>
    public const string PackagingAmbiguous = "shipping.operational_quote.packaging_ambiguous";

    /// <summary>ارز دو مؤلفه یکسان نیست؛ هیچ جمعی اعلام نمی‌شود.</summary>
    public const string CurrencyMismatch = "shipping.operational_quote.currency_mismatch";
}

/// <summary>کدهای دلیل پایدار برای ارزیابی برنامهٔ عملیاتی.</summary>
public static class ShippingScheduleEvaluationReasonCodes
{
    /// <summary>هیچ برنامه‌ای برای این هویت ثبت نشده است.</summary>
    public const string NotConfigured = "shipping.schedule.not_configured";

    /// <summary>برنامهٔ ثبت‌شده غیرفعال است و به‌عنوان محدودیت اعمال نمی‌شود.</summary>
    public const string InactiveNotEnforced = "shipping.schedule.inactive_not_enforced";

    /// <summary>لحظهٔ درخواستی داخل یک پنجرهٔ فعال است.</summary>
    public const string Available = "shipping.schedule.available";

    /// <summary>لحظهٔ درخواستی خارج از همهٔ پنجره‌های فعال همان روز هفته است.</summary>
    public const string OutsideWindows = "shipping.schedule.outside_windows";

    /// <summary>لحظهٔ درخواستی اعلام نشده است؛ ارزیابی انجام نشد.</summary>
    public const string NotEvaluated = "shipping.schedule.not_evaluated";

    /// <summary>بیش از یک برنامهٔ فعال هم‌سطح برای این هویت وجود دارد؛ هیچ ادعای در دسترس بودنی نمی‌شود.</summary>
    public const string Ambiguous = "shipping.schedule.ambiguous";

    /// <summary>
    /// شناسهٔ منطقهٔ زمانی ذخیره‌شده روی این runtime قابل حل نیست. چنین داده‌ای یک نقص داده است،
    /// پس ارزیابی fail-closed می‌شود و «در دسترس» ادعا نمی‌شود.
    /// </summary>
    public const string TimeZoneUnresolvable = "shipping.schedule.timezone_unresolvable";

    /// <summary>ظرفیت پیکربندی‌شده اجرا نمی‌شود؛ عدد ظرفیت یک واقعیت اجرایی نیست.</summary>
    public const string CapacityNotEnforced = "shipping.capacity.not_enforced";
}

/// <summary>
/// نتیجهٔ ارزیابی در دسترس بودن بازهٔ حامل برای یک لحظهٔ درخواستی، همراه با شفافیت کامل دربارهٔ
/// ظرفیت پیکربندی‌شده.
/// </summary>
/// <param name="Availability">وضعیت در دسترس بودن.</param>
/// <param name="ReasonCode">کد دلیل پایدار.</param>
/// <param name="TimeZoneId">منطقهٔ زمانی برنامهٔ ارزیابی‌شده؛ تهی یعنی هیچ برنامه‌ای ارزیابی نشد.</param>
/// <param name="LocalWeekday">روز هفتهٔ ISO محلی لحظهٔ درخواستی؛ تهی یعنی ارزیابی انجام نشد.</param>
/// <param name="LocalTime">ساعت محلی لحظهٔ درخواستی؛ تهی یعنی ارزیابی انجام نشد.</param>
/// <param name="MatchedWindowId">شناسهٔ پنجرهٔ منطبق؛ تهی یعنی هیچ پنجره‌ای منطبق نشد.</param>
/// <param name="ConfiguredCapacityTotal">
/// مجموع ظرفیت پیکربندی‌شده؛ تهی یعنی هیچ پنجره‌ای ظرفیت ندارد. این عدد ظرفیت <em>اجراشده</em>
/// نیست و هرگز نباید به‌عنوان ظرفیت باقی‌مانده خوانده شود.
/// </param>
/// <param name="CapacityEnforcement">وضعیت اجرای ظرفیت؛ در این برش همیشه «اجرا نمی‌شود».</param>
public sealed record ShippingScheduleEvaluation(
    ShippingScheduleAvailability Availability,
    string ReasonCode,
    string? TimeZoneId = null,
    int? LocalWeekday = null,
    TimeOnly? LocalTime = null,
    Guid? MatchedWindowId = null,
    int? ConfiguredCapacityTotal = null,
    ShippingCapacityEnforcement CapacityEnforcement = ShippingCapacityEnforcement.NotEnforced)
{
    /// <summary>
    /// آیا این ارزیابی هیچ محدودیتی را نقض نمی‌کند. نبود برنامه و برنامهٔ غیرفعال هر دو
    /// fail-open هستند تا رفتار قدیمی دست‌نخورده بماند؛ فقط پنجرهٔ فعالِ غیرمنطبق محدودیت است.
    /// </summary>
    public bool IsConstraintSatisfied => Availability is ShippingScheduleAvailability.Available
        or ShippingScheduleAvailability.NotConfigured
        or ShippingScheduleAvailability.InactiveNotEnforced;
}

/// <summary>
/// درخواست ارزیابی عملیاتی Shipping: همهٔ ورودی‌های واقعی و صریح، بدون هیچ خواندن ضمنی از
/// «الان» یا حدس اندازه از سبد.
/// </summary>
/// <param name="ServiceCode">کد خدمت ارسال؛ همان کلیدهای موجود Fulfillment.</param>
/// <param name="Destination">شناسه‌های پایدار مقصد.</param>
/// <param name="CartSubtotalExclusive">جمع سبد بدون هزینهٔ ارسال؛ فقط برای آستانهٔ ارسال رایگان.</param>
/// <param name="CurrencyCode">ارز درخواست؛ تهی یعنی بررسی ارز انجام نشود.</param>
/// <param name="OptionCode">گزینهٔ روش ارسال؛ تهی یعنی فقط در سطح خدمت.</param>
/// <param name="Parcel">اندازهٔ فیزیکی اندازه‌گیری‌شدهٔ مرسوله؛ تهی یعنی اعلام‌نشده.</param>
/// <param name="RequestedPickupAtUtc">
/// لحظهٔ درخواستی تحویل مرسوله به حامل به‌صورت UTC صریح؛ تهی یعنی ارزیابی زمانی انجام نشود.
/// این مقدار هرگز از ساعت سیستم خوانده نمی‌شود تا نتیجه قابل بازتولید باشد.
/// </param>
public sealed record ShippingOperationalQuoteRequest(
    string ServiceCode,
    DestinationIdentifiers Destination,
    decimal CartSubtotalExclusive = 0m,
    string? CurrencyCode = null,
    string? OptionCode = null,
    ShippingMeasuredParcel? Parcel = null,
    DateTimeOffset? RequestedPickupAtUtc = null);

/// <summary>
/// نتیجهٔ کامل و قابل‌حسابرسی ارزیابی عملیاتی: مؤلفه‌های جدا، تفکیک «پرداخت هنگام تسویه» از
/// «وصول هنگام تحویل»، وضعیت بازهٔ حامل و وضعیت صادقانهٔ ظرفیت.
/// </summary>
/// <remarks>
/// <para>
/// <b>هیچ‌گاه صفرِ موفق‌نما.</b> وقتی حمل قیمت‌گذاری نشود، بسته‌بندی مبهم باشد، یا ارزها ناسازگار
/// باشند، همهٔ مبالغ <c>null</c> می‌مانند و فقط کد دلیل پایدار برگردانده می‌شود.
/// </para>
/// <para>
/// <b>تفکیک پرداخت.</b> <see cref="ShippingFreightPayerMode.Prepaid"/> یعنی حمل هنگام تسویه
/// پرداخت می‌شود؛ <see cref="ShippingFreightPayerMode.CollectOnDelivery"/> یعنی حمل هنگام تحویل
/// وصول می‌شود. بسته‌بندی همیشه بخش تسویه است، چون حامل هیچ قراردادی برای وصول آن ندارد.
/// این تفکیک یک <em>واقعیت نرخ</em> است و هیچ تراکنش پرداختی نمی‌سازد.
/// </para>
/// <para>
/// <b>این قرارداد امروز مسیر Checkout را تغییر نمی‌دهد.</b> هیچ مصرف‌کنندهٔ زنده‌ای مبالغ را
/// روی سفارش نمی‌نویسد؛ اتصال به قیمت‌گذاری سفارش یک تصمیم جداگانه و بازبینی‌شده است.
/// </para>
/// </remarks>
/// <param name="Outcome">نتیجهٔ قیمت‌گذاری.</param>
/// <param name="ServiceCode">کد خدمت درخواست‌شده.</param>
/// <param name="OptionCode">گزینهٔ درخواست‌شده؛ فقط برای ردیابی.</param>
/// <param name="CurrencyCode">ارز مبالغ؛ تهی یعنی هیچ مبلغی اعلام نشد.</param>
/// <param name="FreightAmount">هزینهٔ حمل حامل؛ تهی یعنی اعلام‌نشده.</param>
/// <param name="PackagingAmount">هزینهٔ بسته‌بندی؛ تهی یعنی اعلام‌نشده. صفر یک مقدار واقعی است.</param>
/// <param name="TotalServiceExpense">جمع حمل و بسته‌بندی؛ تهی یعنی اعلام‌نشده.</param>
/// <param name="AmountPayableAtCheckout">بخشی که هنگام تسویه پرداخت می‌شود.</param>
/// <param name="AmountCollectOnDelivery">بخشی که هنگام تحویل وصول می‌شود؛ در حالت پیش‌پرداخت صفر است.</param>
/// <param name="FreightPayerMode">طرف پرداخت‌کنندهٔ حمل.</param>
/// <param name="PackagingPayer">طرف پرداخت‌کنندهٔ بسته‌بندی؛ هرگز حامل نیست.</param>
/// <param name="CarrierFreight">مؤلفهٔ قطعی حمل، دست‌نخورده از موتور نرخ موجود.</param>
/// <param name="Packaging">مؤلفهٔ مستقل بسته‌بندی.</param>
/// <param name="Schedule">ارزیابی بازهٔ حامل.</param>
/// <param name="ReasonCode">کد دلیل پایدار نتیجهٔ قیمت‌گذاری.</param>
/// <param name="MatchedCondition">
/// پلهٔ اندازه‌ای که نرخ برنده با آن تطبیق یافت؛ تهی یعنی نرخ بی‌شرط بود یا نرخی اعلام نشد.
/// </param>
public sealed record ShippingOperationalQuote(
    ShippingOperationalQuoteOutcome Outcome,
    string ServiceCode,
    string? OptionCode,
    string? CurrencyCode,
    decimal? FreightAmount,
    decimal? PackagingAmount,
    decimal? TotalServiceExpense,
    decimal? AmountPayableAtCheckout,
    decimal? AmountCollectOnDelivery,
    ShippingFreightPayerMode FreightPayerMode,
    ShippingPackagingChargePayer PackagingPayer,
    ShippingQuote CarrierFreight,
    ShippingPackagingFeeComponent Packaging,
    ShippingScheduleEvaluation Schedule,
    string ReasonCode,
    ShippingConditionMatch? MatchedCondition = null)
{
    /// <summary>آیا مبالغ قطعی و قابل اعلام‌اند.</summary>
    public bool IsQuoted => Outcome == ShippingOperationalQuoteOutcome.Quoted;

    /// <summary>
    /// آیا این مرسوله از نظر عملیاتی واجد شرایط است: مبالغ قطعی <em>و</em> هیچ پنجرهٔ فعالی نقض
    /// نشده باشد. «مبهم» و «ارزیابی‌نشده» هرگز واجد شرایط شمرده نمی‌شوند.
    /// </summary>
    public bool IsOperationallyEligible => IsQuoted && Schedule.IsConstraintSatisfied;
}

/// <summary>
/// ورودی ارزیابی عملیاتی برای سطح ادمین. شکل صریح است تا یک ارزیابی تشخیصی بتواند بدون هیچ
/// نوشتن و بدون هیچ حدس اندازه، همان ورودی‌های واقعی seam را بدهد.
/// </summary>
/// <param name="Scope">دامنهٔ مقصد؛ شناسهٔ کشور الزامی است.</param>
/// <param name="CartSubtotalExclusive">جمع سبد بدون هزینهٔ ارسال.</param>
/// <param name="CurrencyCode">ارز درخواست؛ تهی یعنی بررسی ارز انجام نشود.</param>
/// <param name="OptionCode">گزینهٔ روش ارسال؛ تهی یعنی فقط در سطح خدمت.</param>
/// <param name="WeightGrams">وزن اندازه‌گیری‌شده بر حسب گرم؛ تهی یعنی اعلام‌نشده.</param>
/// <param name="ItemCount">تعداد اقلام فیزیکی مرسوله؛ تهی یعنی اعلام‌نشده.</param>
/// <param name="RequestedPickupAtUtc">لحظهٔ درخواستی تحویل به حامل به UTC؛ تهی یعنی ارزیابی زمانی انجام نشود.</param>
public sealed record ShippingOperationalQuoteEvaluationRequest(
    ShippingDestinationScopeWrite Scope,
    decimal CartSubtotalExclusive = 0m,
    string? CurrencyCode = null,
    string? OptionCode = null,
    long? WeightGrams = null,
    int? ItemCount = null,
    DateTimeOffset? RequestedPickupAtUtc = null);

/// <summary>
/// درز فقط-خواندنی Shipping برای ارزیابی عملیاتی کامل. هیچ نوشتنی، هیچ تراکنش و هیچ قاعدهٔ
/// تجاری تازه‌ای اضافه نمی‌کند: موتور نرخ، resolver بسته‌بندی و برنامهٔ عملیاتی را کنار هم
/// می‌گذارد و تفکیک پرداخت را صریح می‌کند.
/// </summary>
public interface IShippingOperationalQuoteLookup
{
    /// <summary>یک ارزیابی عملیاتی قطعی و fail-closed تولید می‌کند.</summary>
    /// <param name="request">درخواست ارزیابی.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    /// <returns>نتیجهٔ ارزیابی؛ هرگز null.</returns>
    Task<ShippingOperationalQuote> QuoteAsync(
        ShippingOperationalQuoteRequest request,
        CancellationToken cancellationToken);
}
