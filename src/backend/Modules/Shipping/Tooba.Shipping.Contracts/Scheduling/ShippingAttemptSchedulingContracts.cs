using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Shipping.Contracts.Scheduling;

/// <summary>
/// حکم قطعی خواندن <b>ورودی‌های زمان‌بندی یک تلاش فیزیکی جدید</b> از یک بازبینی capture‌شدهٔ دقیق.
/// </summary>
/// <remarks>
/// <para>
/// فقط <see cref="CapturedInputs"/> یک بدنهٔ قابل‌استفاده دارد؛ هر مقدار دیگر یعنی «هیچ ورودی‌ای اعلام
/// نمی‌شود» و مصرف‌کننده نباید از آن هیچ برنامه/موعد/تاریخی بسازد. هیچ مفهوم latest/current/active در
/// این خواندن وجود ندارد.
/// </para>
/// <para>
/// این خواندن یک <b>واقعیت تاریخی</b> را برمی‌گرداند و هیچ ادعایی دربارهٔ فعال بودن روش، صلاحیت مشتری،
/// پوشش مقصد یا امکان‌پذیری یک برنامهٔ جدید نمی‌سازد.
/// </para>
/// </remarks>
public enum ShippingAttemptSchedulingVerdict
{
    /// <summary>بازبینی دقیق و همان گروه/ردیف درخواستی خوانده شد و ورودی‌های ذخیره‌شده اعلام می‌شوند.</summary>
    CapturedInputs = 0,

    /// <summary>هویت مالک درخواستی با زمینهٔ نصب/تجارت جاری نمی‌خواند؛ هیچ ردیفی خوانده نشد.</summary>
    OwnerScopeUnavailable = 1,

    /// <summary>بازبینی دقیق وجود ندارد، مالک دیگری است یا بدنهٔ آن ناسازگار است.</summary>
    CapturedRevisionUnavailable = 2,

    /// <summary>شناسهٔ capture اعلام‌شده توسط فراخوان با ردیف حل‌شده یکی نیست.</summary>
    SnapshotMismatch = 3,

    /// <summary>کد خدمت/گزینهٔ درخواستی با مقدار ذخیره‌شده در همان capture یکی نیست.</summary>
    ServiceOptionMismatch = 4,

    /// <summary>گروه درخواستی داخل همین بازبینی capture‌شده وجود ندارد.</summary>
    GroupUnavailable = 5,

    /// <summary>بیش از یک گروه با همان شناسه در بدنهٔ ذخیره‌شده هست؛ ابهام حل نمی‌شود.</summary>
    GroupAmbiguous = 6,

    /// <summary>ردیف نرخ درخواستی داخل همان گروه وجود ندارد.</summary>
    RateRowUnavailable = 7,

    /// <summary>بیش از یک ردیف با همان شناسه داخل همان گروه هست؛ هیچ ردیفی «اولین تطابق» نمی‌شود.</summary>
    RateRowAmbiguous = 8,
}

/// <summary>کدهای دلیل پایدار برای خواندن ورودی‌های زمان‌بندی یک تلاش جدید.</summary>
public static class ShippingAttemptSchedulingReasonCodes
{
    /// <summary>ورودی‌های ذخیره‌شدهٔ همان بازبینی دقیق اعلام شدند.</summary>
    public const string CapturedInputs = "shipping.attempt_scheduling.captured_inputs";

    /// <summary>هویت مالک قابل استفاده نیست.</summary>
    public const string OwnerScopeUnavailable = "shipping.attempt_scheduling.owner_scope_unavailable";

    /// <summary>بازبینی دقیق در دسترس/معتبر نیست.</summary>
    public const string CapturedRevisionUnavailable = "shipping.attempt_scheduling.captured_revision_unavailable";

    /// <summary>شناسهٔ capture با ردیف حل‌شده نمی‌خواند.</summary>
    public const string SnapshotMismatch = "shipping.attempt_scheduling.snapshot_mismatch";

    /// <summary>خدمت/گزینهٔ درخواستی با capture نمی‌خواند.</summary>
    public const string ServiceOptionMismatch = "shipping.attempt_scheduling.service_option_mismatch";

    /// <summary>گروه درخواستی در این capture نیست.</summary>
    public const string GroupUnavailable = "shipping.attempt_scheduling.group_unavailable";

    /// <summary>شناسهٔ گروه در این capture تکراری است.</summary>
    public const string GroupAmbiguous = "shipping.attempt_scheduling.group_ambiguous";

    /// <summary>ردیف درخواستی در این گروه نیست.</summary>
    public const string RateRowUnavailable = "shipping.attempt_scheduling.rate_row_unavailable";

    /// <summary>شناسهٔ ردیف در این گروه تکراری است.</summary>
    public const string RateRowAmbiguous = "shipping.attempt_scheduling.rate_row_ambiguous";
}

