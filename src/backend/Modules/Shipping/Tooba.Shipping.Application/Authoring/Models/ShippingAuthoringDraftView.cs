using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Packaging;

namespace Tooba.Shipping.Application.Authoring.Models;

/// <summary>یک انتخاب مقصد در پاسخ نویسندگی: گونهٔ گره و شناسهٔ پایدار.</summary>
/// <param name="Kind">گونهٔ گره.</param>
/// <param name="Id">شناسهٔ پایدار گره.</param>
public sealed record ShippingAuthoringSelectedAreaItem(ShippingAuthoringAreaKind Kind, Guid Id);

/// <summary>یک گروه منطقه‌ای در پاسخ: هویت پایدار، ترتیب و انتخاب‌ها.</summary>
/// <param name="GroupId">شناسهٔ پایدار گروه (مرجع آیندهٔ ردیف‌های نرخ).</param>
/// <param name="Order">ترتیب صریح گروه.</param>
/// <param name="Areas">انتخاب‌های گروه.</param>
public sealed record ShippingAuthoringRegionGroupItem(
    Guid GroupId,
    int Order,
    IReadOnlyList<ShippingAuthoringSelectedAreaItem> Areas);

/// <summary>
/// پاسخ یک پیش‌نویس نویسندگی. صادقانه <b>Draft</b> است: هیچ نرخی، فعال‌سازی یا مصرف‌کنندهٔ
/// استعلامی در این سطح وجود ندارد و نبود گروه یعنی پیکربندی ناقص.
/// </summary>
/// <param name="DraftId">شناسهٔ پایدار پیش‌نویس.</param>
/// <param name="Owner">مالک مجاز همین درخواست.</param>
/// <param name="ServiceCode">کد خدمت Binding.</param>
/// <param name="OptionCode">گزینهٔ Binding؛ تهی یعنی سطح خدمت.</param>
/// <param name="Revision">بازبینی فعلی برای درخواست جایگزینی بعدی.</param>
/// <param name="CreatedAt">زمان ایجاد.</param>
/// <param name="UpdatedAt">زمان آخرین جایگزینی.</param>
/// <param name="RegionGroups">گروه‌های منطقه‌ای.</param>
public sealed record ShippingAuthoringDraftItem(
    Guid DraftId,
    ShippingAuthoringOwnerView Owner,
    string ServiceCode,
    string? OptionCode,
    long Revision,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ShippingAuthoringRegionGroupItem> RegionGroups)
{
    /// <summary>
    /// متن‌های <b>واقعاً ذخیره‌شدهٔ</b> نام/توضیح روش به‌ازای زبان، برای خواندنهای فهرستی.
    /// </summary>
    /// <remarks>
    /// این فقط یک <b>فیلد افزایشی</b> روی پاسخ موجود است: هیچ مسیر/سازندهٔ موضعی/طرح‌وارهٔ تازه‌ای
    /// ساخته نمی‌شود و مصرف‌کننده‌های فعلی که فقط Binding را می‌خوانند آن را نادیده می‌گیرند. مقدار آن
    /// همان متن‌های ذخیره‌شده‌ای است که خواندن فهرست در همان پرس‌وجو بارگذاری می‌کند؛ مجموعهٔ خالی
    /// یعنی «متنی در این پاسخ ذخیره نشده است» — نه نام fallback و نه حدس زبان.
    /// </remarks>
    public IReadOnlyList<ShippingAuthoringMethodTranslationItem> MethodTranslations { get; init; } = [];
}

/// <summary>فهرست پیش‌نویس‌های مالک مجاز همین درخواست.</summary>
/// <param name="Drafts">پیش‌نویس‌ها؛ فهرست خالی یعنی هنوز پیش‌نویسی ساخته نشده است.</param>
public sealed record ShippingAuthoringDraftList(IReadOnlyList<ShippingAuthoringDraftItem> Drafts);

