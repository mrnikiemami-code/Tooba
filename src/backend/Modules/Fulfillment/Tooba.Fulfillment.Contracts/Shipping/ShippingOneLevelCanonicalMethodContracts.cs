namespace Tooba.Fulfillment.Contracts.Shipping;

/// <summary>
/// یک روش یک‌سطحیِ canonical که هیچ ردیف گزینهٔ قدیمی متناظر ندارد.
/// </summary>
/// <remarks>
/// این رکورد فقط هویت متعارف روش را حمل می‌کند: کد روش، سرویس/گزینهٔ پیوند تغییرناپذیر و آیکن
/// سرویس والد. هیچ قیمت، پوشش، وعدهٔ تحویل یا «آماده بودن» در آن نیست؛ آن‌ها مالکیت Shipping
/// دارند و فقط از مسیر نرخ authoritative می‌آیند.
/// </remarks>
/// <param name="MethodCode">کد پایدار روش.</param>
/// <param name="ServiceCode">کد سرویس قدیمیِ پیوند تغییرناپذیر.</param>
/// <param name="OptionCode">کد گزینهٔ پیوند تغییرناپذیر (هرگز تهی نیست).</param>
/// <param name="IconKey">آیکن سرویس والد در کاتالوگ قدیمی.</param>
/// <param name="SortOrder">ترتیب نمایش ادمین.</param>
public sealed record ShippingOneLevelCanonicalMethodSnapshot(
    string MethodCode,
    string ServiceCode,
    string OptionCode,
    string IconKey,
    int SortOrder);

/// <summary>
/// درز فقط-خواندنیِ هویت روش‌های یک‌سطحیِ بدون ردیف گزینهٔ قدیمی (روش تازه‌ساختهٔ canonical).
/// </summary>
/// <remarks>
/// <para>
/// روشی که ادمین در کاتالوگ یک‌سطحی می‌سازد ردیف <c>shipping_methods</c> دارد اما هیچ ردیف
/// <c>shipping_service_options</c> متناظر ندارد؛ ساختن ردیف قدیمی جعلی صرفاً برای عبور از نگهبان
/// ممنوع است. این درز همان تفاوت را بدون هیچ نوشتنی اعلام می‌کند تا مصرف‌کننده بتواند هویت واقعی
/// روش را بشناسد، بدون آنکه «قابل انتخاب بودن» یا قیمت را از Fulfillment بگیرد.
/// </para>
/// <para>
/// fail-closed: دروازهٔ کاتالوگ یک‌سطحی خاموش، جدول مهاجرت‌نشده یا کد نامعتبر ⇒ فهرست خالی و
/// <c>false</c>. هیچ ردیفی ساخته، فعال یا قیمت‌گذاری نمی‌شود.
/// </para>
/// </remarks>
public interface IShippingOneLevelCanonicalMethodReader
{
    /// <summary>
    /// روش‌های فعالِ canonical که سرویس والدشان فعال است و هیچ ردیف گزینهٔ قدیمی ندارند.
    /// </summary>
    Task<IReadOnlyList<ShippingOneLevelCanonicalMethodSnapshot>> ListActiveUnboundAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// آیا یک روش canonical با همین سرویس/گزینهٔ پیوند وجود دارد؟ این پرسش فقط برای پذیرش
    /// «کلید نرخ» مالک Shipping است و هیچ فعال‌بودن یا قیمتی را تأیید نمی‌کند.
    /// </summary>
    Task<bool> IsCanonicalMethodBindingAsync(
        string serviceCode,
        string? optionCode,
        CancellationToken cancellationToken);
}
