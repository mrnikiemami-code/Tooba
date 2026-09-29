using Tooba.Catalog.Application.Seller.Models;

namespace Tooba.Catalog.Application.Seller.Ports;

/// <summary>Catalog-owned persistence seam for the seller published-variant selection list.</summary>
public interface ISellerCatalogVariantDirectory
{
    /// <summary>
    /// Published products only, newest 100 by UpdatedAt, Persian-first localized product name,
    /// variants ordered by CatalogCodeSeam.
    /// </summary>
    Task<IReadOnlyList<SellerCatalogVariantOption>> ListPublishedVariantsAsync(
        CancellationToken cancellationToken);
}
