namespace Tooba.Order.Application.Storefront.Shipping.RateAuthority;

/// <summary>
/// تنظیمات درز «مرجع نرخ ارسال». پیش‌فرض‌ها عمداً امن هستند: سایه خاموش است و پیش‌نیازها
/// تأییدنشده‌اند، بنابراین نصب این کد هیچ رفتاری را تغییر نمی‌دهد.
/// </summary>
/// <remarks>
/// <para>
/// <b>چرا دو کلید جدا؟</b> «روشن‌بودن سایه» یک تصمیم عملیاتی است، اما «تأیید پیش‌نیازها» یک ادعای
/// واقعی دربارهٔ داده است: بدون schema ارسال و بدون مجوزهای پوشش صریح، هیچ مقایسه‌ای معنا ندارد و
/// هیچ نرخی نباید از مسیر جدید بیاید. جدا کردن آن دو، اجازه نمی‌دهد یک فلگ تنها، سیاست fail-closed
/// را دور بزند.
/// </para>
/// <para>
/// این کلاس در این Task به هیچ ریشهٔ ترکیب (DI) وصل نمی‌شود؛ صرفاً یک قرارداد تنظیمات آمادهٔ اتصال است.
/// </para>
/// </remarks>
public sealed class StorefrontShippingRateAuthorityOptions
{
    /// <summary>نام بخش تنظیمات.</summary>
    public const string SectionName = "Tooba:ShippingRateAuthority";

    /// <summary>
    /// آیا مقایسهٔ سایه اجرا شود. پیش‌فرض <c>false</c> است؛ سایه هرگز قیمت مشتری را تغییر نمی‌دهد و
    /// صرفاً برای مشاهده/تلمتری اجرا می‌شود.
    /// </summary>
    public bool ShadowEnabled { get; set; }

    /// <summary>
    /// آیا پیش‌نیازهای واقعی تأیید شده‌اند: schema ارسال مهاجرت‌شده و مجوزهای پوشش صریح برای همهٔ
    /// کدهای خدمت فعال. تا وقتی این مقدار <c>false</c> است، سایه حتی با روشن‌بودن
    /// <see cref="ShadowEnabled"/> هم اجرا نمی‌شود.
    /// </summary>
    public bool PrerequisitesConfirmed { get; set; }
}
