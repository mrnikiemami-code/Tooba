namespace Tooba.Order.Application.Storefront.Models;

/// <summary>
/// یک تاریخ قابل ارائه در پیش‌نمایش capture‌شده، با برچسب محلی و <b>بازهٔ همان روز</b>.
/// </summary>
/// <remarks>
/// برچسب‌ها از همان قالب‌بندی محلی موجود ساخته می‌شوند؛ هرگز ارقام گرگوری خام به‌جای تاریخ جلالی
/// نمایش داده نمی‌شود. بازه متعلق به همین تاریخ است و از هیچ اجتماع/میانگین هفته ساخته نمی‌شود.
/// </remarks>
/// <param name="Date">تاریخ ISO میلادی؛ شناسهٔ پایدار مقدار.</param>
/// <param name="Label">برچسب محلی روز (امروز/فردا/…).</param>
/// <param name="SubLabel">تاریخ محلی عددی.</param>
/// <param name="IntervalText">بازهٔ همان روز به شکل <c>HH:mm–HH:mm</c> یا تهی.</param>
/// <param name="IsEarliest">آیا زودترین تاریخ مجاز است.</param>
public sealed record StorefrontCapturedDateSlotView(
    string Date,
    string Label,
    string SubLabel,
    string? IntervalText,
    bool IsEarliest);

/// <summary>
/// یک روش پیشنهادی از یک <b>گروه ارسال capture‌شده</b>؛ همهٔ مقادیر از بازبینی تغییرناپذیر انتخاب‌شده
/// و فکت مالی همان گروه آمده‌اند.
/// </summary>
/// <remarks>
/// <para>
/// <b>Exact text money.</b> مبالغ به‌صورت <b>متن اعشاری دقیق</b> منتقل می‌شوند تا هیچ تبدیل
/// <c>Number</c>/شناور کلاینت و هیچ گردکردن JSON روی پول رخ ندهد؛ ارز همیشه از سرور است و هرگز در
/// کلاینت حدس زده نمی‌شود.
/// </para>
/// <para>
/// مبلغ ناشناخته/قیمت‌داده‌نشده <c>null</c> است و هرگز «۰» نمی‌شود؛ صفر صریح فقط وقتی می‌آید که
/// مالک واقعاً صفر اعلام کرده باشد. کرایه و بسته‌بندی جدا می‌مانند (هیچ جمع‌کردن/گِردکردن/تبدیل ارز
/// انجام نمی‌شود) و «قیمت‌دار» به‌هیچ‌وجه به معنی فعال‌بودن روش یا مجازبودن مشتری نیست.
/// </para>
/// </remarks>
/// <param name="MethodLabel">برچسب ذخیره‌شدهٔ همان capture به زبان محتوا (فقط نمایش).</param>
/// <param name="ServiceCode">کد سرویس ذخیره‌شده.</param>
/// <param name="OptionCode">کد گزینهٔ ذخیره‌شده یا تهی.</param>
/// <param name="FreightAmount">کرایهٔ همان ردیف به متن اعشاری دقیق یا تهی.</param>
/// <param name="PackagingAmount">بسته‌بندی همان ردیف به متن اعشاری دقیق یا تهی.</param>
/// <param name="PayableAtCheckoutAmount">سهم پرداخت در تسویه به متن دقیق یا تهی.</param>
/// <param name="CarrierCollectAmount">سهم پرداخت به حامل به متن دقیق یا تهی.</param>
/// <param name="Currency">ارز گروه از سرور یا تهی.</param>
/// <param name="EarliestEligibleDate">زودترین تاریخ مجاز یا تهی.</param>
/// <param name="AvailableDates">تاریخ‌های مجاز با بازهٔ همان روز.</param>
/// <param name="UnavailableReason">کد پایدار نبود تاریخ یا تهی.</param>
public sealed record StorefrontCapturedPreviewMethodView(
    string MethodLabel,
    string ServiceCode,
    string? OptionCode,
    string? FreightAmount,
    string? PackagingAmount,
    string? PayableAtCheckoutAmount,
    string? CarrierCollectAmount,
    string? Currency,
    string? EarliestEligibleDate,
    IReadOnlyList<StorefrontCapturedDateSlotView> AvailableDates,
    string? UnavailableReason,
    /// <summary>
    /// هویت تأیید همین روش (TB-232): متن دقیق snapshot انتخاب‌شده یا تهی. فقط برای مقایسهٔ
    /// «همان چیزی که دیدم» است؛ هیچ‌گاه مبنای قیمت/مالکیت نیست.
    /// </summary>
    string? ConfirmationSnapshotId = null,
    /// <summary>بازبینی دقیق مبدأ به‌صورت متن اعشاری (بدون گردکردن).</summary>
    string? ConfirmationSourceRevision = null,
    /// <summary>نسخهٔ انتخاب/اشاره‌گر به‌صورت متن اعشاری (بدون گردکردن).</summary>
    string? ConfirmationSelectionVersion = null,
    /// <summary>گروه ناحیهٔ تطبیق‌داده‌شدهٔ همان ردیف یا تهی.</summary>
    string? ConfirmationAreaGroupId = null,
    /// <summary>ردیف تعرفهٔ تطبیق‌داده‌شدهٔ همان روش یا تهی.</summary>
    string? ConfirmationRateRowId = null);

