using Tooba.Catalog.Application.Seller.Models;

namespace Tooba.Catalog.Application.Seller.Ports;

/// <summary>Catalog-owned persistence seam for the seller/admin published-variant picker.</summary>
public interface ISellerCatalogVariantDirectory
{
    /// <summary>Legacy newest-100 compatibility list.</summary>
    Task<IReadOnlyList<SellerCatalogVariantOption>> ListPublishedVariantsAsync(CancellationToken cancellationToken);

    /// <summary>Server search/page over published products and their variants.</summary>
    Task<SellerCatalogVariantPage> SearchPublishedVariantsAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

