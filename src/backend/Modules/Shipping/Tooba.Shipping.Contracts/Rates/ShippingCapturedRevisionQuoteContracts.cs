using Tooba.BuildingBlocks.Results;
using Tooba.Geography.Contracts.Places;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Packaging;
namespace Tooba.Shipping.Contracts.Rates;

/// <summary>
/// نتیجهٔ خواندن/قیمت‌گذاری یک بازبینی <b>capture‌شدهٔ دقیق</b> از نگاه مصرف‌کنندهٔ بین‌ماژولی.
/// </summary>
/// <remarks>
/// این enum عمداً «قابل قیمت‌گذاری نبود» را از «قیمت صفر» جدا می‌کند: هر مقدار غیر از
/// <see cref="Quoted"/> یعنی هیچ مبلغی اعلام نمی‌شود و همهٔ مبالغ در نتیجه <c>null</c> می‌مانند.
/// </remarks>
public enum ShippingCapturedQuoteVerdict
{
    /// <summary>یک ردیف یکتا با محورهای اعلام‌شده تطبیق یافت و هر دو مؤلفه از همان ردیف‌اند.</summary>
    Quoted = 0,

    /// <summary>زمینهٔ نصب/مالک resolve‌نشده یا با مالک درخواستی ناسازگار است.</summary>
    OwnerScopeUnavailable = 1,

    /// <summary>بازبینی دقیق (مالک، پیش‌نویس، بازبینی) وجود ندارد یا مالک دیگری است.</summary>
    CapturedRevisionUnavailable = 2,

    /// <summary>مسیر مقصد در Geography اثبات نشد (غایب/غیرفعال/چرخه/کشور ناسازگار).</summary>
    DestinationUnresolved = 3,

    /// <summary>روش تنظیمات capture‌شده ندارد.</summary>
    NotConfigured = 4,

    /// <summary>حالت محاسبهٔ ذخیره‌شده Manual نیست (CarrierApi/Free هنوز پیاده نشده‌اند).</summary>
    CalculationModeUnavailable = 5,

    /// <summary>هیچ گروه ذخیره‌شده‌ای مقصد را پوشش نمی‌دهد.</summary>
    CoverageMissing = 6,

    /// <summary>بیش از یک گروه ذخیره‌شده مقصد را پوشش می‌دهد.</summary>
    GroupAmbiguous = 7,

    /// <summary>ارز گروه یا درخواست اعلام نشده است.</summary>
    CurrencyMissing = 8,

    /// <summary>ارز گروه با ارز درخواست یکی نیست؛ هیچ تبدیل ارزی انجام نمی‌شود.</summary>
    CurrencyMismatch = 9,

    /// <summary>گروه پوشش‌داده‌شده ردیف نرخ ندارد.</summary>
    RateRowMissing = 10,

    /// <summary>یک محور شناخته‌شده اندازهٔ لازم را ندارد؛ هرگز صفر/رایگان فرض نمی‌شود.</summary>
    MeasurementRequired = 11,

    /// <summary>هیچ ردیفی با اندازه‌های اعلام‌شده سازگار نیست.</summary>
    ConditionNotMatched = 12,

    /// <summary>بیش از یک ردیف سازگار است؛ ابهام حل نمی‌شود.</summary>
    ConditionAmbiguous = 13,

    /// <summary>دادهٔ capture‌شده ناسازگار است (کران نامعتبر، مبلغ منفی، شبکهٔ ناقص).</summary>
    ConfigurationInvalid = 14,
}

/// <summary>کدهای دلیل پایدار برای نتیجهٔ قیمت‌گذاری بازبینی capture‌شده.</summary>
public static class ShippingCapturedQuoteVerdictCodes
{
    /// <summary>یک ردیف یکتا قیمت داد.</summary>
    public const string Quoted = "shipping.captured.quote.quoted";

    /// <summary>زمینهٔ مالک/نصب resolve‌نشده یا ناسازگار.</summary>
    public const string OwnerScopeUnavailable = "shipping.captured.quote.owner_scope_unavailable";

    /// <summary>بازبینی دقیق در دسترس نیست.</summary>
    public const string CapturedRevisionUnavailable = "shipping.captured.quote.captured_revision_unavailable";

    /// <summary>مسیر مقصد اثبات نشد.</summary>
    public const string DestinationUnresolved = "shipping.captured.quote.destination_unresolved";

    /// <summary>تنظیمات روش ثبت نشده است.</summary>
    public const string NotConfigured = "shipping.captured.quote.not_configured";

