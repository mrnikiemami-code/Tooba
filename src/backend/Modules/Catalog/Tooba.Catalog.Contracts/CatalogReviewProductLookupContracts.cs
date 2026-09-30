namespace Tooba.Catalog.Contracts;

/// <summary>مرجع محصول قابل‌بررسی برای Reviews بدون نشت Catalog.Application/Domain.</summary>
public sealed record CatalogReviewableProductDto(
    Guid ProductId,
    string Slug,
    string Title,
    string Status,
    IReadOnlyList<Guid> VariantIds);

/// <summary>مرز Contracts خواندن محصول برای Reviews (slug/id/batch).</summary>
public interface ICatalogReviewProductLookup
{
    /// <summary>محصول قابل‌بررسی با شناسه.</summary>
    Task<CatalogReviewableProductDto?> FindByIdAsync(Guid productId, CancellationToken cancellationToken);

    /// <summary>محصول قابل‌بررسی با slug پایدار.</summary>
    Task<CatalogReviewableProductDto?> FindBySlugAsync(string slug, CancellationToken cancellationToken);

    /// <summary>دستهٔ محصولات قابل‌بررسی برای ترکیب نظرات خانه.</summary>
    Task<IReadOnlyDictionary<Guid, CatalogReviewableProductDto>> GetByIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken);
}
