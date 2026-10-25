using Tooba.Geography.Contracts.Places;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Order.Application.Storefront.Shipping.CapturedGroupProjection;

/// <summary>
/// یک نامزد روش برای یک گروه ارسال: هویت <b>کامل و صریح سرور</b> برای پرس‌وجوی یک بازبینی capture‌شدهٔ
/// دقیق. هیچ‌کدام از این مقادیر از مرورگر/برنامهٔ کلاینت پذیرفته نمی‌شود.
/// </summary>
/// <remarks>
/// <para>
/// <b>No latest/current/active.</b> <see cref="DraftId"/> و <see cref="SourceRevision"/> بازبینی
/// دقیق را می‌گویند؛ این برش هیچ نسخهٔ منتشرشده/فعال را خودش انتخاب نمی‌کند.
/// </para>
/// <para>
/// <b>No inferred responsibility.</b> <see cref="OwnerKind"/> و کلید مالک از برنامهٔ سرور می‌آیند و
/// معنای آن‌ها «کدام مالک را می‌پرسیم» است، نه مجوز؛ هیچ نگاشت Seller→fulfillment اینجا ساخته نمی‌شود.
/// </para>
/// </remarks>
/// <param name="MethodCode">برچسب پایدار نامزد برای مصرف‌کننده (بدون شکستن به مالک/سرویس حدسی).</param>
/// <param name="OwnerKind">گونهٔ مالک resolve‌شده.</param>
/// <param name="OwnerTenantId">tenant مالک Store یا تهی.</param>
/// <param name="OwnerSellerPartyId">شناسهٔ فروشندهٔ مالک Seller یا تهی.</param>
/// <param name="DraftId">شناسهٔ پیش‌نویس capture‌شده.</param>
/// <param name="SourceRevision">شمارهٔ بازبینی دقیق موردانتظار.</param>
/// <param name="Destination">شناسه‌های پایدار مقصد؛ همان‌هایی که باید اثبات شوند.</param>
/// <param name="ServiceCode">کد خدمت موردانتظار؛ پاسخ با کد دیگر پذیرفته نمی‌شود.</param>
/// <param name="OptionCode">کد گزینهٔ موردانتظار یا تهی.</param>
/// <param name="AvailableDateCount">
/// تعداد صریح تاریخ‌های قابل ارائه برای تقویم دریافت (ورودی سرور، نه افق روش/تنظیمات دیگر). صفر یعنی
/// فقط زودترین تاریخ بدون فهرست تاریخ.
/// </param>
public sealed record StorefrontCapturedCandidatePlan(
    string MethodCode,
    ShippingCapturedQuoteOwnerKind OwnerKind,
    string? OwnerTenantId,
    Guid? OwnerSellerPartyId,
    Guid DraftId,
    long SourceRevision,
    DestinationIdentifiers Destination,
    string ServiceCode,
    string? OptionCode,
    int AvailableDateCount);

/// <summary>
/// برنامهٔ سرور برای یک گروه ارسال: کلید پایدار گروه، عضوهای دقیق همان گروه و نامزدهای روش.
/// </summary>
/// <param name="GroupKey">کلید پایدار گروه؛ تهی/تکراری/با فاصلهٔ اضافه مجاز نیست.</param>
/// <param name="CartLineIds">خطوط سبد همین گروه؛ افراز کامل سبد باید برقرار باشد.</param>
/// <param name="Candidates">نامزدهای روش همین گروه.</param>
public sealed record StorefrontCapturedGroupPlan(
    string GroupKey,
    IReadOnlyList<Guid> CartLineIds,
    IReadOnlyList<StorefrontCapturedCandidatePlan> Candidates);

/// <summary>
/// درخواست ترکیب: همان فرمان/دسترسی/نسخهٔ checkout به‌همراه برنامهٔ گروه‌ها.
/// </summary>
/// <param name="Command">فرمان checkout مورداعتماد سرور.</param>
/// <param name="Groups">گروه‌های افرازکنندهٔ سبد.</param>
public sealed record StorefrontCapturedGroupProjectionRequest(
    SubmitCheckoutCommand Command,
    IReadOnlyList<StorefrontCapturedGroupPlan> Groups);

/// <summary>وضعیت پولی یک نامزد در نتیجهٔ ترکیب.</summary>
public enum StorefrontCapturedCandidateOutcome
{
    /// <summary>یک بازبینی دقیق قیمت داد و همهٔ اجزا برای همین نامزد اثبات شد.</summary>
    Quoted = 0,