    /// <summary>حالت CarrierApi اتصال واقعی ندارد.</summary>
    public const string CarrierModeUnavailable = "shipping.captured.quote.carrier_mode_unavailable";

    /// <summary>حالت Free هزینهٔ واقعی را صفر نمی‌کند.</summary>
    public const string FreeNotImplemented = "shipping.captured.quote.free_not_implemented";

    /// <summary>هیچ گروهی مقصد را پوشش نمی‌دهد.</summary>
    public const string CoverageMissing = "shipping.captured.quote.coverage_missing";

    /// <summary>بیش از یک گروه مقصد را پوشش می‌دهد.</summary>
    public const string GroupAmbiguous = "shipping.captured.quote.group_ambiguous";

    /// <summary>ارز اعلام نشده است.</summary>
    public const string CurrencyMissing = "shipping.captured.quote.currency_missing";

    /// <summary>ارزها یکی نیستند.</summary>
    public const string CurrencyMismatch = "shipping.captured.quote.currency_mismatch";

    /// <summary>گروه ردیف نرخ ندارد.</summary>
    public const string RateRowMissing = "shipping.captured.quote.rate_row_missing";

    /// <summary>اندازهٔ لازم اعلام نشده است.</summary>
    public const string MeasurementRequired = "shipping.captured.quote.measurement_required";

    /// <summary>هیچ ردیفی سازگار نیست.</summary>
    public const string ConditionNotMatched = "shipping.captured.quote.condition_not_matched";

    /// <summary>بیش از یک ردیف سازگار است.</summary>
    public const string ConditionAmbiguous = "shipping.captured.quote.condition_ambiguous";

    /// <summary>دادهٔ capture‌شده ناسازگار است.</summary>
    public const string ConfigurationInvalid = "shipping.captured.quote.configuration_invalid";
}

/// <summary>
/// صلاحیت مشتری/فعال‌سازی روش. این برش هیچ‌کدام را نمی‌سنجد و فقط یک مقدار صادقانه دارد.
/// </summary>
public enum ShippingCapturedQuoteCustomerEligibility
{
    /// <summary>صلاحیت مشتری/فعال‌سازی ارزیابی نشده است؛ یک قیمت پولی «فعال بودن روش» نیست.</summary>
    NotEvaluated = 0,
}

/// <summary>یک روز برنامهٔ دریافت ذخیره‌شده، همان‌گونه که capture شده بود.</summary>
/// <param name="Weekday">روز هفتهٔ ISO (۱ تا ۷).</param>
/// <param name="IsWorking">پرچم کاری ذخیره‌شده.</param>
/// <param name="StartTime">ساعت شروع ذخیره‌شده یا تهی.</param>
/// <param name="EndTime">ساعت پایان ذخیره‌شده یا تهی.</param>
public sealed record ShippingCapturedReceivingWeekDay(
    int Weekday,
    bool IsWorking,
    TimeOnly? StartTime,
    TimeOnly? EndTime);

/// <summary>
/// برنامهٔ هفتگی ذخیره‌شده به‌همراه منطقهٔ زمانی؛ فقط متادیتا، بدون هیچ تبدیل به پنجرهٔ تحویل.
/// </summary>
/// <param name="TimeZoneId">شناسهٔ منطقهٔ زمانی ذخیره‌شده.</param>
/// <param name="Days">روزهای ذخیره‌شده.</param>
/// <param name="SlotDurationMinutes">TB-241: مدت اختیاری هر نوبت تحویل به دقیقه یا تهی (پنجرهٔ کامل روز).</param>
public sealed record ShippingCapturedReceivingWeek(
    string TimeZoneId,
    IReadOnlyList<ShippingCapturedReceivingWeekDay> Days,
    int? SlotDurationMinutes = null);

/// <summary>تنظیمات ذخیره‌شدهٔ روش که به قیمت مربوط است.</summary>
/// <param name="FreightPaymentMode">طرف پرداخت کرایه.</param>
/// <param name="CalculationMode">حالت محاسبه.</param>
public sealed record ShippingCapturedQuoteSettings(
    ShippingFreightPayerMode FreightPaymentMode,
    ShippingAuthoringCalculationMode CalculationMode);

/// <summary>
/// گونهٔ مالکِ درخواستی در درز Contracts. این یک <b>هویت داده</b> است، نه مجوز: انطباق آن با زمینهٔ
/// نصب/تجارت جاری داخل Shipping سنجیده می‌شود.
/// </summary>
public enum ShippingCapturedQuoteOwnerKind
{
    /// <summary>فروشگاه SingleStore با tenant پایدار و بدون فروشنده.</summary>
    Store = 0,

