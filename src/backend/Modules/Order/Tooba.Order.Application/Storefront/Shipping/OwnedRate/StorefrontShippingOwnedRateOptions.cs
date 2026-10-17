namespace Tooba.Order.Application.Storefront.Shipping.OwnedRate;

/// <summary>
/// تنظیمات درز «نرخ مشتریِ مالک‌شدهٔ Shipping» در Order.
/// </summary>
/// <remarks>
/// <para>
/// <b>پیش‌فرض خاموش است.</b> نبود بخش تنظیمات یعنی <c>false</c>؛ در آن حالت مسیر فروشگاه دقیقاً همان
/// نرخ/خطا/DTO قبلی را برمی‌گرداند، هیچ کوئری‌ای به Shipping زده نمی‌شود و هیچ ترافیک پنهانی وجود ندارد.
/// </para>
/// <para>
/// این کلید <b>از</b> درز مشاهده‌ای <c>Tooba:ShippingRateAuthority</c> و از سایهٔ عملیاتی <b>جدا</b> است:
/// روشن‌کردن یک سایه هرگز نباید قیمت مشتری را تغییر دهد و روشن‌کردن این کلید نباید سایه را فعال کند.
/// </para>
/// </remarks>
public sealed class StorefrontShippingOwnedRateOptions
{
    /// <summary>نام بخش تنظیمات.</summary>
    public const string SectionName = "Tooba:ShippingOwnedRateAuthority";

    /// <summary>
    /// آیا نرخ مشتری از مرجع تجاری Shipping (تطبیق سخت‌گیرانهٔ گزینه) گرفته شود. پیش‌فرض <c>false</c>.
    /// </summary>
    public bool Enabled { get; set; }
}
