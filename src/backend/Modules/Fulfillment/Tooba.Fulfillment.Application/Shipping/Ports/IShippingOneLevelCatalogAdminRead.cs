using Tooba.Fulfillment.Contracts.Shipping;

namespace Tooba.Fulfillment.Application.Shipping.Ports;

/// <summary>
/// نتیجهٔ خواندن فقط-خواندنی مسیر ادمین برای کاتالوگ یک‌سطحی: دروازهٔ مؤثر سمت سرور،
/// آماده‌بودن کاتالوگ و روش‌ها (شامل غیرفعال‌ها). هیچ EF/DbContext در این قرارداد نیست.
/// </summary>
/// <param name="Gate">دروازهٔ مؤثر ارزیابی‌شده از پیکربندی معتبر سرور.</param>
/// <param name="CatalogReady">آیا کاتالوگ واقعاً خوانده شد. نبود جدول/مهاجرت ⇒ false.</param>
/// <param name="Methods">روش‌ها؛ در نبود کاتالوگ آماده، لیست خالی است.</param>
public sealed record ShippingOneLevelCatalogAdminSnapshot(
    ShippingOneLevelCatalogGate Gate,
    bool CatalogReady,
    IReadOnlyList<ShippingOneLevelMethodSnapshot> Methods);

/// <summary>
/// نتیجهٔ حل پیوند قدیمی یک روش یک‌سطحی. <paramref name="Resolved"/> فقط وقتی <c>true</c> است که
/// دروازه روشن، کاتالوگ آماده و کد روش canonical و موجود باشد؛ در غیر این‌صورت هیچ پیوندی
/// (<c>ServiceCode</c>/<c>OptionCode</c>) برگردانده نمی‌شود.
/// </summary>
/// <param name="Gate">دروازهٔ مؤثر ارزیابی‌شده از پیکربندی معتبر سرور.</param>
/// <param name="CatalogReady">آیا کاتالوگ واقعاً خوانده شد. نبود جدول/مهاجرت ⇒ false.</param>
/// <param name="Resolved">آیا پیوند قطعی حل شد. نبود روش یا کد نامعتبر ⇒ false.</param>
/// <param name="MethodCode">کد روش canonical؛ در نبود حل، تهی.</param>
/// <param name="ServiceCode">کد سرویس قدیمی والد؛ فقط وقتی حل شده باشد.</param>
/// <param name="OptionCode">کد گزینهٔ قدیمی؛ تهی یعنی روش به سرویس بدون گزینه نگاشته شده است.</param>
/// <param name="IsActive">فعال بودن روش؛ روش غیرفعال هرگز پیوند قابل‌استفاده نمی‌دهد.</param>
public sealed record ShippingOneLevelCatalogAdminBindingSnapshot(
    ShippingOneLevelCatalogGate Gate,
    bool CatalogReady,
    bool Resolved,
    string MethodCode,
    string ServiceCode,
    string? OptionCode,
    bool IsActive);

/// <summary>
/// درز خواندن ادمین کاتالوگ یک‌سطحی. این درز فقط-خواندنی است و هیچ قیمت، رزرو، اسنپ‌شات
/// پرداخت‌شده یا کاتالوگ قدیمی را تغییر نمی‌دهد.
/// </summary>
/// <remarks>
/// دو قاعدهٔ سخت:
/// <list type="bullet">
/// <item>دروازهٔ خاموش ⇒ هیچ پرس‌وجوی دیتابیسی اجرا نمی‌شود و <c>CatalogReady=false</c> است.</item>
/// <item>جدول مهاجرت‌نشده ⇒ نتیجهٔ کنترل‌شدهٔ <c>NOT_READY</c> (<c>CatalogReady=false</c>)، نه خطای ۵۰۰
/// و نه ادعای موفقیت با لیست خالی.</item>
/// </list>
/// این درز هرگز <c>Tooba:ShippingRateAuthority</c> یا <c>Tooba:ShippingOperationalShadow</c> را
/// نمی‌خواند، القا نمی‌کند یا تغییر نمی‌دهد.
/// </remarks>
public interface IShippingOneLevelCatalogAdminRead
{
    /// <summary>دروازهٔ مؤثر و کاتالوگ را می‌خواند؛ هیچ نوشتنی ندارد.</summary>
    Task<ShippingOneLevelCatalogAdminSnapshot> ReadAsync(CancellationToken cancellationToken);

    /// <summary>
    /// پیوند قدیمی یک روش را از هویت canonical خودش حل می‌کند. هیچ‌گاه از کد ارسالی کلاینت
    /// (serviceCode/optionCode خام) استفاده نمی‌کند و هرگز <c>method_code</c> را در مرورگر
    /// به سرویس/گزینه نمی‌شکند. دروازهٔ خاموش ⇒ هیچ پرس‌وجوی دیتابیسی و <c>Resolved=false</c>.
    /// </summary>
    /// <param name="methodCode">کد روش یک‌سطحی انتخاب‌شده در UI.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<ShippingOneLevelCatalogAdminBindingSnapshot> ResolveBindingAsync(
        string methodCode,
        CancellationToken cancellationToken);
}