    /// <summary>نصب Marketplace بدون tenant فروشگاه و بدون فروشنده.</summary>
    Marketplace = 1,

    /// <summary>یک فروشندهٔ Marketplace با شناسهٔ واقعی و بدون tenant.</summary>
    Seller = 2,
}

/// <summary>
/// درخواست قیمت‌گذاری یک بازبینی capture‌شدهٔ دقیق. همهٔ فیلدها دادهٔ صریح سرور هستند.
/// </summary>
/// <remarks>
/// <para>
/// <b>Owner key is data identity, not permission.</b> این کلید فقط می‌گوید کدام مالک را می‌پرسیم؛
/// مجوزدهی و انطباق با زمینهٔ نصب داخل Shipping انجام می‌شود و فراخوان آیندهٔ Order باید این کلید را
/// از cart/fulfillment مورداعتماد بسازد، نه از مرورگر.
/// </para>
/// <para>
/// <b>No fallback.</b> هیچ مفهوم latest/current/active اینجا نیست؛ فقط بازبینی دقیق درخواستی خوانده
/// می‌شود. مبالغ/اندازه‌های ورودی اعلام فراخوان‌اند و در این برش «تأییدشده توسط Cart» نیستند.
/// </para>
/// </remarks>
/// <param name="OwnerKind">گونهٔ مالک resolve‌شده.</param>
/// <param name="OwnerTenantId">tenant مالک Store یا تهی.</param>
/// <param name="OwnerSellerPartyId">شناسهٔ فروشندهٔ مالک Seller یا تهی.</param>
/// <param name="DraftId">شناسهٔ پیش‌نویس.</param>
/// <param name="SourceRevision">شمارهٔ بازبینی مبدأ موردانتظار.</param>
/// <param name="Destination">شناسه‌های پایدار مقصد؛ والدها باید سازگار باشند.</param>
/// <param name="GroupWeightGrams">وزن اندازه‌گیری‌شدهٔ گروه یا تهی.</param>
/// <param name="GroupItemCount">تعداد اقلام گروه یا تهی.</param>
/// <param name="FinalGroupGoodsAmountAfterDiscountAndTax">مبلغ نهایی کالای همین گروه پس از تخفیف و مالیات یا تهی.</param>
/// <param name="CurrencyCode">ارز دقیق درخواست یا تهی.</param>
public sealed record ShippingCapturedQuoteLookupRequest(
    ShippingCapturedQuoteOwnerKind OwnerKind,
    string? OwnerTenantId,
    Guid? OwnerSellerPartyId,
    Guid DraftId,
    long SourceRevision,
    DestinationIdentifiers Destination,
    long? GroupWeightGrams = null,
    int? GroupItemCount = null,
    decimal? FinalGroupGoodsAmountAfterDiscountAndTax = null,
    string? CurrencyCode = null);