/// <summary>
/// پیش‌نمایش یک گروه ارسال capture‌شده با روش‌های <b>همان گروه</b>.
/// </summary>
/// <remarks>
/// گروه بدون مسئولیت/بدون نامزد حذف نمی‌شود: با <see cref="UnavailableReason"/> صریح می‌ماند تا مشتری
/// ببیند گروهی هست که هنوز قابل ارسال نیست. هیچ مبلغ صفر/رایگان ساختگی جای آن نمی‌نشیند؛ مبلغ کالای
/// ارزیابی‌نشده <c>null</c> است.
/// </remarks>
/// <param name="GroupKey">کلید پایدار سرور همان گروه (متفاوت از شناسهٔ گروه ناحیه).</param>
/// <param name="TypeLabel">برچسب نوع ارسال واقعی از کاتالوگ DB یا تهی.</param>
/// <param name="SellerPartyId">فروشندهٔ واقعی همان گروه.</param>
/// <param name="CartLineIds">خطوط دقیق همان گروه.</param>
/// <param name="FinalGoodsAmountAfterDiscountAndTax">
/// مبلغ کالای همان گروه پس از تخفیف و مالیات به متن اعشاری دقیق، یا <c>null</c> وقتی هنوز اثبات نشده.
/// </param>
/// <param name="Methods">روش‌های قیمت‌دار همان گروه.</param>
/// <param name="UnavailableReason">کد پایدار در دسترس‌نبودن کل گروه یا تهی.</param>
public sealed record StorefrontCapturedPreviewGroupView(
    string GroupKey,
    string? TypeLabel,
    Guid SellerPartyId,
    IReadOnlyList<Guid> CartLineIds,
    string? FinalGoodsAmountAfterDiscountAndTax,
    IReadOnlyList<StorefrontCapturedPreviewMethodView> Methods,
    string? UnavailableReason);

/// <summary>
/// پیش‌نمایش خواندنی و <b>غیرقابل ثبت</b> گروه‌های ارسال capture‌شده برای یک سبد.
/// </summary>
/// <remarks>
/// <see cref="SelectionVersion"/> و <see cref="SnapshotId"/> فقط provenance سرور برای برش بعدی
/// (parity/ثبت) هستند و در رابط مشتری به‌عنوان متن نمایش داده نمی‌شوند. این شیء هیچ روش یا مبلغی را
/// به فهرست/قیمت قدیمی تزریق نمی‌کند.
/// </remarks>
/// <param name="Enabled">آیا مسیر خواندنی فعال بود.</param>
/// <param name="Currency">ارز یکتای سبد یا تهی وقتی اثبات نشده.</param>
/// <param name="EvaluatedAtUtc">لحظهٔ واحد سرور برای همهٔ گروه‌ها.</param>
/// <param name="Groups">گروه‌ها.</param>
/// <param name="SelectionVersion">نسخهٔ اشاره‌گر انتخاب‌شده یا تهی.</param>
/// <param name="SnapshotId">capture انتخاب‌شده یا تهی.</param>
/// <param name="NoticeReason">کد پایدار توضیح وضعیت خواندنی یا تهی.</param>
/// <param name="SelectionRevision">
/// TB-237-R1: نسخهٔ <b>ذخیره‌شدهٔ</b> ریشهٔ انتخاب به‌صورت متن اعشاری دقیق — همان مقداری که CAS بعدی
/// باید بفرستد. `"0"` یعنی سرور اثبات کرده هیچ انتخاب capture‌شده‌ای ذخیره نشده است؛ `null` یعنی این
/// هویت اثبات نشده (نمای غیرفعال/نامعتبر) و هرگز به «0» تفسیر نمی‌شود.
/// </param>
/// <param name="SelectionEnabled">
/// TB-237-R1: توانایی واقعی «نوشتن انتخاب» در همین سرور. فقط برای نمایش صادقانهٔ وضعیت در UI است؛
/// هیچ مجوزی از آن ساخته نمی‌شود و مرورگر با آن چیزی را معتبر نمی‌کند.
/// </param>
/// <param name="CheckoutEnabled">
/// TB-237-R1: توانایی واقعی «ثبت نهایی» (همان دروازهٔ چهارشرطی). مثل بالا فقط نمایشی است.
/// </param>
public sealed record StorefrontCapturedPreviewView(
    bool Enabled,
    string? Currency,
    DateTimeOffset EvaluatedAtUtc,
    IReadOnlyList<StorefrontCapturedPreviewGroupView> Groups,
    long? SelectionVersion,
    Guid? SnapshotId,
    string? NoticeReason,
    string? SelectionRevision = null,
    bool SelectionEnabled = false,
    bool CheckoutEnabled = false);
