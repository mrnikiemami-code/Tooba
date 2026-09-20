using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure;

/// <summary>Reads Catalog-owned descriptive data for cross-module offer presentation.</summary>
public sealed class CatalogOfferReadGateway(CatalogDbContext db) : ICatalogOfferReadGateway
{
    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, CatalogOfferPresentation>> GetOfferPresentationsAsync(
        IReadOnlyCollection<Guid> catalogVariantIds,
        CancellationToken cancellationToken)
    {
        if (catalogVariantIds.Count == 0)
            return new Dictionary<Guid, CatalogOfferPresentation>();

        var ids = catalogVariantIds.Distinct().ToArray();
        var variants = await db.Variants.AsNoTracking()
            .Where(x => ids.Contains(x.VariantId))
            .Select(x => new { x.VariantId, x.ProductId })
            .ToListAsync(cancellationToken);
        var productIds = variants.Select(x => x.ProductId).Distinct().ToArray();
        var products = await db.Products.AsNoTracking()
            .Where(x => productIds.Contains(x.ProductId))
            .Select(x => new { x.ProductId, x.BrandId, x.UnitOfMeasureId })
            .ToListAsync(cancellationToken);
        var names = await db.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Product
                        && productIds.Contains(x.OwnerId) && x.FieldKey == "name")
            .ToListAsync(cancellationToken);
        var titleMap = names.GroupBy(x => x.OwnerId).ToDictionary(
            x => x.Key,
            x => x.OrderBy(y => y.Locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase) ? 0 : 1).First().Value);
        var brandIds = products.Where(x => x.BrandId != null).Select(x => x.BrandId!.Value).Distinct().ToArray();
        var brands = await db.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Brand
                        && brandIds.Contains(x.OwnerId) && x.FieldKey == "name")
            .ToListAsync(cancellationToken);
        var brandMap = brands.GroupBy(x => x.OwnerId).ToDictionary(x => x.Key, x => x.OrderBy(y => y.Locale).First().Value);
        var unitIds = products.Select(x => x.UnitOfMeasureId).Distinct().ToArray();
        var units = await db.UnitsOfMeasure.AsNoTracking()
            .Where(x => unitIds.Contains(x.UnitOfMeasureId)).ToListAsync(cancellationToken);
        var translations = await db.UnitOfMeasureTranslations.AsNoTracking()
            .Where(x => unitIds.Contains(x.UnitOfMeasureId)).ToListAsync(cancellationToken);
        var productMap = products.ToDictionary(x => x.ProductId);
        var unitMap = units.ToDictionary(x => x.UnitOfMeasureId);
        var translationMap = translations.GroupBy(x => x.UnitOfMeasureId).ToDictionary(x => x.Key, x => x.First());

        return variants.ToDictionary(x => x.VariantId, variant =>
        {
            var product = productMap[variant.ProductId];
            unitMap.TryGetValue(product.UnitOfMeasureId, out var unit);
            translationMap.TryGetValue(product.UnitOfMeasureId, out var translation);
            var brand = product.BrandId is Guid brandId ? brandMap.GetValueOrDefault(brandId) : null;
            return new CatalogOfferPresentation(
                variant.VariantId, variant.ProductId,
                titleMap.GetValueOrDefault(variant.ProductId) ?? string.Empty,
                brand, unit?.Code, translation?.Name ?? unit?.Code, translation?.ShortName ?? unit?.Code);
        });
    }
}
