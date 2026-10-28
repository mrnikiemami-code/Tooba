using Tooba.Shipping.Contracts.Packaging;

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

/// <summary>
/// یک تغییر نام/توضیح برای <b>یک</b> زبان محتوا.
/// </summary>
/// <param name="LanguageId">شناسهٔ زبان محتوای فعال.</param>
/// <param name="Name">نام غیرخالی همین زبان.</param>
/// <param name="Description">توضیح اختیاری همین زبان؛ تهی/خالی فقط توضیح همین زبان را پاک می‌کند.</param>
public sealed record ShippingAuthoringMethodTranslationWrite(
    Guid LanguageId,
    string Name,
    string? Description);

/// <summary>
/// یک تغییر/پاک‌کردن عنوان اختیاری یک گروه برای یک زبان. عنوان تهی فقط همان (گروه، زبان) را پاک
/// می‌کند و هیچ زبان یا گروه دیگری را دست نمی‌زند.
/// </summary>
/// <param name="GroupId">شناسهٔ گروه همین پیش‌نویس.</param>
/// <param name="LanguageId">شناسهٔ زبان محتوای فعال.</param>
/// <param name="Title">عنوان یا تهی برای پاک‌کردن.</param>
public sealed record ShippingAuthoringGroupTitleWrite(
    Guid GroupId,
    Guid LanguageId,
    string? Title);

/// <summary>
/// بدنهٔ به‌روزرسانی سند ارائه: بازبینی ریشهٔ مورد انتظار و دو فهرست تغییر زبان‌محور.
/// </summary>
/// <remarks>
/// <para>
/// <b>PATCH، نه جایگزینی دیکشنری.</b> هر ورودی فقط همان زبان را تغییر می‌دهد؛ زبان غایب یعنی «تغییر
/// نکن». فهرست خالی یعنی «هیچ تغییر» و مجاز است، ولی فهرست <c>null</c>/غایب شکل نامعتبر است.
/// </para>
/// <para>
/// مالک، حالت، Tenant و ارز هرگز در این بدنه نیستند.
/// </para>
/// </remarks>
/// <param name="ExpectedRevision">بازبینی ریشهٔ پیش‌نویس که کلاینت دیده است.</param>
/// <param name="MethodTranslations">تغییرهای نام/توضیح.</param>
/// <param name="GroupTitles">تغییرهای عنوان گروه.</param>
public sealed record ShippingAuthoringPresentationPatchRequest(
    long ExpectedRevision,
    IReadOnlyList<ShippingAuthoringMethodTranslationWrite> MethodTranslations,
    IReadOnlyList<ShippingAuthoringGroupTitleWrite> GroupTitles);

/// <summary>
/// یک روز هفته در بدنهٔ جایگزینی برنامهٔ عملیاتی هفتگی: روز هفتهٔ ISO، پرچم صریح کاری/تعطیل و
/// ساعت‌های اختیاری همان روز.
/// </summary>
/// <remarks>
/// <para>
/// <b>پرچم nullable است، عمداً.</b> اگر <c>IsWorking</c> از نوع <c>bool</c> باشد، نبودِ فیلد در JSON
/// بی‌سروصدا <c>false</c> (یعنی «تعطیل») می‌شود و یک روز کاری حذف‌شده هرگز دیده نمی‌شود. پس پرچم
/// <c>bool?</c> است و الزام حضور آن در اعتبارسنجی/دامنه سنجیده می‌شود.
/// </para>
/// <para>
/// ساعت‌ها <see cref="TimeOnly"/> محلی هستند (بدون تاریخ/منطقه) و با دقت ثابت سریال می‌شوند؛ هیچ
/// تبدیل به UTC یا پارس وابسته به culture انجام نمی‌شود.
/// </para>
/// </remarks>
/// <param name="Weekday">روز هفتهٔ ISO ۱..۷ (۶=شنبه، ۵=جمعه).</param>
/// <param name="IsWorking">پرچم صریح کاری/تعطیل؛ نبود آن شکل نامعتبر است.</param>
/// <param name="StartTime">شروع بازهٔ محلی؛ شامل و برای روز کاری الزامی.</param>
/// <param name="EndTime">پایان بازهٔ محلی؛ غیرشامل و برای روز کاری الزامی.</param>
public sealed record ShippingAuthoringScheduleDayWrite(
    int Weekday,
    bool? IsWorking,
    TimeOnly? StartTime = null,
    TimeOnly? EndTime = null);