/// <summary>
/// کدهای پایدار «ورودی ذخیره‌نشده» برای یک بازبینی تاریخی که فقط بخشی از ورودی‌ها را دارد.
/// </summary>
/// <remarks>
/// این کدها صرفاً <b>غیبت یک واقعیت ذخیره‌شده</b> را می‌گویند، نه شکست یک برنامه: ساخت/رد یک برنامهٔ
/// واقعی تصمیم همان مرجع زمان‌بندی موجود است، نه این خواندن.
/// </remarks>
public static class ShippingAttemptSchedulingInputCodes
{
    /// <summary>تنظیمات روش در همان capture ذخیره نشده است.</summary>
    public const string MethodSettings = "method_settings";

    /// <summary>جفت زمان آماده‌سازی/انتقال در همان capture ذخیره نشده است.</summary>
    public const string PreparationHandoffMinutes = "preparation_handoff_minutes";

    /// <summary>معنای صریح زمان پردازش (هفتهٔ کاری/پیوسته) تنظیم نشده است.</summary>
    public const string ProcessingMode = "processing_mode";

    /// <summary>هفتهٔ کاری پردازش انبار در همان capture ذخیره نشده است (قالب‌های پیش از ۴).</summary>
    public const string ProcessingWeek = "processing_week";

    /// <summary>تقویم پذیرش حامل در همان capture ذخیره نشده است (قالب‌های پیش از ۳).</summary>
    public const string CarrierWeek = "carrier_week";

    /// <summary>برنامهٔ هفتگی دریافت مشتری در همان capture ذخیره نشده است.</summary>
    public const string ReceivingWeek = "receiving_week";
}

/// <summary>
/// برنامهٔ هفتگی <b>دریافت مشتری</b> همان‌گونه که در capture ذخیره شده بود، همراه با مدت نوبت.
/// </summary>
/// <remarks>
/// روزها با همان معنا و ترتیب ذخیره‌شده اعلام می‌شوند: پرچم کاری/تعطیل، روز هفتهٔ ISO و ساعت‌های محلی
/// <c>[OpensAt, ClosesAt)</c>. <see cref="SlotDurationMinutes"/> تهی یعنی «پنجرهٔ کامل روز» و هرگز به
/// یک عدد حدسی تبدیل نمی‌شود.
/// </remarks>
/// <param name="TimeZoneId">شناسهٔ منطقهٔ زمانی ذخیره‌شده.</param>
/// <param name="Days">روزهای ذخیره‌شده.</param>
/// <param name="SlotDurationMinutes">مدت نوبت ذخیره‌شده یا تهی برای پنجرهٔ کامل روز.</param>
public sealed record ShippingAttemptReceivingWeekView(
    string TimeZoneId,
    IReadOnlyList<ShippingSchedulingDayView> Days,
    int? SlotDurationMinutes);

/// <summary>
/// درخواست خواندن ورودی‌های زمان‌بندی یک تلاش فیزیکی جدید از <b>یک</b> بازبینی capture‌شدهٔ دقیق.
/// </summary>
/// <remarks>
/// <para>
/// <b>هویت داده است، نه مجوز.</b> این کلید فقط می‌گوید کدام مالک/پیش‌نویس/بازبینی پرسیده می‌شود؛
/// انطباق آن با زمینهٔ نصب/تجارت جاری داخل Shipping سنجیده می‌شود. فراخوان آیندهٔ Order باید این کلید
/// را از شاهد پذیرفته‌شدهٔ خودش بسازد، نه از مرورگر.
/// </para>
/// <para>
/// <b>گروه/ردیف دقیق اجباری است.</b> کرایه/بسته‌بندی و همچنین کمینه/بیشینهٔ روزهای تحویل داخل
/// <c>Groups[].RateRows[]</c> ذخیره می‌شوند، پس بدون شناسهٔ همان گروه و همان ردیف نمی‌توان ورودی حمل را
/// بازیابی کرد. هیچ ردیفی از روی بازه/مبلغ/جغرافیا/ترتیب آرایه استنتاج نمی‌شود.
/// </para>
/// <para>
/// <b>هیچ اندازه/مقصد/ارزی لازم نیست.</b> این خواندن موتور قیمت را صدا نمی‌زند و هیچ انتخاب نرخی انجام
/// نمی‌دهد؛ فقط ورودی‌های ذخیره‌شدهٔ همان ردیفِ پذیرفته‌شده را می‌خواند.
/// </para>
/// </remarks>
/// <param name="OwnerKind">گونهٔ مالک resolve‌شده.</param>
/// <param name="OwnerTenantId">tenant مالک Store یا تهی.</param>
/// <param name="OwnerSellerPartyId">شناسهٔ فروشندهٔ مالک Seller یا تهی.</param>
/// <param name="DraftId">شناسهٔ پیش‌نویس capture‌شده.</param>
/// <param name="SourceRevision">شمارهٔ بازبینی مبدأ همان capture.</param>
/// <param name="ExpectedServiceCode">کد خدمت موردانتظار همان روش.</param>
/// <param name="ExpectedOptionCode">گزینهٔ موردانتظار یا تهی.</param>
/// <param name="GroupId">شناسهٔ گروه پذیرفته‌شده داخل همان capture.</param>
/// <param name="RateRowId">شناسهٔ ردیف نرخ پذیرفته‌شده داخل همان گروه.</param>
/// <param name="ExpectedSnapshotId">شناسهٔ capture موردانتظار یا تهی وقتی فراخوان آن را bind نکرده است.</param>
public sealed record ShippingAttemptSchedulingLookupRequest(
    ShippingCapturedQuoteOwnerKind OwnerKind,
    string? OwnerTenantId,
    Guid? OwnerSellerPartyId,
    Guid DraftId,
    long SourceRevision,
    string ExpectedServiceCode,
    string? ExpectedOptionCode,
    Guid GroupId,
    Guid RateRowId,
    Guid? ExpectedSnapshotId = null);

