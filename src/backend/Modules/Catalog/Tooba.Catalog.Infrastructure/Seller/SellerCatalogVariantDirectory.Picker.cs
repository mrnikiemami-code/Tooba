using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Application.Seller.Models;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Seller;

/// <summary>
/// Product/variant offer-picker reads: published Catalog products plus localized human variant labels.
/// No price, stock, seller or mutable state is exposed here.
/// </summary>
public sealed partial class SellerCatalogVariantDirectory
{
    /// <inheritdoc />
    public async Task<CatalogPickerProductPage> SearchPublishedProductsAsync(
        string? search,
        int page,
        int pageSize,
        string locale,
        CancellationToken cancellationToken)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);
        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var query = db.Products.AsNoTracking()
            .Where(product => product.Status == CatalogPublicationStatus.Published);
        if (term is not null)
        {
            var pattern = $"%{term}%";
            query = query.Where(product => db.LocalizedTexts.Any(text =>
                text.OwnerKind == CatalogLocalizedOwnerKind.Product
                && text.OwnerId == product.ProductId
                && text.FieldKey == "name"
                && EF.Functions.ILike(text.Value, pattern)));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(product => product.UpdatedAt)
            .ThenBy(product => product.ProductId)
            .Skip((normalizedPage - 1) * normalizedSize)
            .Take(normalizedSize)
            .ToListAsync(cancellationToken);
        var productIds = rows.Select(row => row.ProductId).ToArray();
        var names = await db.LocalizedTexts.AsNoTracking()
            .Where(text => text.OwnerKind == CatalogLocalizedOwnerKind.Product
                           && productIds.Contains(text.OwnerId)
                           && text.FieldKey == "name")
            .ToListAsync(cancellationToken);
        var nameMap = names.GroupBy(text => text.OwnerId)
            .ToDictionary(group => group.Key, group => PickLocalized(group, locale));
        var brandIds = rows.Where(row => row.BrandId != null).Select(row => row.BrandId!.Value).Distinct().ToArray();
        var brands = await db.LocalizedTexts.AsNoTracking()
            .Where(text => text.OwnerKind == CatalogLocalizedOwnerKind.Brand
                           && brandIds.Contains(text.OwnerId)
                           && text.FieldKey == "name")
            .ToListAsync(cancellationToken);
        var brandMap = brands.GroupBy(text => text.OwnerId)
            .ToDictionary(group => group.Key, group => PickLocalized(group, locale));
        var mediaRows = await db.MediaReferences.AsNoTracking()
            .Where(row => productIds.Contains(row.ProductId) && row.IsPrimary)
            .Select(row => new { row.ProductId, row.MediaAssetId, row.DisplayOrder })
            .ToListAsync(cancellationToken);
        var primaryMedia = mediaRows.GroupBy(row => row.ProductId)
            .ToDictionary(group => group.Key, group => group.OrderBy(row => row.DisplayOrder).First().MediaAssetId);
        var variantCounts = await db.Variants.AsNoTracking()
            .Where(variant => productIds.Contains(variant.ProductId) && variant.Status == CatalogPublicationStatus.Published)
            .GroupBy(variant => variant.ProductId)
            .Select(group => new { ProductId = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var countMap = variantCounts.ToDictionary(row => row.ProductId, row => row.Count);
        var unitIds = rows.Select(row => row.UnitOfMeasureId).Distinct().ToArray();
        var units = await db.UnitsOfMeasure.AsNoTracking().Where(unit => unitIds.Contains(unit.UnitOfMeasureId)).ToListAsync(cancellationToken);
        var unitTranslations = await db.UnitOfMeasureTranslations.AsNoTracking().Where(row => unitIds.Contains(row.UnitOfMeasureId)).ToListAsync(cancellationToken);
        var unitMap = units.ToDictionary(unit => unit.UnitOfMeasureId);
        var unitTranslationMap = unitTranslations.GroupBy(row => row.UnitOfMeasureId).ToDictionary(group => group.Key, group => group.First());
        var roundingMode = await db.StoreQuantitySettings.AsNoTracking().Select(row => (string?)row.RoundingMode.ToString()).FirstOrDefaultAsync(cancellationToken);

        var items = rows.Select(row => new CatalogPickerProductOption(
            row.ProductId,
            nameMap.GetValueOrDefault(row.ProductId) ?? string.Empty,
            row.BrandId is Guid brandId ? brandMap.GetValueOrDefault(brandId) : null,
            primaryMedia.TryGetValue(row.ProductId, out var mediaId) ? mediaId : null,
            countMap.GetValueOrDefault(row.ProductId, 0),
            unitMap.TryGetValue(row.UnitOfMeasureId, out var unit) ? unit.Code : null,
            unitTranslationMap.TryGetValue(row.UnitOfMeasureId, out var translation) ? translation.Name : unit?.Code,
            row.QuantityDecimalPlaces,
            row.QuantityStep,
            roundingMode)).ToArray();
        return new CatalogPickerProductPage(
            items,
            total,
            normalizedPage,
            normalizedSize,
            normalizedPage * normalizedSize < total);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CatalogPickerVariantOption>> ListPublishedVariantsForProductAsync(
        Guid productId,
        string locale,
        CancellationToken cancellationToken)
    {
        var variants = await db.Variants.AsNoTracking()
            .Include(variant => variant.AttributeValues)
            .Where(variant => variant.ProductId == productId
                              && variant.Status == CatalogPublicationStatus.Published)
            .OrderBy(variant => variant.SortOrder)
            .ThenBy(variant => variant.CreatedAt)
            .ToListAsync(cancellationToken);
        if (variants.Count == 0)
        {
            return [];
        }

        var valueRows = variants.SelectMany(variant => variant.AttributeValues).ToList();
        var definitionIds = valueRows.Select(value => value.DefinitionId).Distinct().ToArray();
        var optionIds = new List<Guid>();
        foreach (var value in valueRows)
        {
            if (TryOptionId(value.CanonicalValue, out var optionId))
            {
                optionIds.Add(optionId);
            }
        }

        var definitionNames = await ReadDefinitionNamesAsync(definitionIds, locale, cancellationToken);
        var optionNames = await ReadOptionNamesAsync(optionIds.Distinct().ToArray(), locale, cancellationToken);
        return variants.Select(variant =>
        {
            var labels = variant.AttributeValues
                .OrderBy(value => value.DefinitionId)
                .Select(value =>
                {
                    var name = definitionNames.GetValueOrDefault(value.DefinitionId) ?? value.DefinitionId.ToString("N");
                    var label = value.CanonicalValue;
                    if (TryOptionId(value.CanonicalValue, out var optionId))
                    {
                        label = optionNames.GetValueOrDefault(optionId) ?? value.CanonicalValue;
                    }
                    return new CatalogPickerAxisLabel(name, label);
                })
                .ToArray();
            var display = labels.Length == 0
                ? string.Empty
                : string.Join(" · ", labels.Select(label => $"{label.Name}: {label.Value}"));
            return new CatalogPickerVariantOption(
                variant.VariantId,
                variant.ProductId,
                display,
                labels,
                variant.CatalogCodeSeam,
                variant.Status.ToString());
        }).ToArray();
    }

    private static bool TryOptionId(string canonicalValue, out Guid optionId) =>
        Guid.TryParseExact(canonicalValue, "N", out optionId) || Guid.TryParse(canonicalValue, out optionId);

    private static string PickLocalized(IEnumerable<CatalogLocalizedText> rows, string locale)
    {
        var normalizedLocale = string.IsNullOrWhiteSpace(locale) ? "fa-IR" : locale.Trim();
        var language = normalizedLocale.Split('-')[0];
        var ordered = rows
            .OrderByDescending(row => string.Equals(row.Locale, normalizedLocale, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(row => row.Locale.StartsWith(language, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(row => row.Locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(row => row.Locale.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            .ThenBy(row => row.Locale, StringComparer.Ordinal)
            .FirstOrDefault();
        return ordered?.Value ?? string.Empty;
    }

    private async Task<IReadOnlyDictionary<Guid, string>> ReadDefinitionNamesAsync(
        IReadOnlyCollection<Guid> ids,
        string locale,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var rows = await db.LocalizedTexts.AsNoTracking()
            .Where(text => text.OwnerKind == CatalogLocalizedOwnerKind.AttributeDefinition
                           && text.FieldKey == "name"
                           && ids.Contains(text.OwnerId))
            .ToListAsync(cancellationToken);
        var names = rows.GroupBy(row => row.OwnerId)
            .ToDictionary(group => group.Key, group => PickLocalized(group, locale));
        var definitions = await db.AttributeDefinitions.AsNoTracking()
            .Where(definition => ids.Contains(definition.DefinitionId))
            .Select(definition => new { definition.DefinitionId, definition.Code })
            .ToListAsync(cancellationToken);
        foreach (var definition in definitions)
        {
            names.TryAdd(definition.DefinitionId, definition.Code);
        }

        return names;
    }

    private async Task<IReadOnlyDictionary<Guid, string>> ReadOptionNamesAsync(
        IReadOnlyCollection<Guid> ids,
        string locale,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var rows = await db.LocalizedTexts.AsNoTracking()
            .Where(text => text.OwnerKind == CatalogLocalizedOwnerKind.AttributeOption
                           && text.FieldKey == "name"
                           && ids.Contains(text.OwnerId))
            .ToListAsync(cancellationToken);
        var names = rows.GroupBy(row => row.OwnerId)
            .ToDictionary(group => group.Key, group => PickLocalized(group, locale));
        var options = await db.AttributeOptions.AsNoTracking()
            .Where(option => ids.Contains(option.OptionId))
            .Select(option => new { option.OptionId, option.Code })
            .ToListAsync(cancellationToken);
        foreach (var option in options)
        {
            names.TryAdd(option.OptionId, option.Code);
        }

        return names;
    }
}
