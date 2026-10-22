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

/// <summary>
/// یک ردیف نرخ دستی در بدنهٔ نوشتن: شرایط نیم‌باز <c>[min, max)</c> روی سه محور، هزینهٔ حمل و
/// هزینهٔ بسته‌بندی، و بازهٔ مدت تحویل بر حسب روز.
/// </summary>
/// <remarks>
/// <para>
/// <b>هیچ جغرافیایی روی این ردیف نیست.</b> ردیف فقط به هویت پایدار گروه ارجاع می‌دهد؛ کشور/ناحیه/
/// شهر/ناحیهٔ خدمت یک‌بار روی خود گروه ذخیره شده‌اند و اینجا تکرار نمی‌شوند.
/// </para>
/// <para>
/// <b>شرط روی یک محور.</b> <c>min == null</c> یعنی این ردیف روی آن محور شرط ندارد (نامحدود)؛ در این
/// حالت <c>max</c> هم باید تهی باشد. کران پایین شامل و کران بالا غیرشامل است، پس بازه‌های چسبیده
/// هم‌پوشانی ندارند.
/// </para>
/// <para>
/// <b>چرا nullable.</b> مبالغ و مدت تحویل عمداً nullable هستند تا «ارسال‌نشده» از «صفر صریح» جدا
/// بماند؛ صفر یک دادهٔ معتبر پیش‌نویس است، ولی نبود فیلد نباید بی‌سروصدا صفر شود.
/// </para>
/// </remarks>
/// <param name="RateRowId">شناسهٔ ردیف موجود همین گروه برای حفظ هویت؛ تهی برای ردیف تازه.</param>
/// <param name="MinWeightGrams">کران پایین وزن (گرم، شامل)؛ تهی یعنی بی‌شرط روی وزن.</param>
/// <param name="MaxWeightGrams">کران بالای وزن (گرم، غیرشامل)؛ تهی یعنی بی‌کران بالا.</param>
/// <param name="MinItemCount">کران پایین تعداد واحد کالا (شامل)؛ تهی یعنی بی‌شرط روی تعداد.</param>
/// <param name="MaxItemCount">کران بالای تعداد واحد کالا (غیرشامل).</param>
/// <param name="MinMerchandiseAmount">کران پایین مبلغ کالای همین گروه (شامل).</param>
/// <param name="MaxMerchandiseAmount">کران بالای مبلغ کالای همین گروه (غیرشامل).</param>
/// <param name="FreightAmount">هزینهٔ حمل؛ در همان ارز معتبر ذخیره‌شدهٔ گروه.</param>
/// <param name="PackagingAmount">هزینهٔ بسته‌بندی؛ مستقل از هزینهٔ حمل.</param>
/// <param name="MinDeliveryDays">کمینهٔ مدت تحویل بر حسب روز.</param>
/// <param name="MaxDeliveryDays">بیشینهٔ مدت تحویل بر حسب روز (مساوی مجاز است).</param>
public sealed record ShippingAuthoringRateRowWrite(
    Guid? RateRowId,
    long? MinWeightGrams,
    long? MaxWeightGrams,
    int? MinItemCount,
    int? MaxItemCount,
    decimal? MinMerchandiseAmount,
    decimal? MaxMerchandiseAmount,
    decimal? FreightAmount,
    decimal? PackagingAmount,
    int? MinDeliveryDays,
    int? MaxDeliveryDays);

/// <summary>
/// بدنهٔ جایگزینی کامل ردیف‌های نرخ یک گروه. <paramref name="ExpectedRevision"/> همان نشانگر
/// همزمانی ریشهٔ پیش‌نویس است، پس این نوشتن و جایگزینی درخت منطقه‌ای یکدیگر را بازنویسی نمی‌کنند.
/// </summary>
/// <param name="ExpectedRevision">بازبینی ریشهٔ پیش‌نویس که کلاینت دیده است.</param>
/// <param name="Rows">مجموعهٔ کامل ردیف‌ها؛ <c>[]</c> یعنی پیکربندی ناقص و مجاز است.</param>
public sealed record ShippingAuthoringRateRowsReplaceRequest(
    long ExpectedRevision,
    IReadOnlyList<ShippingAuthoringRateRowWrite> Rows);
