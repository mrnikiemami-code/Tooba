using System.Text.Json.Serialization;
using Tooba.Shipping.Contracts.Packaging;

namespace Tooba.Shipping.Contracts.Authoring;

/// <summary>
/// عنوان یک گروه برای یک زبان در سند ویرایشگر. عنوان <c>null</c> فقط همان (گروه، زبان) را پاک می‌کند.
/// </summary>
/// <param name="LanguageId">شناسهٔ زبان محتوای فعال.</param>
/// <param name="Title">عنوان یا تهی برای پاک‌کردن.</param>
public sealed record ShippingAuthoringEditorGroupTitleWrite(
    Guid LanguageId,
    string? Title);

/// <summary>
/// یک گروه در سند ویرایشگر یک روش موجود: هویت پایدار اختیاری، انتخاب‌های جغرافیایی، ردیف‌های نرخ
/// همان گروه و عنوان‌های همان گروه. گروه تازه هیچ شناسه‌ای از کلاینت نمی‌گیرد.
/// </summary>
/// <remarks>
/// <para>
/// <b>مجموعهٔ کامل و مرتب.</b> ترتیب این فهرست همان ترتیب ذخیره‌شدهٔ گروه‌هاست؛ گروه موجودی که حذف
/// شود حذف می‌شود و هیچ گروهی از مالک دیگر دوباره وصل نمی‌شود.
/// </para>
/// <para>
/// <b>ردیف و عنوان تودرتو.</b> ردیف‌ها و عنوان‌ها زیر همین گروه می‌آیند تا یک گروه تازه در همان PUT
/// بتواند ردیف و عنوان داشته باشد، بدون هیچ GUID جعلی از سمت کلاینت. <c>null</c> برای هر مجموعه شکل
/// نامعتبر است؛ <c>[]</c> مجموعهٔ خالی معتبر است.
/// </para>
/// </remarks>
/// <param name="GroupId">شناسهٔ گروه موجود همین پیش‌نویس؛ تهی برای گروه تازه.</param>
/// <param name="Areas">مجموعهٔ کامل انتخاب‌های مقصد همین گروه.</param>
/// <param name="Rows">مجموعهٔ کامل ردیف‌های نرخ همین گروه.</param>
/// <param name="Titles">تغییرهای عنوان همین گروه؛ <c>[]</c> یعنی بدون تغییر.</param>
public sealed record ShippingAuthoringEditorGroupWrite(
    Guid? GroupId,
    IReadOnlyList<ShippingAuthoringSelectedAreaWrite> Areas,
    IReadOnlyList<ShippingAuthoringRateRowWrite> Rows,
    IReadOnlyList<ShippingAuthoringEditorGroupTitleWrite> Titles);

/// <summary>
/// برنامهٔ عملیاتی هفتگی در سند ویرایشگر: منطقهٔ زمانی صریح و دقیقاً هفت روز.
/// </summary>
/// <param name="TimeZoneId">شناسهٔ منطقهٔ زمانی.</param>
/// <param name="Days">هفت روز ISO.</param>
/// <param name="SlotDurationMinutes">TB-241: مدت اختیاری هر نوبت تحویل به دقیقه؛ تهی یعنی پنجرهٔ کامل روز.</param>
public sealed record ShippingAuthoringEditorScheduleWrite(
    string TimeZoneId,
    IReadOnlyList<ShippingAuthoringScheduleDayWrite> Days,
    int? SlotDurationMinutes = null);

/// <summary>
/// تنظیمات روش در سند ویرایشگر: زمان پرداخت کرایه و حالت محاسبه، دو انتخاب مستقل و صریح.
/// </summary>
/// <param name="FreightPaymentMode">زمان پرداخت کرایه.</param>
/// <param name="CalculationMode">حالت محاسبه.</param>
public sealed record ShippingAuthoringEditorSettingsWrite(
    ShippingFreightPayerMode? FreightPaymentMode,
    ShippingAuthoringCalculationMode? CalculationMode);

