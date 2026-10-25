using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Authoring;

namespace Tooba.Shipping.Domain.Rates;

/// <summary>
/// نتیجهٔ قیمت‌گذاری یک بازبینیِ <b>capture‌شدهٔ</b> دقیق؛ فقط پولی و فقط محاسباتی.
/// </summary>
/// <remarks>
/// این enum عمداً «قابل قیمت‌گذاری نبود» را از «قیمت صفر» جدا می‌کند: هر مقدار غیر از
/// <see cref="Quoted"/> یعنی هیچ مبلغی اعلام نمی‌شود و همهٔ مبالغ در نتیجه <c>null</c> می‌مانند.
/// </remarks>
public enum ShippingCapturedQuoteOutcome
{
    /// <summary>یک ردیف یکتا با همهٔ محورهای شناخته‌شده تطبیق یافت و هر دو مؤلفهٔ هزینه از همان ردیف‌اند.</summary>
    Quoted = 0,

    /// <summary>هویت مالک/پیش‌نویس/بازبینی payload با هویت موردانتظار فراخوان یکی نیست؛ هیچ دادهٔ غریبه خوانده نمی‌شود.</summary>
    IdentityMismatch = 1,

    /// <summary>روش هیچ تنظیمات capture‌شده‌ای ندارد؛ «تنظیم‌نشده» هرگز پیش‌فرض نمی‌شود.</summary>
    NotConfigured = 2,

    /// <summary>حالت محاسبهٔ ذخیره‌شده Manual نیست (CarrierApi/Free هنوز پیاده نشده‌اند).</summary>
    CalculationModeUnavailable = 3,

    /// <summary>هیچ گروه ذخیره‌شده‌ای مسیر مقصد را پوشش نمی‌دهد.</summary>
    CoverageMissing = 4,

    /// <summary>بیش از یک گروه ذخیره‌شده مسیر مقصد را پوشش می‌دهد؛ «اولین/کم‌هزینه‌ترین» انتخاب نمی‌شود.</summary>
    GroupAmbiguous = 5,

    /// <summary>ارز گروه یا ارز درخواست اعلام نشده است.</summary>
    CurrencyMissing = 6,

    /// <summary>ارز گروه با ارز درخواست یکی نیست؛ هیچ تبدیل ارزی انجام نمی‌شود.</summary>
    CurrencyMismatch = 7,

    /// <summary>گروه پوشش‌داده‌شده هیچ ردیف نرخی ندارد.</summary>
    RateRowMissing = 8,

    /// <summary>یک محور شناخته‌شده روی ردیف هست و اندازهٔ لازم اعلام نشده؛ هرگز صفر/رایگان فرض نمی‌شود.</summary>
    MeasurementRequired = 9,

    /// <summary>اندازه‌ها اعلام شده‌اند ولی هیچ ردیفی با آن‌ها سازگار نیست.</summary>
    ConditionNotMatched = 10,

    /// <summary>بیش از یک ردیف با همان اندازه‌ها سازگار است؛ ابهام حل نمی‌شود.</summary>
    ConditionAmbiguous = 11,

    /// <summary>مسیر مقصد از نظر ساختاری ناقص/ناسازگار است (والدِ الزامی غایب یا شناسهٔ تهی).</summary>
    DestinationPathInvalid = 12,

    /// <summary>دادهٔ capture‌شده خودش ناسازگار است (کران نامعتبر، مبلغ منفی، روزهای نامعتبر، enum ناشناخته).</summary>
    ConfigurationInvalid = 13,
}

/// <summary>
/// صلاحیت مشتری/فعال‌سازی روش. این برش <b>هیچ‌کدام</b> را نمی‌سنجد و فقط یک مقدار صادقانه دارد.
/// </summary>
/// <remarks>
/// یک enum تک‌مقداری عمداً انتخاب شده تا هیچ فراخوانی نتواند یک نتیجهٔ پولی را به‌عنوان «روش برای
/// مشتری فعال است» بخواند؛ افزودن مقدار واقعی نیازمند تصمیم و اثبات جداگانه است.
/// </remarks>
public enum ShippingCapturedQuoteEligibility
{
    /// <summary>صلاحیت مشتری/فعال‌سازی در این برش ارزیابی نشده است.</summary>
    NotEvaluated = 0,
}

