namespace Tooba.Fulfillment.Application.Shipping.Ports;

/// <summary>
/// نتیجهٔ نوشتن نام یک روش یک‌سطحی. هر حالت شکست صریح است تا هیچ نوشتنی خاموش رخ ندهد و
/// «موفق» هرگز برای دروازهٔ خاموش/کاتالوگ مهاجرت‌نشده ادعا نشود.
/// </summary>
public enum ShippingOneLevelMethodNameWriteStatus
{
    /// <summary>نام ترجمه در همان زبان نوشته شد (به‌روزرسانی یا درج).</summary>
    Updated = 0,

    /// <summary>دروازهٔ کاتالوگ یک‌سطحی خاموش است ⇒ هیچ پرس‌وجو/نوشتنی انجام نمی‌شود.</summary>
    GateDisabled = 1,

    /// <summary>جدول کاتالوگ یک‌سطحی مهاجرت نشده است ⇒ نتیجهٔ کنترل‌شده، نه خطای ۵۰۰.</summary>
    CatalogNotReady = 2,

    /// <summary>کد روش خام/نامعتبر است و هرگز نرمال یا به هویت تازه تبدیل نمی‌شود.</summary>
    InvalidMethodCode = 3,

    /// <summary>روشی با این کد canonical وجود ندارد.</summary>
    MethodNotFound = 4,

    /// <summary>نام خالی/فقط‌فاصله است؛ برچسب انسانی الزامی است.</summary>
    NameRequired = 5,

    /// <summary>نام از عرض ستون پایدار ترجمه بلندتر است.</summary>
    NameTooLong = 6,
}

/// <summary>
/// نتیجهٔ نوشتن نام روش یک‌سطحی. در شکست، هیچ نوشتنی رخ نداده است.
/// </summary>
/// <param name="Status">حالت صریح نتیجه.</param>
/// <param name="MethodCode">کد canonical روش (در کد نامعتبر تهی).</param>
/// <param name="LanguageId">زبان هدف.</param>
/// <param name="Name">نام نوشته‌شده؛ فقط در حالت <see cref="ShippingOneLevelMethodNameWriteStatus.Updated"/> معتبر است.</param>
public sealed record ShippingOneLevelMethodNameWriteResult(
    ShippingOneLevelMethodNameWriteStatus Status,
    string MethodCode,
    Guid LanguageId,
    string Name);

/// <summary>
/// درز نوشتن نام روش یک‌سطحی روی مالک Fulfillment — تنها نوشتن مجاز این مسیر: ترجمهٔ نام.
/// </summary>
/// <remarks>
/// <para>
/// دامنهٔ این درز عمداً باریک است. <c>method_code</c>، <c>legacy_service_code</c>،
/// <c>legacy_option_code</c>، <c>provider_kind</c> و فعال/غیرفعال بودن هرگز از این‌جا نوشته نمی‌شوند؛
/// همچنین هیچ گزینهٔ فرزند، گروه‌بندی ارائه‌دهنده یا کنترل سطح دومی وجود ندارد. هویت روش همان
/// <c>method_code</c> canonical است و هیچ شناسهٔ گزینهٔ قدیمی به‌عنوان هویت استفاده نمی‌شود.
/// </para>
/// <para>
/// اعتبارسنجی زبان در لایهٔ Application با رجیستری زبان انجام می‌شود؛ این درز فقط شکل کد/نام،
/// دروازه، آماده‌بودن کاتالوگ و وجود روش را کنترل می‌کند.
/// </para>
/// </remarks>
public interface IShippingOneLevelCatalogAdminWrite
{
    /// <summary>
    /// نام ترجمهٔ روش را در یک زبان می‌نویسد. نوشتن روی یکتایی canonical
    /// <c>(shipping_method_id, language_id)</c> انجام می‌شود: به‌روزرسانی ردیف موجود، در غیر این‌صورت درج
    /// ردیف تازه. اجرای دوباره با همان مقدار، بی‌اثر و idempotent است.
    /// </summary>
    /// <param name="methodCode">کد روش canonical انتخاب‌شده در UI.</param>
    /// <param name="languageId">زبان هدف؛ اعتبار آن در Application بررسی شده است.</param>
    /// <param name="name">نام انسانی محدود و غیرخالی.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<ShippingOneLevelMethodNameWriteResult> UpdateMethodNameAsync(
        string methodCode,
        Guid languageId,
        string? name,
        CancellationToken cancellationToken);
}

/// <summary>حدود برچسب نام روش یک‌سطحی؛ هم‌اندازهٔ ستون پایدار ترجمه.</summary>
public static class ShippingOneLevelMethodNameLimits
{
    /// <summary>حداکثر طول نام (هم‌اندازهٔ <c>shipping_method_translations.name</c>).</summary>
    public const int MaxNameLength = 128;
}