/// <summary>
/// بدنهٔ جایگزینی کامل برنامهٔ عملیاتی هفتگی یک پیش‌نویس: بازبینی ریشه، منطقهٔ زمانی صریح و هفت روز.
/// </summary>
/// <remarks>
/// مالک، حالت، Tenant، فروشنده، ارز، زبان و هیچ شناسهٔ جغرافیایی در این بدنه نیست. مجموعهٔ روزها
/// اجباری است و باید دقیقاً هفت روز یکتا باشد؛ <c>[]</c> یا روز کمتر «تعطیل» نیست، پیکربندی ناقص است.
/// </remarks>
/// <param name="ExpectedRevision">بازبینی ریشهٔ پیش‌نویس که کلاینت دیده است.</param>
/// <param name="TimeZoneId">شناسهٔ منطقهٔ زمانی صریح (ترجیحاً IANA)؛ سرور آن را اعتبارسنجی می‌کند.</param>
/// <param name="Days">دقیقاً هفت روز هفته.</param>
/// <param name="SlotDurationMinutes">
/// TB-241: مدت اختیاری هر نوبت تحویل به دقیقه (۱..۱۴۴۰). تهی/غایب یعنی «یک پنجرهٔ کامل روز» و
/// هرگز به ۶۰ دقیقه استنتاج نمی‌شود.
/// </param>
public sealed record ShippingAuthoringWeeklyScheduleReplaceRequest(
    long ExpectedRevision,
    string TimeZoneId,
    IReadOnlyList<ShippingAuthoringScheduleDayWrite> Days,
    int? SlotDurationMinutes = null);

/// <summary>
/// مُد محاسبهٔ کرایهٔ یک روش ارسال در پیش‌نویس مالک: دستی، API حامل یا رایگان.
/// </summary>
/// <remarks>
/// <para>
/// <b>یک انتخاب مستقل، نه ترکیب با زمان پرداخت.</b> این enum فقط «چگونه محاسبه شود» را می‌گوید؛
/// پیش‌پرداخت/پس‌کرایه (<c>ShippingFreightPayerMode</c>) یک انتخاب <b>جدا</b> است و هر شش ترکیب
/// مجازند. هیچ‌کدام در دیگری ادغام نمی‌شود.
/// </para>
/// <para>
/// <b>انتخاب، نه آمادگی.</b> <c>CarrierApi</c> فقط یک درخواست حالت است: هیچ هویت حامل، اعتبارنامه،
/// استعلام، مختصات یا ادعای آمادگی از آن نتیجه نمی‌شود. <c>Free</c> هم فقط یک انتخاب پیش‌نویس است:
/// هیچ هزینهٔ واقعی صفر نمی‌شود، هیچ تأمین‌کننده استنتاج نمی‌شود و هیچ ردیف دستی حذف نمی‌گردد.
/// </para>
/// </remarks>
public enum ShippingAuthoringCalculationMode
{
    /// <summary>کرایه از ردیف‌های دستی همان گروه محاسبه می‌شود.</summary>
    Manual = 0,

    /// <summary>کرایه از API حامل درخواست می‌شود (اتصال واقعی حامل هنوز پیاده نشده است).</summary>
    CarrierApi = 1,

    /// <summary>کرایه برای مشتری رایگان در نظر گرفته می‌شود؛ هزینهٔ واقعی و تأمین‌کننده بیرون این برش‌اند.</summary>
    Free = 2,
}

/// <summary>
/// بدنهٔ جایگزینی تنظیمات روش یک پیش‌نویس: بازبینی ریشه و دو انتخاب مستقل.
/// </summary>
/// <remarks>
/// <para>
/// <b>حضور صریح اجباری است.</b> هر دو مقدار nullable هستند؛ اگر <c>bool</c>/enum غیرnullable بودند،
/// نبودِ فیلد در JSON بی‌سروصدا به مقدار صفر (<c>Prepaid</c>/<c>Manual</c>) تبدیل می‌شد و یک انتخاب
/// ارسال‌نشده هرگز دیده نمی‌شد. الزام حضور در اعتبارسنجی و دامنه سنجیده می‌شود.
/// </para>
/// <para>
/// مالک، فروشنده، نسخهٔ نصب، Tenant، ارز، زبان و هیچ اختیار فعال‌سازی در این بدنه نیست. «تنظیم‌نشده»
/// حالت واقعی است و به <c>Manual</c>/<c>Prepaid</c> تفسیر نمی‌شود.
/// </para>
/// </remarks>
/// <param name="ExpectedRevision">بازبینی ریشهٔ پیش‌نویس که کلاینت دیده است.</param>
/// <param name="FreightPaymentMode">زمان پرداخت کرایه؛ حضور آن اجباری است.</param>
/// <param name="CalculationMode">حالت محاسبهٔ کرایه؛ حضور آن اجباری است.</param>
public sealed record ShippingAuthoringMethodSettingsReplaceRequest(
    long ExpectedRevision,
    ShippingFreightPayerMode? FreightPaymentMode,
    ShippingAuthoringCalculationMode? CalculationMode);