/// <summary>
/// ورودی‌های <b>ذخیره‌شده</b> برای زمان‌بندی یک تلاش فیزیکی جدید، همراه با provenance کامل.
/// </summary>
/// <remarks>
/// <para>
/// <b>فقط Contracts.</b> هیچ نوع Domain/Application/ماندگاری اینجا نیست و هیچ موتور زمان‌بندی دومی ساخته
/// نشده است: مصرف‌کننده همین مقادیر را به مرجع موجود <see cref="ShippingDispatchSchedulingAuthority"/>
/// می‌دهد.
/// </para>
/// <para>
/// <b>کامل‌بودن، ادعای امکان‌پذیری نیست.</b> <see cref="MissingInputs"/> فقط می‌گوید کدام واقعیت در همان
/// capture ذخیره نشده است؛ <see cref="HasCompleteInputs"/> یعنی «همهٔ ورودی‌هایی که این درز اعلام می‌کند
/// موجودند» و هیچ معنای «برنامه ساختنی است» ندارد. یک قالب تاریخی می‌تواند معتبر خوانده شود و در همان
/// حال ناقص باشد.
/// </para>
/// <para>
/// <b>هیچ مقدار پیش‌فرضی.</b> دقیقهٔ آماده‌سازی/انتقال، مدت نوبت، روز تعطیل و منطقهٔ زمانی عیناً همان
/// مقدار ذخیره‌شده‌اند؛ غیبت هرکدام <c>null</c> می‌ماند و به صفر/۲۴ساعته/هفتهٔ ساختگی تبدیل نمی‌شود.
/// </para>
/// </remarks>
/// <param name="OwnerKind">گونهٔ مالک ذخیره‌شده.</param>
/// <param name="OwnerTenantId">tenant ذخیره‌شده یا تهی.</param>
/// <param name="OwnerSellerPartyId">فروشندهٔ ذخیره‌شده یا تهی.</param>
/// <param name="SnapshotId">شناسهٔ ردیف capture خوانده‌شده.</param>
/// <param name="DraftId">پیش‌نویس همان capture.</param>
/// <param name="SourceRevision">بازبینی همان capture.</param>
/// <param name="FormatVersion">نسخهٔ قالب همان capture.</param>
/// <param name="ServiceCode">کد خدمت ذخیره‌شده.</param>
/// <param name="OptionCode">گزینهٔ ذخیره‌شده یا تهی.</param>
/// <param name="GroupId">گروه همان ردیف ذخیره‌شده.</param>
/// <param name="RateRowId">ردیف نرخ همان گروه.</param>
/// <param name="MinDeliveryDays">کمینهٔ روزهای تحویل ذخیره‌شده در همان ردیف.</param>
/// <param name="MaxDeliveryDays">بیشینهٔ روزهای تحویل ذخیره‌شده در همان ردیف.</param>
/// <param name="PreparationMinutes">زمان آماده‌سازی ذخیره‌شده یا تهی.</param>
/// <param name="HandoffMinutes">زمان انتقال/تحویل به حامل ذخیره‌شده یا تهی.</param>
/// <param name="ProcessingMode">معنای صریح زمان پردازش ذخیره‌شده.</param>
/// <param name="ReceivingWeek">هفتهٔ دریافت مشتری ذخیره‌شده یا تهی.</param>
/// <param name="CarrierWeek">هفتهٔ پذیرش حامل ذخیره‌شده یا تهی.</param>
/// <param name="ProcessingWeek">هفتهٔ کاری پردازش ذخیره‌شده یا تهی.</param>
/// <param name="MissingInputs">کدهای پایدار ورودی‌های ذخیره‌نشده.</param>
public sealed record ShippingAttemptSchedulingInputs(
    ShippingCapturedQuoteOwnerKind OwnerKind,
    string? OwnerTenantId,
    Guid? OwnerSellerPartyId,
    Guid SnapshotId,
    Guid DraftId,
    long SourceRevision,
    int FormatVersion,
    string ServiceCode,
    string? OptionCode,
    Guid GroupId,
    Guid RateRowId,
    int MinDeliveryDays,
    int MaxDeliveryDays,
    int? PreparationMinutes,
    int? HandoffMinutes,
    ShippingAuthoringPreparationProcessingMode ProcessingMode,
    ShippingAttemptReceivingWeekView? ReceivingWeek,
    ShippingSchedulingWeekView? CarrierWeek,
    ShippingSchedulingWeekView? ProcessingWeek,
    IReadOnlyList<string> MissingInputs)
{
    /// <summary>
    /// آیا همهٔ ورودی‌هایی که این درز اعلام می‌کند ذخیره شده‌اند. این یک ادعای امکان‌پذیری
    /// برنامه/پوشش/فعال‌سازی نیست.
    /// </summary>
    public bool HasCompleteInputs => MissingInputs.Count == 0;
}