/// <summary>
/// کدهای دلیل پایدار برای نتیجهٔ قیمت‌گذاری بازبینی capture‌شده؛ مستقل از زبان و مناسب لاگ/تلمتری.
/// </summary>
public static class ShippingCapturedQuoteReasonCodes
{
    /// <summary>یک ردیف یکتا قیمت داد و هر دو مؤلفه از همان ردیف‌اند.</summary>
    public const string Quoted = "shipping.captured.quote.quoted";

    /// <summary>هویت payload با هویت موردانتظار فراخوان یکی نیست.</summary>
    public const string IdentityMismatch = "shipping.captured.quote.identity_mismatch";

    /// <summary>تنظیمات روش در capture ثبت نشده است.</summary>
    public const string NotConfigured = "shipping.captured.quote.not_configured";

    /// <summary>حالت محاسبه CarrierApi است و اتصال واقعی حامل وجود ندارد.</summary>
    public const string CarrierModeUnavailable = "shipping.captured.quote.carrier_mode_unavailable";

    /// <summary>حالت محاسبه Free است و صفر کردن هزینهٔ واقعی رایگان‌بودن نمی‌سازد.</summary>
    public const string FreeNotImplemented = "shipping.captured.quote.free_not_implemented";

    /// <summary>هیچ گروهی مسیر مقصد را پوشش نمی‌دهد.</summary>
    public const string CoverageMissing = "shipping.captured.quote.coverage_missing";

    /// <summary>بیش از یک گروه مسیر مقصد را پوشش می‌دهد.</summary>
    public const string GroupAmbiguous = "shipping.captured.quote.group_ambiguous";

    /// <summary>ارز گروه یا درخواست اعلام نشده است.</summary>
    public const string CurrencyMissing = "shipping.captured.quote.currency_missing";

    /// <summary>ارز گروه و درخواست یکی نیست.</summary>
    public const string CurrencyMismatch = "shipping.captured.quote.currency_mismatch";

    /// <summary>گروه پوشش‌داده‌شده ردیف نرخ ندارد.</summary>
    public const string RateRowMissing = "shipping.captured.quote.rate_row_missing";

    /// <summary>اندازهٔ لازم برای یک محور شناخته‌شده اعلام نشده است.</summary>
    public const string MeasurementRequired = "shipping.captured.quote.measurement_required";

    /// <summary>هیچ ردیفی با اندازه‌های اعلام‌شده سازگار نیست.</summary>
    public const string ConditionNotMatched = "shipping.captured.quote.condition_not_matched";

    /// <summary>بیش از یک ردیف سازگار است.</summary>
    public const string ConditionAmbiguous = "shipping.captured.quote.condition_ambiguous";

    /// <summary>مسیر مقصد ساختاراً ناقص/ناسازگار است.</summary>
    public const string DestinationPathInvalid = "shipping.captured.quote.destination_path_invalid";

    /// <summary>دادهٔ capture‌شده ناسازگار است.</summary>
    public const string ConfigurationInvalid = "shipping.captured.quote.configuration_invalid";
}

/// <summary>
/// بازهٔ نیم‌باز <c>[Min, Max)</c> برای محور <b>مبلغ کالا</b>؛ آینهٔ دقیق
/// <see cref="ShippingNumericTier"/> روی <see cref="decimal"/>.
/// </summary>
/// <remarks>
/// محور مبلغ نمی‌تواند از بازهٔ صحیح استفاده کند چون مبلغ اعشاری است. معنای نیم‌باز عیناً همان است:
/// <c>[100000, 5000000)</c> و <c>[5000000, ∞)</c> هیچ مبلغی را در دو ردیف هم‌زمان نمی‌گذارند و مرز
/// دقیقاً یک بار قیمت می‌گیرد. <see cref="Max"/> تهی یعنی کران بالای باز.
/// </remarks>
public sealed record ShippingAmountTier
{
    /// <summary>کران پایین؛ شامل و همیشه مشخص.</summary>
    public decimal Min { get; }

