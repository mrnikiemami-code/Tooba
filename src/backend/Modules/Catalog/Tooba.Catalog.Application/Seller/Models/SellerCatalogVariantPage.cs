namespace Tooba.Catalog.Application.Seller.Models;

/// <summary>Bounded server page for the published Catalog variant picker.</summary>
public sealed record SellerCatalogVariantPage(
    IReadOnlyList<SellerCatalogVariantOption> Items,
    int TotalCount,
    int Page,
    int PageSize,
    bool HasMore);