/// <summary>یک ردیف نرخ دستی در پاسخ؛ بدون هیچ ستون جغرافیایی.</summary>
/// <param name="RateRowId">شناسهٔ پایدار سرورساخته.</param>
/// <param name="MinWeightGrams">کران پایین وزن (گرم).</param>
/// <param name="MaxWeightGrams">کران بالای وزن (گرم).</param>
/// <param name="MinItemCount">کران پایین تعداد واحد کالا.</param>
/// <param name="MaxItemCount">کران بالای تعداد واحد کالا.</param>
/// <param name="MinMerchandiseAmount">کران پایین مبلغ کالای گروه.</param>
/// <param name="MaxMerchandiseAmount">کران بالای مبلغ کالای گروه.</param>
/// <param name="FreightAmount">هزینهٔ حمل.</param>
/// <param name="PackagingAmount">هزینهٔ بسته‌بندی.</param>
/// <param name="MinDeliveryDays">کمینهٔ مدت تحویل (روز).</param>
/// <param name="MaxDeliveryDays">بیشینهٔ مدت تحویل (روز).</param>
public sealed record ShippingAuthoringRateRowItem(
    Guid RateRowId,
    long? MinWeightGrams,
    long? MaxWeightGrams,
    int? MinItemCount,
    int? MaxItemCount,
    decimal? MinMerchandiseAmount,
    decimal? MaxMerchandiseAmount,
    decimal FreightAmount,
    decimal PackagingAmount,
    int MinDeliveryDays,
    int MaxDeliveryDays);

/// <summary>
/// پاسخ مجموعهٔ ردیف‌های نرخ یک گروه: هویت گروه/پیش‌نویس، بازبینی فعلی ریشهٔ پیش‌نویس (نشانگر
/// همزمانی مشترک با جایگزینی درخت) و ارز ذخیره‌شده (تهی یعنی هنوز bind نشده).
/// </summary>
/// <param name="DraftId">شناسهٔ پیش‌نویس.</param>
/// <param name="GroupId">شناسهٔ پایدار گروه.</param>
/// <param name="Revision">بازبینی فعلی ریشهٔ پیش‌نویس.</param>
/// <param name="CurrencyCode">ارز ذخیره‌شدهٔ مجموعهٔ ردیف‌ها؛ تهی یعنی هنوز ذخیره نشده.</param>
/// <param name="Rows">ردیف‌ها؛ خالی یعنی پیکربندی ناقص، نه ارسال رایگان.</param>
public sealed record ShippingAuthoringGroupRateRowsItem(
    Guid DraftId,
    Guid GroupId,
    long Revision,
    string? CurrencyCode,
    IReadOnlyList<ShippingAuthoringRateRowItem> Rows);

/// <summary>یک متن ذخیره‌شدهٔ مالک برای یک زبان محتوا (خواندن خام، بدون fallback).</summary>
/// <param name="LanguageId">شناسهٔ پایدار زبان.</param>
/// <param name="Name">نام ذخیره‌شده.</param>
/// <param name="Description">توضیح ذخیره‌شده یا تهی.</param>
public sealed record ShippingAuthoringMethodTranslationItem(
    Guid LanguageId,
    string Name,
    string? Description);

/// <summary>یک عنوان ذخیره‌شدهٔ گروه برای یک زبان (خواندن خام).</summary>
/// <param name="GroupId">شناسهٔ گروه.</param>
/// <param name="LanguageId">شناسهٔ زبان.</param>
/// <param name="Title">عنوان ذخیره‌شده.</param>
public sealed record ShippingAuthoringGroupTitleItem(
    Guid GroupId,
    Guid LanguageId,
    string Title);

/// <summary>
/// سند ارائهٔ یک پیش‌نویس: متن‌های <b>واقعاً ذخیره‌شده</b> به‌ازای زبان، همراه registry فعال و زبان
/// پیش‌فرض فعلی.
/// </summary>
/// <remarks>
/// متن زبان غیرفعال حذف نمی‌شود؛ فقط در <paramref name="ContentLanguages"/> (که همان زبان‌های فعال
/// هستند) ظاهر نمی‌شود تا کلاینت بتواند «ذخیره‌شده ولی فعلاً غیرفعال» را از «فعال و کامل» تشخیص دهد.
/// <paramref name="HasDefaultLanguageName"/> تنها نتیجهٔ مقایسهٔ متن ذخیره‌شده با شناسهٔ پیش‌فرض
/// <b>فعلی</b> است، نه یک نام fallback.
/// </remarks>
/// <param name="DraftId">شناسهٔ پیش‌نویس.</param>
/// <param name="Revision">بازبینی ریشهٔ فعلی.</param>
/// <param name="MethodTranslations">متن‌های ذخیره‌شدهٔ نام/توضیح.</param>
/// <param name="GroupTitles">عنوان‌های ذخیره‌شدهٔ گروه‌ها.</param>
/// <param name="ContentLanguages">زبان‌های فعال authoritative همین لحظه.</param>
/// <param name="DefaultContentLanguageId">شناسهٔ زبان پیش‌فرض فعلی.</param>
/// <param name="HasDefaultLanguageName">آیا نام برای زبان پیش‌فرض فعلی ذخیره شده است.</param>
public sealed record ShippingAuthoringPresentationItem(
    Guid DraftId,
    long Revision,
    IReadOnlyList<ShippingAuthoringMethodTranslationItem> MethodTranslations,
    IReadOnlyList<ShippingAuthoringGroupTitleItem> GroupTitles,
    IReadOnlyList<ShippingAuthoringLanguageView> ContentLanguages,
    Guid DefaultContentLanguageId,
    bool HasDefaultLanguageName);

