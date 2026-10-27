namespace Tooba.Order.Application.Storefront.Models;

/// <summary>
/// یک روش پیشنهادی از یک <b>گروه ارسال capture‌شده</b>؛ همهٔ مقادیر از بازبینی تغییرناپذیر انتخاب‌شده
/// و فکت مالی همان گروه آمده‌اند.
/// </summary>
/// <remarks>
/// کرایه و بسته‌بندی جدا می‌مانند (هیچ جمع‌کردن/گِردکردن/تبدیل ارز در سرور یا کلاینت انجام نمی‌شود) و
/// «قیمت‌دار» به‌هیچ‌وجه به معنی فعال‌بودن روش یا مجازبودن مشتری نیست.
/// </remarks>
/// <param name="MethodLabel">برچسب ذخیره‌شدهٔ همان capture به زبان محتوا.</param>
/// <param name="ServiceCode">کد سرویس ذخیره‌شده.</param>
/// <param name="OptionCode">کد گزینهٔ ذخیره‌شده یا تهی.</param>
/// <param name="FreightAmount">کرایهٔ همان ردیف.</param>
/// <param name="PackagingAmount">بسته‌بندی همان ردیف.</param>
/// <param name="PayableAtCheckoutAmount">سهم پرداخت در تسویه.</param>
/// <param name="CarrierCollectAmount">سهم پرداخت به حامل.</param>
/// <param name="Currency">ارز گروه.</param>
/// <param name="EarliestEligibleDate">زودترین تاریخ مجاز یا تهی.</param>
/// <param name="AvailableDates">تاریخ‌های مجاز با بازهٔ همان روز.</param>
/// <param name="UnavailableReason">کد پایدار نبود تاریخ یا تهی.</param>
public sealed record StorefrontCapturedPreviewMethodView(
    string MethodLabel,
    string ServiceCode,
    string? OptionCode,
    decimal? FreightAmount,
    decimal? PackagingAmount,
    decimal? PayableAtCheckoutAmount,
    decimal? CarrierCollectAmount,
    string Currency,
    string? EarliestEligibleDate,
    IReadOnlyList<StorefrontDeliveryDateOption> AvailableDates,
    string? UnavailableReason);

/// <summary>
/// پیش‌نمایش یک گروه ارسال capture‌شده با روش‌های <b>همان گروه</b>.
/// </summary>
/// <remarks>
/// گروه بدون مسئولیت/بدون نامزد حذف نمی‌شود: با <see cref="UnavailableReason"/> صریح می‌ماند تا مشتری
/// ببیند گروهی هست که هنوز قابل ارسال نیست. هیچ مبلغ صفر/رایگان ساختگی جای آن نمی‌نشیند.
/// </remarks>
/// <param name="GroupKey">کلید پایدار سرور همان گروه (متفاوت از شناسهٔ گروه ناحیه).</param>
/// <param name="TypeLabel">برچسب نوع ارسال واقعی از کاتالوگ DB یا تهی.</param>
/// <param name="SellerPartyId">فروشندهٔ واقعی همان گروه.</param>
/// <param name="CartLineIds">خطوط دقیق همان گروه.</param>
/// <param name="FinalGoodsAmountAfterDiscountAndTax">مبلغ کالای همان گروه پس از تخفیف و مالیات.</param>
/// <param name="Methods">روش‌های قیمت‌دار همان گروه.</param>
/// <param name="UnavailableReason">کد پایدار در دسترس‌نبودن کل گروه یا تهی.</param>
public sealed record StorefrontCapturedPreviewGroupView(
    string GroupKey,
    string? TypeLabel,
    Guid SellerPartyId,
    IReadOnlyList<Guid> CartLineIds,
    decimal FinalGoodsAmountAfterDiscountAndTax,
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
/// <param name="Currency">ارز یکتای سبد.</param>
/// <param name="EvaluatedAtUtc">لحظهٔ واحد سرور برای همهٔ گروه‌ها.</param>
/// <param name="Groups">گروه‌ها.</param>
/// <param name="SelectionVersion">نسخهٔ اشاره‌گر انتخاب‌شده یا تهی.</param>
/// <param name="SnapshotId">capture انتخاب‌شده یا تهی.</param>
/// <param name="NoticeReason">کد پایدار توضیح وضعیت خواندنی یا تهی.</param>
public sealed record StorefrontCapturedPreviewView(
    bool Enabled,
    string Currency,
    DateTimeOffset EvaluatedAtUtc,
    IReadOnlyList<StorefrontCapturedPreviewGroupView> Groups,
    long? SelectionVersion,
    Guid? SnapshotId,
    string? NoticeReason);
