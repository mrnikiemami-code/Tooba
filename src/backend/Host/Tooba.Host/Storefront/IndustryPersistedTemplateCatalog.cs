using Tooba.Host.Admin;

namespace Tooba.Host.Storefront;

/// <summary>Facade over Batch A/B persisted industry Template Catalog seeds.</summary>
internal static class IndustryPersistedTemplateCatalog
{
    public const string LocaleFa = IndustryBatchATemplateCatalogSeed.LocaleFa;

    public static bool IsSupported(string templateKey) =>
        IndustryBatchATemplateCatalogSeed.SupportedKeys.Contains(templateKey)
        || IndustryBatchBTemplateCatalogSeed.SupportedKeys.Contains(templateKey);

    public static string OriginFor(string templateKey)
    {
        if (IndustryBatchATemplateCatalogSeed.SupportedKeys.Contains(templateKey))
        {
            return IndustryBatchATemplateCatalogSeed.OriginFor(templateKey);
        }

        return IndustryBatchBTemplateCatalogSeed.OriginFor(templateKey);
    }

    public static string MediaPublicUrl(string templateKey, int index)
    {
        if (IndustryBatchATemplateCatalogSeed.SupportedKeys.Contains(templateKey))
        {
            return IndustryBatchATemplateCatalogSeed.MediaPublicUrl(templateKey, index);
        }

        return IndustryBatchBTemplateCatalogSeed.MediaPublicUrl(templateKey, index);
    }

    public static string? TryResolveMedia(string templateKey, Guid mediaAssetId)
    {
        if (IndustryBatchATemplateCatalogSeed.SupportedKeys.Contains(templateKey))
        {
            return IndustryBatchATemplateCatalogSeed.TryResolveMedia(templateKey, mediaAssetId);
        }

        if (IndustryBatchBTemplateCatalogSeed.SupportedKeys.Contains(templateKey))
        {
            return IndustryBatchBTemplateCatalogSeed.TryResolveMedia(templateKey, mediaAssetId);
        }

        return null;
    }
}
