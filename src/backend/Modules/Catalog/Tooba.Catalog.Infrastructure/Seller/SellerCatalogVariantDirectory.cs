using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Application.Seller.Models;
using Tooba.Catalog.Application.Seller.Ports;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Seller;

/// <summary>
/// Catalog persistence for the published variant picker. Search and paging are executed in SQL;
/// the old newest-100-product list remains only for compatibility with existing callers.
/// </summary>
public sealed class SellerCatalogVariantDirectory(CatalogDbContext db) : ISellerCatalogVariantDirectory
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SellerCatalogVariantOption>> ListPublishedVariantsAsync(
        CancellationToken cancellationToken)
    {
        var page = await SearchPublishedVariantsAsync(null, 1, 100, cancellationToken);
        return page.Items;
    }

    /// <inheritdoc />
    public async Task<SellerCatalogVariantPage> SearchPublishedVariantsAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);
        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var query = from variant in db.Variants.AsNoTracking()
                    join product in db.Products.AsNoTracking() on variant.ProductId equals product.ProductId
                    where product.Status == CatalogPublicationStatus.Published
                    select new { variant, product };
        if (term is not null)
        {
            var pattern = $"%{term}%";
            query = query.Where(row =>
                (row.variant.CatalogCodeSeam != null && EF.Functions.ILike(row.variant.CatalogCodeSeam, pattern))
                || db.LocalizedTexts.Any(text =>
                    text.OwnerKind == CatalogLocalizedOwnerKind.Product
                    && text.OwnerId == row.product.ProductId
                    && text.FieldKey == "name"
                    && EF.Functions.ILike(text.Value, pattern)));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(row => row.product.UpdatedAt)
            .ThenBy(row => row.variant.CatalogCodeSeam)
            .Skip((normalizedPage - 1) * normalizedSize)
            .Take(normalizedSize)
            .ToListAsync(cancellationToken);
        var productIds = rows.Select(row => row.product.ProductId).Distinct().ToArray();
        var names = await db.LocalizedTexts.AsNoTracking()
            .Where(text => text.OwnerKind == CatalogLocalizedOwnerKind.Product
                           && productIds.Contains(text.OwnerId)
                           && text.FieldKey == "name")
            .ToListAsync(cancellationToken);
        var nameMap = names.GroupBy(text => text.OwnerId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(text => text.Locale.StartsWith("fa") ? 0 : 1).First().Value);
        var items = rows.Select(row => new SellerCatalogVariantOption(
            row.variant.VariantId,
            row.product.ProductId,
            nameMap.GetValueOrDefault(row.product.ProductId) ?? string.Empty,
            row.variant.CatalogCodeSeam,
            row.product.Status.ToString())).ToArray();
        return new SellerCatalogVariantPage(
            items,
            total,
            normalizedPage,
            normalizedSize,
            normalizedPage * normalizedSize < total);
    }
}
