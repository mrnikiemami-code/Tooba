namespace Tooba.Shipping.Contracts.Authoring;

/// <summary>
/// سطحی که یک انتخاب جغرافیایی در آن متوقف شده است، در قرارداد مسیرهای نویسندگی.
/// این enum فقط شکل انتقال است؛ هویت ماندگاری (گونه + شناسهٔ گره) در Domain است.
/// </summary>
public enum ShippingAuthoringAreaKind
{
    /// <summary>کشور.</summary>
    Country = 0,

    /// <summary>ناحیهٔ اداری.</summary>
    AdministrativeArea = 1,

    /// <summary>شهر.</summary>
    City = 2,

    /// <summary>ناحیهٔ خدمت.</summary>
    ServiceArea = 3,
}

/// <summary>
/// یک انتخاب مقصد در بدنهٔ نوشتن: شناسه‌های پایدار همان سطح و والدهای اعلام‌شدهٔ آن.
/// </summary>
/// <remarks>
/// والدها بخشی از <b>ورودی</b> اثبات هستند، نه اعتماد: سرور آن‌ها را با فکت‌های معتبر Geography
/// می‌سنجد و زنجیرهٔ واقعی را خودش می‌سازد. شناسهٔ کشور الزامی است؛ عمق‌های پایین‌تر بسته به
/// <see cref="Kind"/> الزامی یا ممنوع‌اند و اعتبارسنجی شکل همین را تضمین می‌کند.
/// </remarks>
/// <param name="Kind">سطح توقف انتخاب.</param>
/// <param name="CountryId">شناسهٔ پایدار کشور.</param>
/// <param name="AdministrativeAreaId">ناحیهٔ اداری والد (برای شهر/ناحیهٔ خدمت).</param>
/// <param name="CityId">شهر والد (برای ناحیهٔ خدمت).</param>
/// <param name="ServiceAreaId">شناسهٔ ناحیهٔ خدمت (فقط برای سطح ServiceArea).</param>
public sealed record ShippingAuthoringSelectedAreaWrite(
    ShippingAuthoringAreaKind Kind,
    Guid CountryId,
    Guid? AdministrativeAreaId = null,
    Guid? CityId = null,
    Guid? ServiceAreaId = null);

/// <summary>
/// یک گروه منطقه‌ای در بدنهٔ نوشتن: شناسهٔگروه موجود (برای حفظ هویت) و انتخاب‌های آن.
/// </summary>
/// <param name="GroupId">شناسهٔ گروه موجود همین پیش‌نویس؛ تهی برای گروه تازه.</param>
/// <param name="Areas">انتخاب‌های مقصد این گروه.</param>
public sealed record ShippingAuthoringRegionGroupWrite(
    Guid? GroupId,
    IReadOnlyList<ShippingAuthoringSelectedAreaWrite> Areas);

/// <summary>
/// بدنهٔ ایجاد پیش‌نویس: Binding روش ارسال و درخت منطقه‌ای اولیه (می‌تواند خالی باشد).
/// مالک هرگز در بدنه نیست و شناسهٔ پیش‌نویس سرورساخته است.
/// </summary>
/// <param name="ServiceCode">کد خدمت موجود Fulfillment.</param>
/// <param name="OptionCode">گزینهٔ اختیاری همان خدمت.</param>
/// <param name="RegionGroups">درخت منطقه‌ای اولیه.</param>
public sealed record ShippingAuthoringDraftCreateRequest(
    string ServiceCode,
    string? OptionCode,
    IReadOnlyList<ShippingAuthoringRegionGroupWrite> RegionGroups);

/// <summary>
/// بدنهٔ جایگزینی درخت منطقه‌ای. <paramref name="ExpectedRevision"/> الزامی است تا جایگزینی
/// خوش‌بینانه هرگز روی یک وضعیت تازه‌تر نوشته نشود.
/// </summary>
/// <param name="ExpectedRevision">بازبینی مورد انتظار کلاینت.</param>
/// <param name="RegionGroups">درخت منطقه‌ای جدید (جایگزینی کامل).</param>
public sealed record ShippingAuthoringDraftReplaceRegionsRequest(
    long ExpectedRevision,
    IReadOnlyList<ShippingAuthoringRegionGroupWrite> RegionGroups);