/// <summary>یک روز هفتهٔ برنامهٔ عملیاتی مالک در پاسخ: پرچم صریح و ساعت‌های (به‌یادماندهٔ) همان روز.</summary>
/// <param name="Weekday">روز هفتهٔ ISO ۱..۷.</param>
/// <param name="IsWorking">آیا این روز صریحاً روز کاری است.</param>
/// <param name="StartTime">ساعت شروع محلی یا تهی وقتی هیچ ساعتی ثبت نشده است.</param>
/// <param name="EndTime">ساعت پایان محلی (غیرشامل) یا تهی.</param>
public sealed record ShippingAuthoringScheduleDayItem(
    int Weekday,
    bool IsWorking,
    TimeOnly? StartTime,
    TimeOnly? EndTime);

/// <summary>
/// برنامهٔ عملیاتی هفتگی یک پیش‌نویس: بازبینی ریشهٔ واقعی و وضعیت صریح پیکربندی‌شده/نشده.
/// </summary>
/// <remarks>
/// برای برنامهٔ <b>پیکربندی‌نشده</b> هیچ منطقهٔ زمانی و هیچ ساعتی ساخته نمی‌شود
/// (<c>TimeZoneId = null</c>، <c>Days = []</c>). یک هفتهٔ کاملاً تعطیل، هفت ردیف واقعی با
/// <c>IsWorking = false</c> برمی‌گرداند و از «پیکربندی‌نشده» قابل تفکیک است. هیچ fallback منطقهٔ
/// زمانی، ساعت پیش‌فرض یا هفت روز کاری ساخته نمی‌شود.
/// </remarks>
/// <param name="DraftId">شناسهٔ پیش‌نویس.</param>
/// <param name="Revision">بازبینی ریشهٔ فعلی برای نوشتن بعدی.</param>
/// <param name="IsConfigured">آیا برنامه صریحاً پیکربندی شده است.</param>
/// <param name="TimeZoneId">منطقهٔ زمانی ذخیره‌شده یا تهی در حالت پیکربندی‌نشده.</param>
/// <param name="Days">روزهای مرتب‌شده بر اساس روز هفته؛ خالی در حالت پیکربندی‌نشده.</param>
public sealed record ShippingAuthoringWeeklyScheduleItem(
    Guid DraftId,
    long Revision,
    bool IsConfigured,
    string? TimeZoneId,
    IReadOnlyList<ShippingAuthoringScheduleDayItem> Days);

/// <summary>
/// تنظیمات روش یک پیش‌نویس: بازبینی ریشهٔ واقعی و وضعیت صریح تنظیم‌شده/نشده با دو انتخاب مستقل.
/// </summary>
/// <remarks>
/// برای پیش‌نویس <b>تنظیم‌نشده</b> هیچ مقداری ساخته نمی‌شود (<c>FreightPaymentMode = null</c> و
/// <c>CalculationMode = null</c>)؛ <c>Manual</c>/<c>Prepaid</c> پیش‌فرض نیستند. این سند هیچ ادعای
/// آمادگی عملیاتی، اتصال حامل یا تأمین مالی نمی‌کند.
/// </remarks>
/// <param name="DraftId">شناسهٔ پیش‌نویس.</param>
/// <param name="Revision">بازبینی ریشهٔ فعلی برای نوشتن بعدی.</param>
/// <param name="IsConfigured">آیا تنظیمات صریحاً نوشته شده است.</param>
/// <param name="FreightPaymentMode">زمان پرداخت کرایه یا تهی در حالت تنظیم‌نشده.</param>
/// <param name="CalculationMode">حالت محاسبهٔ کرایه یا تهی در حالت تنظیم‌نشده.</param>
public sealed record ShippingAuthoringMethodSettingsItem(
    Guid DraftId,
    long Revision,
    bool IsConfigured,
    ShippingFreightPayerMode? FreightPaymentMode,
    ShippingAuthoringCalculationMode? CalculationMode);
