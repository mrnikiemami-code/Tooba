namespace Tooba.Shipping.Application.Authoring.Models;

/// <summary>
/// زمینهٔ مجاز نویسندگی ادمین ارسال — فقط واقعیت‌های authoritative فروشگاه، بدون هیچ انتخاب
/// مالک/حالت/زبان/ارز از سمت مرورگر.
/// </summary>
/// <param name="Edition">
/// حالت مؤثر نصب: <c>SingleStore</c> یا <c>Marketplace</c>. از زمینهٔ resolve‌شدهٔ پلتفرم می‌آید.
/// صرف <c>Marketplace</c> بودن نصب به‌هیچ‌وجه یعنی نویسندگی seller-scoped آماده است یا مجوز
/// مالک فروشنده صادر شده؛ مالکیت/مجوز فروشنده کار جداگانه‌ای است.
/// </param>
/// <param name="DefaultCurrency">ارز پیش‌فرض پیکربندی‌شدهٔ همان فروشگاه از StoreContext.</param>
/// <param name="ContentLanguages">
/// زبان‌های محتوای <b>فعال</b> با metadata واقعی مالک، مرتب‌شده بر اساس ترتیب مالک؛ هیچ ترتیب
/// نمایشی از locale ساخته نمی‌شود.
/// </param>
/// <param name="DefaultContentLanguageId">
/// شناسهٔ زبان پیش‌فرض محتوا؛ همیشه به یکی از اعضای <paramref name="ContentLanguages"/> اشاره می‌کند.
/// </param>
public sealed record ShippingAuthoringContextView(
    string Edition,
    string DefaultCurrency,
    IReadOnlyList<ShippingAuthoringLanguageView> ContentLanguages,
    Guid DefaultContentLanguageId);

/// <summary>یک زبان محتوای فعال با metadata authoritative.</summary>
/// <param name="LanguageId">شناسهٔ پایدار زبان در Localization.</param>
/// <param name="Code">کد زبان.</param>
/// <param name="Culture">فرهنگ (culture).</param>
/// <param name="UrlPrefix">پیشوند مسیر؛ برای زبان پیش‌فرض می‌تواند خالی باشد.</param>
/// <param name="Direction">جهت نوشتار <c>rtl</c>/<c>ltr</c>.</param>
/// <param name="DisplayName">نام نمایشی.</param>
/// <param name="NativeName">نام بومی.</param>
/// <param name="IsDefault">آیا زبان پیش‌فرض محتوا است.</param>
/// <param name="SortOrder">ترتیب اعلام‌شدهٔ مالک.</param>
public sealed record ShippingAuthoringLanguageView(
    Guid LanguageId,
    string Code,
    string Culture,
    string UrlPrefix,
    string Direction,
    string DisplayName,
    string NativeName,
    bool IsDefault,
    int SortOrder);
