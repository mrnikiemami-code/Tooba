using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Application.Seller.Models;
using Tooba.Catalog.Application.Seller.Ports;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Seller;

/// <summary>
/// Catalog persistence for the seller published-variant selection list.
/// Published products only, newest 100 by UpdatedAt, Persian-first localized product name,
/// variants ordered by CatalogCodeSeam — preserved exactly from the evacuated Host composer.
/// </summary>
public sealed class SellerCatalogVariantDirectory(CatalogDbContext db) : ISellerCatalogVariantDirectory
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SellerCatalogVariantOption>> ListPublishedVariantsAsync(
        CancellationToken cancellationToken)
    {
        var products = await db.Products.AsNoTracking()
            .Where(x => x.Status == CatalogPublicationStatus.Published)
            .OrderByDescending(x => x.UpdatedAt).Take(100).ToListAsync(cancellationToken);
        var ids = products.Select(x => x.ProductId).ToArray();
        var names = await db.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Product
                        && ids.Contains(x.OwnerId) && x.FieldKey == "name")
            .ToListAsync(cancellationToken);
        var nameMap = names.GroupBy(x => x.OwnerId).ToDictionary(
            x => x.Key, x => x.OrderBy(y => y.Locale.StartsWith("fa") ? 0 : 1).First().Value);
        var variants = await db.Variants.AsNoTracking()
            .Where(x => ids.Contains(x.ProductId)).OrderBy(x => x.CatalogCodeSeam).ToListAsync(cancellationToken);
        var statuses = products.ToDictionary(x => x.ProductId, x => x.Status.ToString());
        return variants.Select(x => new SellerCatalogVariantOption(
            x.VariantId, x.ProductId, nameMap.GetValueOrDefault(x.ProductId) ?? string.Empty,
            x.CatalogCodeSeam, statuses.GetValueOrDefault(x.ProductId) ?? "Published")).ToArray();
    }
}