    /// <summary>کران بالا؛ غیرشامل. تهی یعنی بی‌کران بالا.</summary>
    public decimal? Max { get; }

    private ShippingAmountTier(decimal min, decimal? max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>بازهٔ <c>[min, max)</c> می‌سازد؛ <paramref name="max"/> تهی یعنی بی‌کران بالا.</summary>
    /// <param name="min">کران پایین شامل.</param>
    /// <param name="max">کران بالا غیرشامل؛ در صورت وجود باید اکیداً بزرگ‌تر از کران پایین باشد.</param>
    public static ShippingAmountTier Between(decimal min, decimal? max) => new(min, max);

    /// <summary>آیا مقدار داده‌شده داخل این بازه است (پایین شامل، بالا غیرشامل).</summary>
    /// <param name="value">مبلغ اندازه‌گیری‌شده.</param>
    public bool Contains(decimal value) => value >= Min && (Max is not { } max || value < max);
}

/// <summary>
/// هویتِ موردانتظارِ کاملِ یک بازبینی capture‌شده، همان‌طور که فراخوانِ سرور آن را در اختیار دارد.
/// </summary>
/// <remarks>
/// این هویت از payloadِ رمزگشایی‌شده <b>مستقل</b> است تا یک payload از مالک/پیش‌نویس/بازبینی دیگر
/// نتواند قیمت بگیرد. مالک در این لایه فقط <em>هویت داده</em> است؛ مجوزدهی مالک/نسخه در لایهٔ
/// سرور موجود انجام می‌شود.
/// </remarks>
/// <param name="OwnerKind">گونهٔ مالک موردانتظار.</param>
/// <param name="OwnerTenantId">tenant مالک Store یا تهی.</param>
/// <param name="OwnerSellerPartyId">شناسهٔ فروشندهٔ مالک Seller یا تهی.</param>
/// <param name="DraftId">شناسهٔ پیش‌نویس موردانتظار.</param>
/// <param name="SourceRevision">شمارهٔ بازبینی مبدأ موردانتظار.</param>
public sealed record ShippingCapturedRevisionIdentity(
    ShippingDraftOwnerKind OwnerKind,
    string? OwnerTenantId,
    Guid? OwnerSellerPartyId,
    Guid DraftId,
    long SourceRevision);

/// <summary>
/// ورودیِ صریح و مورداعتمادِ سرور برای قیمت‌گذاری یک بازبینی capture‌شده. هیچ HTTP/مرورگر/ساعت/DB
/// در این ورودی دخالت ندارد.
/// </summary>
/// <remarks>
/// <para>
/// <b>مسیر مقصد.</b> <see cref="Destination"/> همان مسیر تایپ‌شدهٔ شناسه‌های Geography همراه با
/// والدهاست؛ این متد فقط ساختار آن را می‌سنجد. اعتبارسنجی «این مسیر واقعاً در Geography وجود دارد»
/// وظیفهٔ آداپتر آینده است و هیچ UUID دلخواهی به‌خودی‌خود مورداعتماد نیست.
/// </para>
/// <para>
/// <b>مبلغ کالا.</b> <see cref="FinalGroupGoodsAmountAfterDiscountAndTax"/> مبلغ نهاییِ کالاهای
/// <em>همین گروه</em> پس از تخفیف و پس از مالیات است؛ جمع سبد بدون مالیات جای آن را نمی‌گیرد و این
/// لایه هیچ تخصیص مالیات/تخفیفی نمی‌سازد.
/// </para>
/// </remarks>
/// <param name="Expected">هویت موردانتظار کامل (مالک/پیش‌نویس/بازبینی).</param>
/// <param name="CapturedRevision">بدنهٔ تایپ‌شدهٔ همان بازبینی capture‌شده (رمزگشایی‌شده).</param>
/// <param name="Destination">مسیر تایپ‌شدهٔ مقصد با والدها.</param>
/// <param name="GroupWeightGrams">وزن اندازه‌گیری‌شدهٔ گروه؛ تهی یعنی اعلام‌نشده.</param>
/// <param name="GroupItemCount">تعداد اقلام گروه؛ تهی یعنی اعلام‌نشده.</param>
/// <param name="FinalGroupGoodsAmountAfterDiscountAndTax">مبلغ نهایی کالای گروه پس از تخفیف و مالیات؛ تهی یعنی اعلام‌نشده.</param>
/// <param name="CurrencyCode">ارز دقیق درخواست؛ تهی یعنی اعلام‌نشده.</param>
/// <param name="AdministrativeAreaAncestors">
/// زنجیرهٔ <b>اثبات‌شدهٔ</b> ناحیهٔ اداری مقصد از ریشه تا برگ؛ تهی یعنی فراخوان فقط همان برگ را
/// اعلام کرده است. وقتی داده شود، پوشش ناحیهٔ اداری ذخیره‌شده با هر عضو این زنجیره تطبیق می‌یابد
/// (ناحیهٔ ریشه‌ایِ والد یک شهر عمیق هم پوشش می‌دهد) و هیچ شناسه‌ای بیرون این زنجیره پذیرفته نمی‌شود.
/// </param>
public sealed record ShippingCapturedRevisionQuoteRequest(
    ShippingCapturedRevisionIdentity Expected,
    ShippingAuthoringRevisionPayload CapturedRevision,
    ShippingDestinationScope Destination,
    long? GroupWeightGrams = null,
    int? GroupItemCount = null,
    decimal? FinalGroupGoodsAmountAfterDiscountAndTax = null,
    string? CurrencyCode = null,
    IReadOnlyList<Guid>? AdministrativeAreaAncestors = null);

/// <summary>شرط‌های واقعاً تطبیق‌یافتهٔ ردیف برنده؛ فقط توصیفی و برای شفافیت.</summary>
/// <param name="Weight">بازهٔ وزن برنده؛ تهی یعنی ردیف روی وزن شرطی نبود.</param>
/// <param name="Count">بازهٔ تعداد برنده؛ تهی یعنی ردیف روی تعداد شرطی نبود.</param>
/// <param name="Merchandise">بازهٔ مبلغ کالای برنده؛ تهی یعنی ردیف روی مبلغ شرطی نبود.</param>
public sealed record ShippingCapturedQuoteMatchedConditions(
    ShippingNumericTier? Weight,
    ShippingNumericTier? Count,
    ShippingAmountTier? Merchandise);

/// <summary>
/// ردیف و گروه برندهٔ یک قیمت، همراه با همان واقعیت‌های capture‌شده‌ای که مرحلهٔ تقویم/سفارش بعدی
/// لازم دارد. هیچ سیاستی بازتفسیر و هیچ برچسب زبانی ساخته نمی‌شود.
/// </summary>
/// <param name="GroupId">گروه برنده.</param>
/// <param name="RateRowId">ردیف برنده (هویت مبلغ‌ها).</param>
/// <param name="DraftId">پیش‌نویس capture‌شده.</param>
/// <param name="SourceRevision">بازبینی capture‌شده.</param>
/// <param name="FormatVersion">نسخهٔ قالب payload.</param>
/// <param name="ShippingTypeId">شناسهٔ خام نوع ذخیره‌شده یا تهی (بدون بازخوانی کاتالوگ).</param>
/// <param name="MinDeliveryDays">کمینهٔ مدت تحویل ذخیره‌شده.</param>
/// <param name="MaxDeliveryDays">بیشینهٔ مدت تحویل ذخیره‌شده.</param>
/// <param name="Settings">تنظیمات ذخیره‌شدهٔ روش (پرداخت و حالت محاسبه).</param>
/// <param name="WeeklySchedule">برنامهٔ هفتگی ذخیره‌شده یا تهی (بدون تبدل به پنجرهٔ تحویل).</param>
/// <param name="Conditions">شرط‌های تطبیق‌یافتهٔ ردیف و مقدار اندازه‌های اعلام‌شده.</param>
public sealed record ShippingCapturedQuoteSelection(
    Guid GroupId,
    Guid RateRowId,
    Guid DraftId,
    long SourceRevision,
    int FormatVersion,
    Guid? ShippingTypeId,
    int MinDeliveryDays,
    int MaxDeliveryDays,
    ShippingAuthoringRevisionSettings Settings,
    ShippingAuthoringRevisionWeek? WeeklySchedule,
    ShippingCapturedQuoteMatchedConditions Conditions);

/// <summary>
/// نتیجهٔ قطعی قیمت‌گذاری یک بازبینی capture‌شده.
/// </summary>
/// <remarks>
/// <para>
/// <b>یک ردیف، دو مؤلفه.</b> در حالت <see cref="ShippingCapturedQuoteOutcome.Quoted"/>، کرایه و
/// بسته‌بندی هر دو از <em>همان</em> ردیف برنده می‌آیند؛ جمع‌کردن کرایهٔ یک ردیف با بسته‌بندی ردیف
/// دیگر ممنوع است.
/// </para>
/// <para>
/// <b>تفکیک پرداخت.</b> بسته‌بندی همیشه هنگام تسویه پرداخت می‌شود (حامل هیچ قراردادی برای آن ندارد)،
/// پس فقط مؤلفهٔ کرایه در حالت <see cref="ShippingFreightPayerMode.CollectOnDelivery"/> به حامل
/// منتقل می‌شود. این همان معنای پرداخت‌کنندهٔ تعریف‌شده در ماژول است و هیچ تراکنش/تخصیصی نمی‌سازد.
/// </para>
/// <para>
/// <b>قیمت پولی ≠ صلاحیت مشتری.</b> نتیجه فقط یک قیمت پولی است؛ <see cref="Eligibility"/> همیشه
/// <see cref="ShippingCapturedQuoteEligibility.NotEvaluated"/> است و هیچ مصرف‌کننده‌ای نباید از آن
/// «روش فعال/آماده» نتیجه بگیرد.
/// </para>
/// </remarks>
/// <param name="Outcome">وضعیت قطعی.</param>
/// <param name="ReasonCode">کد دلیل پایدار.</param>
/// <param name="ServiceCode">کد خدمت capture‌شده.</param>
/// <param name="OptionCode">گزینهٔ capture‌شده یا تهی.</param>
/// <param name="CurrencyCode">ارز نتیجه؛ فقط در حالت قیمت‌دار مقدار دارد.</param>
/// <param name="FreightAmount">کرایهٔ ردیف برنده؛ در غیر قیمت‌دار تهی.</param>
/// <param name="PackagingAmount">بسته‌بندی همان ردیف برنده؛ در غیر قیمت‌دار تهی.</param>
/// <param name="TotalExpenseAmount">جمع کرایه و بسته‌بندی؛ در غیر قیمت‌دار تهی.</param>
/// <param name="PayableAtCheckoutAmount">سهم پرداخت هنگام تسویه؛ در غیر قیمت‌دار تهی.</param>
/// <param name="CarrierCollectAmount">سهم وصول توسط حامل؛ در غیر قیمت‌دار تهی.</param>
/// <param name="FreightPayerMode">طرف پرداخت کرایهٔ ذخیره‌شده؛ در غیر قیمت‌دار تهی.</param>
/// <param name="Selection">ردیف/گروه برنده و متادیتای provenance؛ در غیر قیمت‌دار تهی.</param>
public sealed record ShippingCapturedRevisionQuote(
    ShippingCapturedQuoteOutcome Outcome,
    string ReasonCode,
    string ServiceCode,
    string? OptionCode,
    string? CurrencyCode,
    decimal? FreightAmount,
    decimal? PackagingAmount,
    decimal? TotalExpenseAmount,
    decimal? PayableAtCheckoutAmount,
    decimal? CarrierCollectAmount,
    ShippingFreightPayerMode? FreightPayerMode,
    ShippingCapturedQuoteSelection? Selection)
{
    /// <summary>آیا این نتیجه یک قیمت پولی واقعی است.</summary>
    public bool IsQuoted => Outcome == ShippingCapturedQuoteOutcome.Quoted;

    /// <summary>صلاحیت مشتری/فعال‌سازی؛ این برش هرگز آن را نمی‌سنجد.</summary>
    public ShippingCapturedQuoteEligibility Eligibility => ShippingCapturedQuoteEligibility.NotEvaluated;
}
