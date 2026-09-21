namespace Tooba.Fulfillment.Application.Shipping;

/// <summary>ترجمه سرویس ارسال برای خواندن کاتالوگ.</summary>
public sealed record ShippingCatalogTranslationSnapshot(Guid LanguageId, string Name, string? Description);

/// <summary>ترجمه گزینه.</summary>
public sealed record ShippingCatalogOptionTranslationSnapshot(Guid LanguageId, string Name);

/// <summary>گزینه سطح ۲.</summary>
public sealed record ShippingCatalogOptionSnapshot(
    Guid ShippingServiceOptionId,
    Guid ShippingServiceId,
    string Code,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<ShippingCatalogOptionTranslationSnapshot> Translations);

/// <summary>سرویس ارسال والد.</summary>
public sealed record ShippingCatalogServiceSnapshot(
    Guid ShippingServiceId,
    string Code,
    string ProviderKind,
    string IconKey,
    string ColorKey,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<ShippingCatalogTranslationSnapshot> Translations,
    IReadOnlyList<ShippingCatalogOptionSnapshot> Options);

/// <summary>خواندن کاتالوگ سرویس ارسال بدون افشای FulfillmentDbContext به Host.</summary>
public interface IShippingCatalogReader
{
    /// <summary>کل کاتالوگ (سرویس‌ها، ترجمه‌ها، گزینه‌ها).</summary>
    Task<IReadOnlyList<ShippingCatalogServiceSnapshot>> ListAsync(CancellationToken cancellationToken);

    /// <summary>یک سرویس با جزئیات.</summary>
    Task<ShippingCatalogServiceSnapshot?> GetAsync(Guid serviceId, CancellationToken cancellationToken);
}
