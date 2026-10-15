namespace Tooba.Fulfillment.Contracts.Shipping;

/// <summary>ترجمهٔ نام روش یک‌سطحی برای خواندن کاتالوگ.</summary>
public sealed record ShippingOneLevelMethodTranslationSnapshot(Guid LanguageId, string Name, string? Description);

/// <summary>
/// یک روش ارسال یک‌سطحی خواندنی. هیچ نوع EF/DbContext در این قرارداد نیست.
/// </summary>
/// <param name="ShippingMethodId">شناسه پایدار روش.</param>
/// <param name="MethodCode">کد عمومی پایدار روش (حداکثر ۳۲ کاراکتر).</param>
/// <param name="ProviderKind">نوع ارائه‌دهنده؛ metadata پنهان، نه سطح دوم پیکربندی.</param>
/// <param name="LegacyServiceCode">کد سرویس قدیمی (پیوند سازگاری، تغییرناپذیر).</param>
/// <param name="LegacyOptionCode">کد گزینهٔ قدیمی؛ تهی یعنی سرویس بدون گزینه.</param>
/// <param name="IsActive">فعال بودن برای انتخاب.</param>
/// <param name="SortOrder">ترتیب نمایش.</param>
/// <param name="Translations">ترجمه‌های نام.</param>
public sealed record ShippingOneLevelMethodSnapshot(
    Guid ShippingMethodId,
    string MethodCode,
    string ProviderKind,
    string LegacyServiceCode,
    string? LegacyOptionCode,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<ShippingOneLevelMethodTranslationSnapshot> Translations);

/// <summary>
/// خواندن کاتالوگ روش یک‌سطحی Fulfillment بدون افشای EF model.
/// </summary>
/// <remarks>
/// این درز فقط-خواندنی است و هیچ مسیر قیمت/انتخاب/اسنپ‌شات را تغییر نمی‌دهد. تا زمانی که دروازهٔ
/// <see cref="ShippingOneLevelCatalogOptions"/> خاموش است، مصرف‌کننده‌ای در فروشگاه به آن وصل نیست.
/// </remarks>
public interface IShippingOneLevelCatalogReader
{
    /// <summary>همهٔ روش‌ها، بدون فیلتر فعال بودن.</summary>
    Task<IReadOnlyList<ShippingOneLevelMethodSnapshot>> ListAsync(CancellationToken cancellationToken);

    /// <summary>روش‌های فعال، مرتب‌شده.</summary>
    Task<IReadOnlyList<ShippingOneLevelMethodSnapshot>> ListActiveAsync(CancellationToken cancellationToken);

    /// <summary>یک روش با کد پایدار؛ تهی یعنی وجود ندارد.</summary>
    Task<ShippingOneLevelMethodSnapshot?> GetByCodeAsync(string methodCode, CancellationToken cancellationToken);
}

/// <summary>
/// دروازهٔ کاتالوگ یک‌سطحی. پیش‌فرض <b>خاموش</b> است و هیچ بخشی در appsettings ندارد.
/// </summary>
/// <remarks>
/// این دروازه فقط مسیر خواندن کاتالوگ/UI آینده را کنترل می‌کند و نباید
/// <c>Tooba:ShippingRateAuthority</c> یا <c>Tooba:ShippingOperationalShadow</c> را بخواند، القا کند یا
/// تغییر دهد. روشن‌بودن آن هیچ قیمت یا اسنپ‌شات پرداخت‌شده‌ای را عوض نمی‌کند.
/// </remarks>
public sealed class ShippingOneLevelCatalogOptions
{
    /// <summary>نام بخش پیکربندی؛ در appsettings وجود ندارد پس مقدار پیش‌فرض اعمال می‌شود.</summary>
    public const string SectionName = "Tooba:ShippingOneLevelCatalog";

    /// <summary>آیا مسیر کاتالوگ یک‌سطحی روشن است. پیش‌فرض خاموش.</summary>
    public bool Enabled { get; set; }
}

/// <summary>وضعیت دروازهٔ کاتالوگ یک‌سطحی.</summary>
public enum ShippingOneLevelCatalogGate
{
    /// <summary>خاموش؛ رفتار پیش از این قابلیت، بی‌تغییر.</summary>
    Disabled = 0,

    /// <summary>روشن؛ فقط مسیر خواندن کاتالوگ/UI یک‌سطحی مجاز است.</summary>
    Enabled = 1,
}

/// <summary>سیاست fail-closed دروازهٔ کاتالوگ یک‌سطحی.</summary>
public static class ShippingOneLevelCatalogPolicy
{
    /// <summary>وضعیت دروازه را از تنظیمات تعیین می‌کند؛ تهی یعنی خاموش.</summary>
    public static ShippingOneLevelCatalogGate Evaluate(ShippingOneLevelCatalogOptions? options) =>
        options?.Enabled == true
            ? ShippingOneLevelCatalogGate.Enabled
            : ShippingOneLevelCatalogGate.Disabled;
}
