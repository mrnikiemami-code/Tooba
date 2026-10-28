using Tooba.Shipping.Contracts;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Packaging;

namespace Tooba.Shipping.Application.Authoring.Models;

/// <summary>تنظیمات روش در سند ویرایشگر: دو انتخاب مستقل و صریح.</summary>
/// <param name="FreightPaymentMode">زمان پرداخت کرایه.</param>
/// <param name="CalculationMode">حالت محاسبه.</param>
public sealed record ShippingAuthoringEditorSettingsItem(
    ShippingFreightPayerMode FreightPaymentMode,
    ShippingAuthoringCalculationMode CalculationMode);

/// <summary>یک روز برنامه در سند ویرایشگر.</summary>
/// <param name="Weekday">روز هفتهٔ ISO.</param>
/// <param name="IsWorking">پرچم صریح کاری/تعطیل.</param>
/// <param name="StartTime">ساعت شروع یا تهی.</param>
/// <param name="EndTime">ساعت پایان یا تهی.</param>
public sealed record ShippingAuthoringEditorScheduleDayItem(
    int Weekday,
    bool IsWorking,
    TimeOnly? StartTime,
    TimeOnly? EndTime);

/// <summary>برنامهٔ هفتگی ذخیره‌شده در سند ویرایشگر.</summary>
/// <param name="TimeZoneId">منطقهٔ زمانی.</param>
/// <param name="Days">هفت روز.</param>
/// <param name="SlotDurationMinutes">TB-241: مدت اختیاری هر نوبت تحویل به دقیقه یا تهی.</param>
public sealed record ShippingAuthoringEditorScheduleItem(
    string TimeZoneId,
    IReadOnlyList<ShippingAuthoringEditorScheduleDayItem> Days,
    int? SlotDurationMinutes = null);

/// <summary>عنوان ذخیره‌شدهٔ یک گروه برای یک زبان (متن خام، بدون fallback).</summary>
/// <param name="LanguageId">شناسهٔ زبان.</param>
/// <param name="Title">عنوان ذخیره‌شده.</param>
public sealed record ShippingAuthoringEditorGroupTitleItem(Guid LanguageId, string Title);

/// <summary>
/// یک گروه کامل در سند ویرایشگر: هویت، ترتیب، ارز bind‌شده، انتخاب‌های جغرافیایی، ردیف‌ها و عنوان‌ها.
/// </summary>
/// <param name="GroupId">شناسهٔ پایدار گروه.</param>
/// <param name="Order">ترتیب صفرپایه.</param>
/// <param name="CurrencyCode">ارز ذخیره‌شده یا تهی.</param>
/// <param name="Areas">انتخاب‌های مقصد.</param>
/// <param name="Rows">ردیف‌های نرخ.</param>
/// <param name="Titles">عنوان‌های ذخیره‌شده.</param>
public sealed record ShippingAuthoringEditorGroupItem(
    Guid GroupId,
    int Order,
    string? CurrencyCode,
    IReadOnlyList<ShippingAuthoringSelectedAreaItem> Areas,
    IReadOnlyList<ShippingAuthoringRateRowItem> Rows,
    IReadOnlyList<ShippingAuthoringEditorGroupTitleItem> Titles);

/// <summary>
/// سند کامل نویسندگی یک پیش‌نویس موجود: یک snapshot ریشه با همهٔ بخش‌های authored، بازبینی و زمان،
/// انتخاب نوع (با حل واقعی کاتالوگ) و registry زبان فعال.
/// </summary>
/// <param name="DraftId">شناسهٔ پیش‌نویس.</param>
/// <param name="Owner">نمای مالک مجاز.</param>
/// <param name="ServiceCode">کد خدمت.</param>
/// <param name="OptionCode">گزینهٔ اختیاری.</param>
/// <param name="Revision">بازبینی ریشه.</param>
/// <param name="CreatedAt">زمان ایجاد.</param>
/// <param name="UpdatedAt">زمان آخرین تغییر.</param>
/// <param name="Type">انتخاب نوع با حل واقعی.</param>
/// <param name="Settings">تنظیمات روش یا تهی (تنظیم‌نشده).</param>
/// <param name="Presentation">متن روش و registry زبان.</param>
/// <param name="WeeklySchedule">برنامهٔ هفتگی یا تهی (پیکربندی‌نشده).</param>
/// <param name="RegionGroups">گروه‌های منطقه‌ای به ترتیب صریح.</param>
public sealed record ShippingAuthoringEditorDocumentItem(
    Guid DraftId,
    ShippingAuthoringOwnerView Owner,
    string ServiceCode,
    string? OptionCode,
    long Revision,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    ShippingAuthoringDraftTypeAssignmentItem Type,
    ShippingAuthoringEditorSettingsItem? Settings,
    ShippingAuthoringPresentationItem Presentation,
    ShippingAuthoringEditorScheduleItem? WeeklySchedule,
    IReadOnlyList<ShippingAuthoringEditorGroupItem> RegionGroups)
{
    /// <summary>
    /// فکت‌های افزودنی مقصد: یک ورودی برای هر کلید <b>متمایز</b> (نوع، شناسه) از همان درخت
    /// انتخاب‌شده، با برچسب محلی‌شدهٔ زبان پیش‌فرض محتوا.
    /// </summary>
    /// <remarks>
    /// این فیلد <b>مرجعیت</b> ندارد و فقط خواندنی است: نوشتن همچنان جغرافیای واقعی را اعتبارسنجی
    /// می‌کند و هیچ فکت hydrate‌شده‌ای از مرورگر پذیرفته نمی‌شود. انتخاب خام در
    /// <see cref="RegionGroups"/> در هر حالت دست‌نخورده می‌ماند؛ حتی وقتی هویت <c>Unresolved</c> است.
    /// </remarks>
    public IReadOnlyList<ShippingAuthoringDestinationFactItem> DestinationFacts { get; init; } = [];
}
