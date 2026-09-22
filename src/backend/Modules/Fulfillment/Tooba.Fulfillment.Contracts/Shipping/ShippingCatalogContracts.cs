namespace Tooba.Fulfillment.Contracts.Shipping;

public sealed record ShippingCatalogTranslationSnapshot(Guid LanguageId, string Name, string? Description);

public sealed record ShippingCatalogOptionTranslationSnapshot(Guid LanguageId, string Name);

public sealed record ShippingCatalogOptionSnapshot(
    Guid ShippingServiceOptionId,
    Guid ShippingServiceId,
    string Code,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<ShippingCatalogOptionTranslationSnapshot> Translations);

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

/// <summary>Read shipping catalog without Fulfillment.Application / DbContext leakage.</summary>
public interface IShippingCatalogReader
{
    Task<IReadOnlyList<ShippingCatalogServiceSnapshot>> ListAsync(CancellationToken cancellationToken);
    Task<ShippingCatalogServiceSnapshot?> GetAsync(Guid serviceId, CancellationToken cancellationToken);
}

/// <summary>Ensures the shipping catalog seed exists (Order storefront projection seam).</summary>
public interface IShippingCatalogSeedPort
{
    Task EnsureSeedAsync(CancellationToken cancellationToken);
}
