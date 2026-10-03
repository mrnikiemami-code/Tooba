using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks.Grid;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Grid;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Offer.Contracts.Ports;
using Tooba.Pricing.Contracts;

namespace Tooba.Catalog.Infrastructure.Directories;

/// <summary>
/// Catalog-owned Admin ProductWorkspace list/grid gateway (page IDs + catalog list slices).
/// </summary>
public sealed class CatalogAdminProductWorkspaceListGateway(
    CatalogDbContext catalog,
    IOfferQueryGateway offers,
    IPriceQueryGateway prices,
    IInventoryQueryGateway inventory) : ICatalogAdminProductWorkspaceListGateway
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> ListRecentProductIdsAsync(int take, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(take, 1, 500);
        return await catalog.Products.AsNoTracking()
            .OrderByDescending(x => x.UpdatedAt)
            .Take(limit)
            .Select(x => x.ProductId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<(IReadOnlyList<Guid> PageIds, int TotalCount)> ResolveGridPageProductIdsAsync(
        GridQueryRequest query,
        CancellationToken cancellationToken)
    {
        var engine = new AdminProductGridQueryEngine(catalog, offers, prices, inventory);
        return engine.ResolvePageProductIdsAsync(query, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CatalogAdminProductListSlice>> LoadListCatalogSlicesAsync(
        IReadOnlyList<Guid> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        var products = await catalog.Products.AsNoTracking()
            .Where(x => productIds.Contains(x.ProductId))
            .ToListAsync(cancellationToken);
        var byId = products.ToDictionary(x => x.ProductId);
        products = productIds.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
        var names = await LoadNamesAsync(CatalogLocalizedOwnerKind.Product, productIds, cancellationToken);
        var variantRows = await catalog.Variants.AsNoTracking()
            .Where(x => productIds.Contains(x.ProductId))
            .Select(x => new { x.ProductId, x.VariantId })
            .ToListAsync(cancellationToken);
        var categoryLinks = await catalog.ProductCategories.AsNoTracking()
            .Where(x => productIds.Contains(x.ProductId))
            .ToListAsync(cancellationToken);
        var leafCategoryIds = categoryLinks.Select(x => x.CategoryId).Distinct().ToList();
        var categoryLeafNames = leafCategoryIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await LoadNamesAsync(CatalogLocalizedOwnerKind.Category, leafCategoryIds, cancellationToken);
        var brandIds = products
            .Where(p => p.BrandId is Guid)
            .Select(p => p.BrandId!.Value)
            .Distinct()
            .ToList();
        var brandNames = brandIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await LoadNamesAsync(CatalogLocalizedOwnerKind.Brand, brandIds, cancellationToken);
        var mediaRows = await catalog.MediaReferences.AsNoTracking()
            .Where(x => productIds.Contains(x.ProductId))
            .ToListAsync(cancellationToken);

        return products.Select(product =>
        {
            var variantIds = variantRows.Where(v => v.ProductId == product.ProductId).Select(v => v.VariantId).ToList();
            var productLinks = categoryLinks
                .Where(link => link.ProductId == product.ProductId)
                .ToList();
            var primaryLink = productLinks.FirstOrDefault(link => link.Role == CatalogProductCategoryRole.Primary);
            var primaryName = primaryLink is null
                ? null
                : categoryLeafNames.GetValueOrDefault(primaryLink.CategoryId);
            var additionalNames = productLinks
                .Where(link => link.Role == CatalogProductCategoryRole.Additional)
                .OrderBy(link => link.CategoryId)
                .Select(link => categoryLeafNames.GetValueOrDefault(link.CategoryId))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            var summaryParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(primaryName))
            {
                summaryParts.Add(primaryName!);
            }

            summaryParts.AddRange(additionalNames);
            var productMedia = mediaRows
                .Where(m => m.ProductId == product.ProductId)
                .OrderByDescending(m => m.IsPrimary)
                .ThenBy(m => m.DisplayOrder)
                .ToList();
            var primaryMedia = productMedia.FirstOrDefault(m => m.IsPrimary) ?? productMedia.FirstOrDefault();
            var brandLabel = product.BrandId is Guid bid
                ? (brandNames.GetValueOrDefault(bid) ?? "برند")
                : "بدون برند";
            return new CatalogAdminProductListSlice(
                product.ProductId,
                names.GetValueOrDefault(product.ProductId) ?? product.SlugSeam ?? product.ProductId.ToString("N")[..8],
                product.Status.ToString(),
                product.UpdatedAt,
                variantIds,
                summaryParts.Count == 0 ? "بدون دسته" : string.Join("، ", summaryParts),
                primaryMedia?.MediaAssetId,
                primaryLink?.CategoryId,
                brandLabel,
                primaryName,
                additionalNames,
                additionalNames.Count);
        }).ToList();
    }

    private async Task<Dictionary<Guid, string>> LoadNamesAsync(
        CatalogLocalizedOwnerKind kind,
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var rows = await catalog.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == kind && ids.Contains(x.OwnerId) && x.FieldKey == "name")
            .ToListAsync(cancellationToken);
        return rows
            .GroupBy(x => x.OwnerId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.Locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase) ? 0 : 1).First().Value);
    }
}
