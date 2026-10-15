namespace Tooba.Order.Application.Storefront.Shipping.OperationalShadow;

/// <summary>
/// تنظیمات درز «سایهٔ عملیاتی ارسال». این درز عمداً از درز سایهٔ سادهٔ نرخ
/// (<see cref="RateAuthority.StorefrontShippingRateAuthorityOptions"/>) <b>جدا</b> است.
/// </summary>
/// <remarks>
/// <para>
/// <b>چرا یک درز تازه و نه گسترش درز قبلی.</b> درز قبلی فقط «مبلغ و مجازبودن» را مقایسه می‌کند؛
/// این درز مشاهدهٔ عملیاتی کامل (بسته‌بندی، تفکیک پرداخت، وضعیت بازهٔ حامل و صداقت ظرفیت) را
/// می‌بیند. اگر همین دو فلگ قبلی را بازاستفاده می‌کردیم، روشن‌کردن سایهٔ قدیمی ناخواسته یک
/// ادعای تجاری تازه را هم فعال می‌کرد. پس دو کلید مستقل با پیش‌فرض خاموش لازم است.
/// </para>
/// <para>
/// <b>هر دو پیش‌فرض خاموش.</b> روشن‌بودن یک فلگ کافی نیست: «تأیید پیش‌نیازها» یک ادعای واقعی
/// دربارهٔ داده است و جدا نگه داشته می‌شود تا یک فلگ تنها نتواند سیاست fail-closed را دور بزند.
/// </para>
/// </remarks>
public sealed class StorefrontShippingOperationalShadowOptions
{
    /// <summary>نام بخش تنظیمات.</summary>
    public const string SectionName = "Tooba:ShippingOperationalShadow";

    /// <summary>
    /// آیا مشاهدهٔ عملیاتی سایه اجرا شود. پیش‌فرض <c>false</c>؛ نتیجه فقط برای مشاهده/تلمتری است و
    /// هیچ‌گاه قیمت، وعدهٔ تحویل، پیش‌نویس یا snapshot سفارش را تغییر نمی‌دهد.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// آیا پیش‌نیازهای واقعی تأیید شده‌اند. تا وقتی این مقدار <c>false</c> است، حتی با روشن‌بودن
    /// <see cref="Enabled"/> هم هیچ مشاهده‌ای اجرا نمی‌شود.
    /// </summary>
    public bool PrerequisitesConfirmed { get; set; }
}