/// <summary>
/// بستهٔ اختیاری انتخاب نوع در سند ویرایشگر. حضور خود بسته یعنی «این بخش در سند است»؛ نبودِ بسته
/// یعنی «دست نزن».
/// </summary>
/// <remarks>
/// <b>غیبت ≠ پاک‌کردن.</b> <see cref="ShippingTypeId"/> با <see cref="JsonRequiredAttribute"/> علامت
/// خورده است: بستهٔ ارائه‌شده باید کلید را داشته باشد. <c>null</c> صریح یعنی پاک‌کردن، و شناسهٔ خالی
/// نامعتبر است.
/// </remarks>
public sealed record ShippingAuthoringEditorTypeWrite
{
    /// <summary>شناسهٔ تعریف نوع؛ <c>null</c> صریح یعنی پاک‌کردن.</summary>
    [JsonRequired]
    public Guid? ShippingTypeId { get; init; }
}

/// <summary>
/// بدنهٔ ذخیرهٔ اتمی سند ویرایشگر یک پیش‌نویس <b>موجود</b>: یک بازبینی مورد انتظار و یک سند مرکب.
/// </summary>
/// <remarks>
/// <para>
/// <b>مرکب، نه جایگزینی کور.</b> <see cref="RegionGroups"/> و <see cref="MethodTranslations"/> اجباری
/// هستند (خالی مجاز، <c>null</c>/غایب نامعتبر). <see cref="WeeklySchedule"/>،
/// <see cref="MethodSettings"/> و <see cref="TypeAssignment"/> اختیاری‌اند: غایب یا <c>null</c> یعنی
/// «همان مقدار ذخیره‌شده حفظ شود». هیچ قدرت پاک‌کردنی برای این سه بخش اضافه نمی‌شود.
/// </para>
/// <para>
/// مالک، فروشنده، Tenant، نسخهٔ نصب، ارز، زبان پیش‌فرض و هیچ شناسهٔ جغرافیایی در بدنه نیستند.
/// </para>
/// </remarks>
/// <param name="ExpectedRevision">بازبینی ریشه که کلاینت دیده است؛ باید مثبت باشد.</param>
/// <param name="RegionGroups">کل مجموعهٔ مرتب گروه‌ها.</param>
/// <param name="MethodTranslations">وصلهٔ زبان‌محور نام/توضیح روش؛ <c>[]</c> یعنی بدون تغییر.</param>
/// <param name="WeeklySchedule">برنامهٔ هفتگی اختیاری؛ غایب یعنی حفظ مقدار فعلی.</param>
/// <param name="MethodSettings">تنظیمات روش اختیاری؛ غایب یعنی حفظ مقدار فعلی.</param>
/// <param name="TypeAssignment">انتخاب نوع اختیاری؛ غایب یعنی حفظ مقدار فعلی.</param>
public sealed record ShippingAuthoringEditorDocumentReplaceRequest
{
    /// <summary>بازبینی ریشه که کلاینت دیده است.</summary>
    [JsonRequired]
    public long ExpectedRevision { get; init; }

    /// <summary>کل مجموعهٔ مرتب گروه‌ها.</summary>
    [JsonRequired]
    public IReadOnlyList<ShippingAuthoringEditorGroupWrite>? RegionGroups { get; init; }

    /// <summary>وصلهٔ زبان‌محور نام/توضیح روش.</summary>
    [JsonRequired]
    public IReadOnlyList<ShippingAuthoringMethodTranslationWrite>? MethodTranslations { get; init; }

    /// <summary>برنامهٔ هفتگی اختیاری.</summary>
    public ShippingAuthoringEditorScheduleWrite? WeeklySchedule { get; init; }

    /// <summary>تنظیمات روش اختیاری.</summary>
    public ShippingAuthoringEditorSettingsWrite? MethodSettings { get; init; }

    /// <summary>انتخاب نوع اختیاری.</summary>
    public ShippingAuthoringEditorTypeWrite? TypeAssignment { get; init; }
}
