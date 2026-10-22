using System.Text.Json.Serialization;

namespace Tooba.Shipping.Contracts.Authoring;

/// <summary>
/// یک متن چندزبانهٔ خام و ذخیره‌شدهٔ تعریف نوع ارسال، با شناسهٔ واقعی زبان محتوا.
/// </summary>
/// <remarks>
/// هیچ انتخاب زبان از روی culture رابط کاربری انجام نمی‌شود: هر ردیف شناسهٔ زبان و متن خودش را حمل
/// می‌کند و مصرف‌کننده خودش تصمیم می‌گیرد کدام را نمایش دهد. هیچ نام جایگزین/fallback اینجا ساخته
/// نمی‌شود.
/// </remarks>
/// <param name="LanguageId">شناسهٔ پایدار زبان محتوا.</param>
/// <param name="Name">نام غیرتهی همان زبان.</param>
/// <param name="Description">توضیح اختیاری همان زبان.</param>
public sealed record ShippingAuthoringTypeTranslation(
    Guid LanguageId,
    string Name,
    string? Description);

/// <summary>
/// یک تعریف نوع ارسال به‌صورت فکت فقط-خواندنی برای پنل نویسندگی Shipping.
/// </summary>
/// <remarks>
/// این قرارداد فقط هویت/کد/ترتیب/وضعیت فعال/بازبینی و متن‌های خام را حمل می‌کند. هیچ نرخ، جغرافیا،
/// بسته‌بندی، برنامه، ارز، پرداخت یا وعدهٔ تحویل در آن نیست و انتخاب یک نوع هیچ روش ارسالی را فعال
/// نمی‌کند.
/// </remarks>
/// <param name="ShippingTypeId">شناسهٔ پایدار تعریف.</param>
/// <param name="Code">کد canonical تعریف.</param>
/// <param name="SortOrder">ترتیب اعلام‌شدهٔ مالک.</param>
/// <param name="Revision">شمارهٔ بازبینی تعریف.</param>
/// <param name="Translations">متن‌های خام ذخیره‌شده به‌ازای زبان.</param>
public sealed record ShippingAuthoringTypeItem(
    Guid ShippingTypeId,
    string Code,
    int SortOrder,
    long Revision,
    IReadOnlyList<ShippingAuthoringTypeTranslation> Translations);

/// <summary>
/// فهرست انتخاب‌های نوع ارسال: فقط تعریف‌های <b>فعال</b> با ترتیب اعلام‌شدهٔ مالک.
/// </summary>
/// <remarks>
/// فهرست خالی یک کاتالوگ واقعاً خالی است، نه «دسترسی‌نداشتن»: شکست/لغو فراخوانی مقصد هرگز به فهرست
/// خالی تبدیل نمی‌شود. این قرارداد هیچ مسیر ایجاد/ویرایش کاتالوگ در Shipping نمی‌سازد.
/// </remarks>
/// <param name="Types">تعریف‌های فعال به ترتیب اعلام‌شده.</param>
public sealed record ShippingAuthoringTypeChoicesItem(
    IReadOnlyList<ShippingAuthoringTypeItem> Types);

/// <summary>
/// وضعیت واقعی حل‌شدن شناسهٔ ذخیره‌شدهٔ نوع در لحظهٔ خواندن.
/// </summary>
public enum ShippingAuthoringTypeResolution
{
    /// <summary>هیچ نوعی انتخاب نشده است (صریحاً انتخاب‌نشده).</summary>
    Unconfigured = 0,

    /// <summary>تعریف ذخیره‌شده وجود دارد و در همین لحظه فعال است.</summary>
    Active = 1,

    /// <summary>تعریف ذخیره‌شده وجود دارد اما غیرفعال است؛ شناسه عمداً پاک یا جایگزین نمی‌شود.</summary>
    Inactive = 2,

    /// <summary>تعریف ذخیره‌شده در کاتالوگ یافت نشد؛ خطای زیرساخت هرگز به «یافت‌نشد» تبدیل نمی‌شود.</summary>
    Missing = 3,
}

/// <summary>
/// انتخاب فعلی نوع ارسال یک پیش‌نویس: شناسهٔ خام ذخیره‌شده و وضعیت واقعی حل‌شدن آن.
/// </summary>
/// <remarks>
/// شناسهٔ خام همیشه گزارش می‌شود تا یک ارجاع غیرفعال/حذف‌شده به‌صورت صادقانه دیده شود؛ هیچ
/// پاک‌سازی/جایگزینی خودکار رخ نمی‌دهد. <c>shippingType</c> برای «انتخاب‌نشده» تهی است.
/// </remarks>
/// <param name="DraftId">شناسهٔ پیش‌نویس.</param>
/// <param name="Revision">بازبینی ریشهٔ پیش‌نویس پس از این عملیات.</param>
/// <param name="ShippingTypeId">شناسهٔ ذخیره‌شده یا تهی.</param>
/// <param name="Resolution">وضعیت واقعی حل‌شدن در همین لحظه.</param>
/// <param name="ShippingType">تعریف کامل وقتی وجود دارد؛ برای «یافت‌نشد» تهی.</param>
public sealed record ShippingAuthoringDraftTypeAssignmentItem(
    Guid DraftId,
    long Revision,
    Guid? ShippingTypeId,
    ShippingAuthoringTypeResolution Resolution,
    ShippingAuthoringTypeItem? ShippingType);

/// <summary>
/// بدنهٔ تعیین/پاک‌کردن نوع ارسال یک پیش‌نویس: بازبینی ریشه و <b>حضور صریح</b> انتخاب.
/// </summary>
/// <remarks>
/// <para>
/// <b>غیبت ≠ پاک‌کردن.</b> فیلد <c>shippingTypeId</c> با <see cref="JsonRequiredAttribute"/> علامت‌گذاری
/// شده است: اگر کلید در JSON نباشد، درخواست پیش از هر هندلری به شکل نامعتبر تبدیل می‌شود. مقدار
/// <c>null</c> صریح یعنی «پاک‌کن»؛ یک آرایه/رشتهٔ نامعتبر همان شکست JSON است.
/// </para>
/// <para>
/// مالک، فروشنده، نسخهٔ نصب، Tenant، کد/متن تعریف، ارز، زبان و هیچ اختیار فعال‌سازی در این بدنه
/// نیست. شناسهٔ خالی (<c>Guid.Empty</c>) و بازبینی غیرمثبت پیش از هندلر رد می‌شوند.
/// </para>
/// </remarks>
public sealed record ShippingAuthoringDraftTypeAssignmentReplaceRequest
{
    /// <summary>بازبینی ریشهٔ پیش‌نویس که کلاینت دیده است.</summary>
    [JsonRequired]
    public long ExpectedRevision { get; init; }

    /// <summary>شناسهٔ تعریف نوع؛ <c>null</c> صریح یعنی پاک‌کردن.</summary>
    [JsonRequired]
    public Guid? ShippingTypeId { get; init; }
}