    /// <summary>قیمتی اعلام نمی‌شود؛ دلیل پایدار در نتیجه است و هیچ مبلغ ساختگی جای آن نیست.</summary>
    Unavailable = 1,
}

/// <summary>
/// تقویم دریافت مشتری برای یک نامزد: زودترین تاریخ، تاریخ‌های مجاز با بازهٔ همان روز، یا دلیل صریح
/// نبود تاریخ.
/// </summary>
/// <remarks>
/// خالی‌بودن <see cref="AvailableDates"/> با <see cref="UnavailableReason"/> تهی یعنی «افق صفر»؛ با دلیل
/// غیرتهی یعنی دادهٔ capture‌شده اجازهٔ تاریخ نمی‌دهد. هیچ بازهٔ ثابت مثل ۹ تا ۱۲ ساخته نمی‌شود.
/// </remarks>
/// <param name="EarliestEligibleDate">زودترین تاریخ ممکن یا تهی.</param>
/// <param name="AvailableDates">تاریخ‌های قابل ارائه با بازهٔ همان روز.</param>
/// <param name="UnavailableReason">دلیل پایدار نبود تاریخ یا تهی.</param>
public sealed record StorefrontCapturedReceivingDates(
    DateOnly? EarliestEligibleDate,
    IReadOnlyList<StorefrontShippingReceivingSlot> AvailableDates,
    string? UnavailableReason)
{
    /// <summary>آیا دست‌کم یک تاریخ قابل ارائه وجود دارد.</summary>
    public bool HasAvailableDates => AvailableDates.Count > 0;
}

/// <summary>
/// نتیجهٔ یک نامزد: هویت درخواستی، وضعیت، دلیل پایدار، نتیجهٔ کامل قیمت مالک (provenance/settings/
/// receiving week) و تقویم دریافت همین نامزد.
/// </summary>
/// <remarks>
/// <see cref="Outcome"/> = <see cref="StorefrontCapturedCandidateOutcome.Quoted"/> فقط یعنی «یک ردیف
/// یکتا برای این اندازه‌ها قیمت داد». این یعنی فعال‌بودن روش یا مجازبودن مشتری نیست:
/// <see cref="ShippingCapturedQuoteResult.Eligibility"/> همیشه
/// <see cref="ShippingCapturedQuoteCustomerEligibility.NotEvaluated"/> می‌ماند.
/// </remarks>
/// <param name="Identity">همان نامزد صریح سرور.</param>
/// <param name="Outcome">وضعیت پولی پذیرفته‌شده.</param>
/// <param name="ReasonCode">کد دلیل پایدار؛ در حالت قیمت‌دار کد «quoted» مالک است.</param>
/// <param name="OwnerResult">
/// پاسخ مالک که <b>به همین نامزد متعهد</b> بود؛ در حالت غیرقیمت‌دار مبالغش تهی است و در حالت
/// ناسازگاری تعهد کاملاً تهی می‌ماند تا هیچ مبلغی به اشتباه به این نامزد نسبت داده نشود.
/// </param>
/// <param name="Receiving">تقویم دریافت؛ در نبود قیمت یا نبود هفته تهی است.</param>
public sealed record StorefrontCapturedCandidateProjection(
    StorefrontCapturedCandidatePlan Identity,
    StorefrontCapturedCandidateOutcome Outcome,
    string ReasonCode,
    ShippingCapturedQuoteResult? OwnerResult,
    StorefrontCapturedReceivingDates? Receiving);

