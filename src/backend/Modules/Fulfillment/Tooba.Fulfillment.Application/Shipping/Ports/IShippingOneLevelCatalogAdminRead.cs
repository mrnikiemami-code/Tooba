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
}
