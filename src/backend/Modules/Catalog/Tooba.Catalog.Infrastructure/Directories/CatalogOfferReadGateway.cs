using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Directories;

/// <summary>Reads Catalog-owned descriptive data for cross-module offer presentation.</summary>
public sealed class CatalogOfferReadGateway(CatalogDbContext db) : ICatalogOfferReadGateway
{
    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, CatalogOfferPresentation>> GetOfferPresentationsAsync(
        IReadOnlyCollection<Guid> catalogVariantIds,
        CancellationToken cancellationToken,
        string? locale = null)
    {
        if (catalogVariantIds.Count == 0)
            return new Dictionary<Guid, CatalogOfferPresentation>();

        var ids = catalogVariantIds.Distinct().ToArray();
        var normalizedLocale = string.IsNullOrWhiteSpace(locale) ? "fa-IR" : locale.Trim();
        var variants = await db.Variants.AsNoTracking()
            .Where(x => ids.Contains(x.VariantId))
            .Select(x => new { x.VariantId, x.ProductId, x.CatalogCodeSeam })
            .ToListAsync(cancellationToken);
        var productIds = variants.Select(x => x.ProductId).Distinct().ToArray();
        var products = await db.Products.AsNoTracking()
            .Where(x => productIds.Contains(x.ProductId))
            .Select(x => new { x.ProductId, x.BrandId, x.UnitOfMeasureId, x.QuantityDecimalPlaces, x.QuantityStep })
            .ToListAsync(cancellationToken);
        var names = await db.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Product
                        && productIds.Contains(x.OwnerId) && x.FieldKey == "name")
            .ToListAsync(cancellationToken);
        var titleMap = names.GroupBy(x => x.OwnerId).ToDictionary(x => x.Key, x => PickLocalized(x, normalizedLocale));
        var brandIds = products.Where(x => x.BrandId != null).Select(x => x.BrandId!.Value).Distinct().ToArray();
        var brands = await db.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Brand
                        && brandIds.Contains(x.OwnerId) && x.FieldKey == "name")
            .ToListAsync(cancellationToken);
        var brandMap = brands.GroupBy(x => x.OwnerId).ToDictionary(x => x.Key, x => PickLocalized(x, normalizedLocale));
        var unitIds = products.Select(x => x.UnitOfMeasureId).Distinct().ToArray();
        var units = await db.UnitsOfMeasure.AsNoTracking()
            .Where(x => unitIds.Contains(x.UnitOfMeasureId)).ToListAsync(cancellationToken);
        var translations = await db.UnitOfMeasureTranslations.AsNoTracking()
            .Where(x => unitIds.Contains(x.UnitOfMeasureId)).ToListAsync(cancellationToken);
        var productMap = products.ToDictionary(x => x.ProductId);
        var unitMap = units.ToDictionary(x => x.UnitOfMeasureId);
        var translationMap = translations.GroupBy(x => x.UnitOfMeasureId).ToDictionary(x => x.Key, x => x.First());
        var primaryMediaRows = await db.MediaReferences.AsNoTracking()
            .Where(x => productIds.Contains(x.ProductId) && x.IsPrimary)
            .Select(x => new { x.ProductId, x.MediaAssetId, x.DisplayOrder })
            .ToListAsync(cancellationToken);
        var primaryMedia = primaryMediaRows
            .GroupBy(x => x.ProductId)
            .ToDictionary(x => x.Key, x => x.OrderBy(row => row.DisplayOrder).First().MediaAssetId);
        var variantLabels = await ReadVariantLabelsAsync(ids, normalizedLocale, cancellationToken);
        var roundingMode = await db.StoreQuantitySettings.AsNoTracking().Select(x => (string?)x.RoundingMode.ToString()).FirstOrDefaultAsync(cancellationToken);

        return variants.ToDictionary(x => x.VariantId, variant =>
        {
            var product = productMap[variant.ProductId];
            unitMap.TryGetValue(product.UnitOfMeasureId, out var unit);
            translationMap.TryGetValue(product.UnitOfMeasureId, out var translation);
            var brand = product.BrandId is Guid brandId ? brandMap.GetValueOrDefault(brandId) : null;
            return new CatalogOfferPresentation(
                variant.VariantId, variant.ProductId,
                titleMap.GetValueOrDefault(variant.ProductId) ?? string.Empty,
                brand, unit?.Code, translation?.Name ?? unit?.Code, translation?.ShortName ?? unit?.Code,
                variant.CatalogCodeSeam,
                primaryMedia.TryGetValue(variant.ProductId, out var mediaId) ? mediaId : null,
                variantLabels.GetValueOrDefault(variant.VariantId),
                product.QuantityDecimalPlaces,
                product.QuantityStep,
                roundingMode);
        });
    }

    private async Task<IReadOnlyDictionary<Guid, string>> ReadVariantLabelsAsync(
        IReadOnlyCollection<Guid> variantIds,
        string locale,
        CancellationToken cancellationToken)
    {
        if (variantIds.Count == 0) return new Dictionary<Guid, string>();
        var values = await db.Set<CatalogVariantAttributeValue>().AsNoTracking()
            .Where(value => variantIds.Contains(value.VariantId))
            .ToListAsync(cancellationToken);
        if (values.Count == 0) return new Dictionary<Guid, string>();
        var definitionIds = values.Select(value => value.DefinitionId).Distinct().ToArray();
        var optionIds = values
            .Select(value => Guid.TryParseExact(value.CanonicalValue, "N", out var id) || Guid.TryParse(value.CanonicalValue, out id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty).Distinct().ToArray();
        var definitionTexts = await db.LocalizedTexts.AsNoTracking()
            .Where(text => text.OwnerKind == CatalogLocalizedOwnerKind.AttributeDefinition && text.FieldKey == "name" && definitionIds.Contains(text.OwnerId))
            .ToListAsync(cancellationToken);
        var optionTexts = await db.LocalizedTexts.AsNoTracking()
            .Where(text => text.OwnerKind == CatalogLocalizedOwnerKind.AttributeOption && text.FieldKey == "name" && optionIds.Contains(text.OwnerId))
            .ToListAsync(cancellationToken);
        var definitionNames = definitionTexts.GroupBy(text => text.OwnerId).ToDictionary(group => group.Key, group => PickLocalized(group, locale));
        var optionNames = optionTexts.GroupBy(text => text.OwnerId).ToDictionary(group => group.Key, group => PickLocalized(group, locale));
        return values.GroupBy(value => value.VariantId).ToDictionary(group => group.Key, group =>
            string.Join(" · ", group.OrderBy(value => value.DefinitionId).Select(value =>
            {
                var definitionName = definitionNames.GetValueOrDefault(value.DefinitionId) ?? value.DefinitionId.ToString("N");
                var label = value.CanonicalValue;
                if (Guid.TryParseExact(value.CanonicalValue, "N", out var optionId) || Guid.TryParse(value.CanonicalValue, out optionId))
                    label = optionNames.GetValueOrDefault(optionId) ?? value.CanonicalValue;
                return $"{definitionName}: {label}";
            })));
    }
    private static string PickLocalized(IEnumerable<CatalogLocalizedText> rows, string locale)
    {
        var normalizedLocale = string.IsNullOrWhiteSpace(locale) ? "fa-IR" : locale.Trim();
        var language = normalizedLocale.Split('-')[0];
        return rows
            .OrderByDescending(row => string.Equals(row.Locale, normalizedLocale, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(row => row.Locale.StartsWith(language, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(row => row.Locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(row => row.Locale.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            .ThenBy(row => row.Locale, StringComparer.Ordinal)
            .FirstOrDefault()?.Value ?? string.Empty;
    }
}