/// <summary>
/// فکت‌های مالی گروه به‌همراه نامزدهای همان گروه.
/// </summary>
/// <remarks>
/// <see cref="FinalGoodsAmountAfterDiscountAndTax"/> همان مقدار واقعی منبع Order (پس از تخفیف و پس از
/// مالیات) است و مبنای اندازهٔ مالی نامزدها بود؛ نه مبلغی که کلاینت فرستاده و نه سهمی از جمع کل سبد.
/// <see cref="GroupWeightGrams"/> و <see cref="PhysicalItemCount"/> تا وجود منبع authoritative تهی
/// می‌مانند.
/// </remarks>
/// <param name="GroupKey">کلید پایدار گروه.</param>
/// <param name="SellerPartyId">فروشندهٔ واقعی گروه.</param>
/// <param name="Currency">ارز یگانهٔ همان گروه.</param>
/// <param name="FinalGoodsAmountAfterDiscountAndTax">مبلغ نهایی کالای همان گروه.</param>
/// <param name="QuantityTotal">جمع مقدار خطوط (متادیتا، اعشاری).</param>
/// <param name="GroupWeightGrams">وزن authoritative یا تهی.</param>
/// <param name="PhysicalItemCount">تعداد فیزیکی authoritative یا تهی.</param>
/// <param name="CartLineIds">عضوهای دقیق گروه.</param>
/// <param name="Candidates">نامزدهای همان گروه به همان ترتیب درخواست.</param>
public sealed record StorefrontCapturedGroupProjection(
    string GroupKey,
    Guid SellerPartyId,
    string Currency,
    decimal FinalGoodsAmountAfterDiscountAndTax,
    decimal QuantityTotal,
    long? GroupWeightGrams,
    decimal? PhysicalItemCount,
    IReadOnlyList<Guid> CartLineIds,
    IReadOnlyList<StorefrontCapturedCandidateProjection> Candidates);

/// <summary>
/// نتیجهٔ کامل یک ترکیب: هویت سبد/نسخه/ارز، لحظهٔ واحد سرور و گروه‌ها.
/// </summary>
/// <param name="CartId">سبد authoritative.</param>
/// <param name="CartVersion">نسخهٔ واقعی سبد که با نسخهٔ درخواستی سنجیده شد.</param>
/// <param name="Currency">ارز یگانهٔ سبد.</param>
/// <param name="EvaluatedAtUtc">همان یک خواندن ساعت سرور برای کل ترکیب.</param>
/// <param name="Groups">گروه‌ها به همان ترتیب درخواست.</param>
public sealed record StorefrontCapturedGroupProjectionResult(
    Guid CartId,
    int CartVersion,
    string Currency,
    DateTimeOffset EvaluatedAtUtc,
    IReadOnlyList<StorefrontCapturedGroupProjection> Groups);

/// <summary>
/// کدهای دلیل پایدار لایهٔ ترکیب Order. کدهای مالک Shipping دست‌نخورده عبور می‌کنند.
/// </summary>
public static class StorefrontCapturedGroupProjectionReasons
{
    /// <summary>برنامهٔ گروه/نامزد ناقص یا تکراری است؛ هیچ فهرست کوچک‌شده‌ای پذیرفته نمی‌شود.</summary>
    public const string PlanInvalid = "storefront.captured_group.plan_invalid";

    /// <summary>فکت‌های برگشتی با سبد/نسخه/گروه‌های درخواستی نمی‌خوانند.</summary>
    public const string FactsMismatch = "storefront.captured_group.facts_mismatch";

    /// <summary>پاسخ مالک به این نامزد/بازبینی/ارز/سرویس متعهد نیست؛ به‌عنوان قیمت پذیرفته نمی‌شود.</summary>
    public const string BindingMismatch = "storefront.captured_group.binding_mismatch";

    /// <summary>پرس‌وجوی مالک با شکست تجاری برگشت (بدون قیمت).</summary>
    public const string QuoteFailed = "storefront.captured_group.quote_failed";

    /// <summary>نتیجهٔ قیمت‌دار، برنامهٔ دریافت capture‌شده ندارد.</summary>
    public const string ReceivingWeekMissing = "storefront.captured_group.receiving_week_missing";

    /// <summary>منطقهٔ زمانی ذخیره‌شده قابل resolve نیست.</summary>
    public const string ReceivingTimeZoneInvalid = "storefront.captured_group.receiving_timezone_invalid";

    /// <summary>برنامهٔ هفتگی ذخیره‌شده ناقص/تکراری/خارج از محدوده/بازهٔ معکوس است.</summary>
    public const string ReceivingWeekInvalid = "storefront.captured_group.receiving_week_invalid";

    /// <summary>کمینهٔ روزهای تحویل ذخیره نشده یا خارج از محدودهٔ محاسبه است.</summary>
    public const string MinimumDeliveryDaysInvalid = "storefront.captured_group.minimum_delivery_days_invalid";

    /// <summary>تعداد تاریخ‌های درخواستی خارج از محدودهٔ صریح تقویم است.</summary>
    public const string AvailableDateCountInvalid = "storefront.captured_group.available_date_count_invalid";

    /// <summary>هیچ روز کاری در هفته نیست؛ تاریخ قابل ارائه وجود ندارد.</summary>
    public const string NoWorkingDays = "storefront.captured_group.no_working_days";
}