/// <summary>
/// نتیجهٔ قطعی خواندن ورودی‌های زمان‌بندی تلاش جدید؛ در هر حکمی جز
/// <see cref="ShippingAttemptSchedulingVerdict.CapturedInputs"/> هیچ بدنه‌ای وجود ندارد.
/// </summary>
/// <param name="Verdict">حکم خواندن.</param>
/// <param name="ReasonCode">کد دلیل پایدار.</param>
/// <param name="Inputs">ورودی‌های ذخیره‌شده یا تهی در امتناع.</param>
public sealed record ShippingAttemptSchedulingResult(
    ShippingAttemptSchedulingVerdict Verdict,
    string ReasonCode,
    ShippingAttemptSchedulingInputs? Inputs = null)
{
    /// <summary>آیا این نتیجه یک بدنهٔ قابل‌استفاده دارد.</summary>
    public bool HasInputs
        => Verdict == ShippingAttemptSchedulingVerdict.CapturedInputs && Inputs is not null;

    /// <summary>یک امتناع بدون هیچ بدنه‌ای.</summary>
    /// <param name="verdict">حکم امتناع.</param>
    /// <param name="reasonCode">کد دلیل پایدار.</param>
    public static ShippingAttemptSchedulingResult Refused(
        ShippingAttemptSchedulingVerdict verdict,
        string reasonCode)
        => new(verdict, reasonCode);
}

/// <summary>
/// تنها درز فقط‌خواندنی برای خواندن ورودی‌های زمان‌بندی یک تلاش فیزیکی جدید از یک بازبینی
/// <b>capture‌شدهٔ دقیق</b>.
/// </summary>
/// <remarks>
/// <para>
/// <b>چرا این درز وجود دارد.</b> شاهد پذیرفته‌شده در Order خروجی‌های برنامه (پنجره/منطقه/روزهای حمل/
/// موعدها) را نگه می‌دارد ولی <em>شکل هفته‌ها</em> و سیاست زمانی را ندارد؛ بازیابی آن‌ها از مسیر قیمت
/// نیازمند مقصد/اندازه/انتخاب نرخ است. این خواندن همان ورودی‌ها را بدون هیچ‌یک از آن‌ها برمی‌گرداند.
/// </para>
/// <para>
/// <b>هیچ نوشتنی.</b> این خواندن یک ردیف تغییرناپذیر را می‌خواند؛ نه capture می‌سازد، نه پیش‌نویس/تنظیمات
/// جاری را جایگزین capture می‌کند و نه انتخاب نرخ/provider/Geography را صدا می‌زند.
/// </para>
/// <para>
/// <b>استثنا و لغو عیناً منتشر می‌شوند.</b> فقط بدنهٔ ناسازگار همان ردیف (خطای معنایی canonical خواندن
/// payload) به حکم صریح تبدیل می‌شود؛ هیچ شکست بیرونی به «نیست» تبدیل نمی‌شود.
/// </para>
/// </remarks>
public interface IShippingAttemptSchedulingLookup
{
    /// <summary>ورودی‌های ذخیره‌شدهٔ همان بازبینی دقیق و همان گروه/ردیف را می‌خواند.</summary>
    /// <param name="request">درخواست صریح سرور.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    /// <returns>نتیجه؛ هرگز null.</returns>
    Task<ShippingAttemptSchedulingResult> LookupAsync(
        ShippingAttemptSchedulingLookupRequest request,
        CancellationToken cancellationToken);
}