/// <summary>
/// نتیجهٔ قطعی قیمت‌گذاری یک بازبینی capture‌شده، همراه با provenance کامل برای مرحلهٔ بعدی.
/// </summary>
/// <remarks>
/// <para>
/// <b>یک ردیف، دو مؤلفه.</b> در حالت <see cref="ShippingCapturedQuoteVerdict.Quoted"/> کرایه و
/// بسته‌بندی هر دو از <see cref="RateRowId"/> می‌آیند و تفکیک پرداخت از معنای تعریف‌شدهٔ ماژول تبعیت
/// می‌کند: بسته‌بندی همیشه هنگام تسویه، و فقط کرایه می‌تواند توسط حامل وصول شود.
/// </para>
/// <para>
/// <b>قیمت ≠ فعال بودن.</b> <see cref="Eligibility"/> همیشه
/// <see cref="ShippingCapturedQuoteCustomerEligibility.NotEvaluated"/> است و هیچ مصرف‌کننده‌ای نباید از این
/// نتیجه «روش فعال/آماده» یا «مجاز برای این مشتری» نتیجه بگیرد. هیچ نوشتنی ساخته نمی‌شود.
/// </para>
/// </remarks>
/// <param name="Verdict">وضعیت قطعی.</param>
/// <param name="ReasonCode">کد دلیل پایدار.</param>
/// <param name="ServiceCode">کد خدمت capture‌شده.</param>
/// <param name="OptionCode">گزینهٔ capture‌شده یا تهی.</param>
/// <param name="CurrencyCode">ارز نتیجه؛ فقط در حالت قیمت‌دار مقدار دارد.</param>
/// <param name="FreightAmount">کرایهٔ ردیف برنده؛ در غیر قیمت‌دار تهی.</param>
/// <param name="PackagingAmount">بسته‌بندی همان ردیف؛ در غیر قیمت‌دار تهی.</param>
/// <param name="TotalExpenseAmount">جمع کرایه و بسته‌بندی؛ در غیر قیمت‌دار تهی.</param>
/// <param name="PayableAtCheckoutAmount">سهم پرداخت هنگام تسویه؛ در غیر قیمت‌دار تهی.</param>
/// <param name="CarrierCollectAmount">سهم وصول توسط حامل؛ در غیر قیمت‌دار تهی.</param>
/// <param name="Settings">تنظیمات ذخیره‌شدهٔ روش؛ در غیر قیمت‌دار تهی.</param>
/// <param name="SnapshotId">شناسهٔ ردیف capture؛ در دسترس‌نبودن بازبینی تهی است.</param>
/// <param name="DraftId">پیش‌نویس capture‌شده.</param>
/// <param name="SourceRevision">بازبینی capture‌شده.</param>
/// <param name="FormatVersion">نسخهٔ قالب payload.</param>
/// <param name="ShippingTypeId">شناسهٔ خام نوع ذخیره‌شده یا تهی.</param>
/// <param name="GroupId">گروه برنده یا تهی.</param>
/// <param name="RateRowId">ردیف برنده یا تهی.</param>
/// <param name="MinDeliveryDays">کمینهٔ مدت تحویل ذخیره‌شده یا تهی.</param>
/// <param name="MaxDeliveryDays">بیشینهٔ مدت تحویل ذخیره‌شده یا تهی.</param>
/// <param name="ReceivingWeek">برنامهٔ هفتگی ذخیره‌شده یا تهی.</param>
public sealed record ShippingCapturedQuoteResult(
    ShippingCapturedQuoteVerdict Verdict,
    string ReasonCode,
    string? ServiceCode,
    string? OptionCode,
    string? CurrencyCode,
    decimal? FreightAmount,
    decimal? PackagingAmount,
    decimal? TotalExpenseAmount,
    decimal? PayableAtCheckoutAmount,
    decimal? CarrierCollectAmount,
    ShippingCapturedQuoteSettings? Settings,
    Guid? SnapshotId,
    Guid? DraftId,
    long? SourceRevision,
    int? FormatVersion,
    Guid? ShippingTypeId,
    Guid? GroupId,
    Guid? RateRowId,
    int? MinDeliveryDays,
    int? MaxDeliveryDays,
    ShippingCapturedReceivingWeek? ReceivingWeek)
{
    /// <summary>آیا این نتیجه یک قیمت پولی واقعی است.</summary>
    public bool IsQuoted => Verdict == ShippingCapturedQuoteVerdict.Quoted;

    /// <summary>صلاحیت مشتری/فعال‌سازی؛ این برش هرگز آن را نمی‌سنجد.</summary>
    public ShippingCapturedQuoteCustomerEligibility Eligibility => ShippingCapturedQuoteCustomerEligibility.NotEvaluated;
}

/// <summary>
/// تنها درز خواندنِ بین‌ماژولی برای قیمت یک بازبینی capture‌شدهٔ دقیق. فقط-خواندنی است و هیچ
/// فعال‌سازی/انتشار/سفارشی نمی‌سازد.
/// </summary>
/// <remarks>
/// این درز عمداً payload یا سیاست را برنمی‌گرداند؛ فقط نتیجهٔ قطعی پولی و provenance را می‌دهد تا
/// مصرف‌کننده نتواند قواعد قیمت‌گذاری را بازپیاده‌سازی کند.
/// </remarks>
public interface IShippingCapturedRevisionQuoteLookup
{
    /// <summary>
    /// بازبینی دقیق را می‌خواند، مسیر مقصد را در Geography اثبات می‌کند و قیمت را قطعی برمی‌گرداند.
    /// </summary>
    /// <param name="request">درخواست صریح سرور.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    /// <returns>نتیجه؛ هرگز null. نبود مالک/بازبینی/مقصد یک <see cref="ShippingCapturedQuoteResult"/> صریح است.</returns>
    Task<Result<ShippingCapturedQuoteResult>> QuoteAsync(
        ShippingCapturedQuoteLookupRequest request,
        CancellationToken cancellationToken);
}
