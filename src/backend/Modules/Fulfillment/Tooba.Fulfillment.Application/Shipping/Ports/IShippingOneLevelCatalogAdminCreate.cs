namespace Tooba.Fulfillment.Application.Shipping.Ports;

/// <summary>
/// نتیجهٔ ایجاد یک روش ارسال یک‌سطحی canonical. هر حالت شکست صریح است تا هیچ روشی «موفق» اعلام
/// نشود مگر ردیف canonical واقعاً نوشته شده باشد.
/// </summary>
public enum ShippingOneLevelMethodCreateStatus
{
    /// <summary>روش canonical نوشته شد (همیشه غیرفعال؛ فعال‌سازی از درز ۰۹۶ می‌آید).</summary>
    Created = 0,

    /// <summary>دروازهٔ کاتالوگ یک‌سطحی خاموش است ⇒ هیچ پرس‌وجو/نوشتنی انجام نمی‌شود.</summary>
    GateDisabled = 1,

    /// <summary>جدول کاتالوگ یک‌سطحی مهاجرت نشده است ⇒ نتیجهٔ کنترل‌شده، نه خطای ۵۰۰.</summary>
    CatalogNotReady = 2,

    /// <summary>کد روش خام/نامعتبر است و هرگز نرمال یا به هویت تازه تبدیل نمی‌شود.</summary>
    InvalidMethodCode = 3,

    /// <summary>روشی با همین کد canonical از قبل وجود دارد.</summary>
    MethodCodeDuplicate = 4,

    /// <summary>پیوند قدیمی مشتق‌شده در اختیار روش دیگری است (نگهبان جدایی روش‌ها).</summary>
    BindingDuplicate = 5,

    /// <summary>نام هیچ زبانی نیامده است؛ روش بدون برچسب انسانی ساخته نمی‌شود.</summary>
    NameRequired = 6,

    /// <summary>نام از عرض ستون پایدار ترجمه بلندتر است.</summary>
    NameTooLong = 7,

    /// <summary>همان زبان دو بار در درخواست آمده است.</summary>
    DuplicateLanguage = 8,

    /// <summary>نوشتن هم‌زمان با یک یکتایی واقعی برخورد کرد ⇒ هیچ ادعای موفقیتی نیست.</summary>
    ConcurrentConflict = 9,

    /// <summary>metadata ارائه‌دهنده شکل canonical ندارد (فقط a-z/0-9/_).</summary>
    InvalidProviderKind = 10,
}

/// <summary>روش canonical نوشته‌شده؛ فقط در حالت <see cref="ShippingOneLevelMethodCreateStatus.Created"/> معتبر است.</summary>
/// <param name="Status">حالت صریح نتیجه.</param>
/// <param name="MethodCode">کد canonical روش؛ تغییرناپذیر.</param>
/// <param name="ProviderKind">metadata ارائه‌دهنده؛ هرگز اقتدار قیمت/پوشش نیست.</param>
/// <param name="LegacyServiceCode">کد سرویس پیوند مشتق‌شده از همان کد canonical.</param>
/// <param name="LegacyOptionCode">کد گزینه؛ تهی یعنی روش سطح‌سرویس.</param>
/// <param name="IsActive">همیشه <c>false</c>؛ فعال‌سازی فقط از درز ۰۹۶.</param>
/// <param name="SortOrder">ترتیب تخصیص‌یافته توسط سرور.</param>
public sealed record ShippingOneLevelMethodCreateOutcome(
    ShippingOneLevelMethodCreateStatus Status,
    string MethodCode,
    string ProviderKind,
    string LegacyServiceCode,
    string? LegacyOptionCode,
    bool IsActive,
    int SortOrder);

/// <summary>یک نام ورودی برای ایجاد روش؛ زبانش از قبل در رجیستری زبان اعتبارسنجی شده است.</summary>
public sealed record ShippingOneLevelMethodCreateName(Guid LanguageId, string Name);

/// <summary>
/// درز نوشتن ایجاد روش یک‌سطحی روی مالک Fulfillment.
/// </summary>
/// <remarks>
/// <para>
/// تنها هویت پایدار، کد canonical روش است. پیوند قدیمی هرگز از کلاینت گرفته نمی‌شود؛ سرور آن را با
/// همان قاعدهٔ نگاشت canonical موجود (همان قاعدهٔ backfill) از کد مشتق می‌کند:
/// <c>service:option</c> ⇒ <c>(service, option)</c> و کد تک‌بخشی ⇒ <c>(code, null)</c>.
/// بنابراین «پیوند ناهم‌خوان» قابل ارسال نیست و پیوند تکراری fail-closed رد می‌شود.
/// </para>
/// <para>
/// روش تازه همیشه غیرفعال نوشته می‌شود. فعال‌سازی فقط از درز تراکنشی ۰۹۶ می‌آید که وجود والد و
/// گزینهٔ واقعی را شرط می‌کند؛ پس روش تازه بدون هویت قدیمی معتبر هرگز قابل‌انتخاب مشتری نمی‌شود.
/// هیچ ردیف سرویس/گزینهٔ قدیمی، هیچ سیاست نرخ/پوشش/برنامه/بسته‌بندی و هیچ دادهٔ Checkout/Order در
/// این درز نوشته نمی‌شود.
/// </para>
/// </remarks>
public interface IShippingOneLevelCatalogAdminCreate
{
    /// <summary>
    /// یک روش canonical مستقل می‌سازد. اجرای دوباره با همان کد fail-closed است (تکرار پذیرفته نمی‌شود).
    /// </summary>
    Task<ShippingOneLevelMethodCreateOutcome> CreateMethodAsync(
        string methodCode,
        string? providerKind,
        IReadOnlyList<ShippingOneLevelMethodCreateName> translations,
        CancellationToken cancellationToken);
}

/// <summary>قاعدهٔ نگاشت canonical کد روش به پیوند قدیمی؛ تابع خالص و مشترک با backfill.</summary>
public static class ShippingOneLevelMethodBinding
{
    /// <summary>
    /// پیوند قدیمی را از کد canonical مشتق می‌کند. کد باید از قبل نرمال‌شده باشد؛ کد نامعتبر
    /// پیوند نمی‌دهد. <c>service:option</c> ⇒ (<c>service</c>, <c>option</c>) و کد تک‌بخشی
    /// ⇒ (<c>code</c>, <c>null</c>) که همان قاعدهٔ سرویس بدون گزینه در backfill است.
    /// </summary>
    public static (string ServiceCode, string? OptionCode)? Derive(string? normalizedCode)
    {
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return null;
        }

        var parts = normalizedCode.Split(':');
        return parts.Length switch
        {
            1 => (parts[0], null),
            2 => (parts[0], parts[1]),
            _ => null,
        };
    }
}
