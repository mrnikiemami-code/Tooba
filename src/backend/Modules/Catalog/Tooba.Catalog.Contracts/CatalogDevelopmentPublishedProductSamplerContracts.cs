namespace Tooba.Catalog.Contracts;

/// <summary>
/// نمونه‌گیری Development از محصولات Published متعلق به رده‌های متمایز — بدون نشت CatalogDbContext به ماژول‌های دیگر.
/// </summary>
public interface ICatalogDevelopmentPublishedProductSampler
{
    /// <summary>
    /// حداکثر <paramref name="take"/> شناسهٔ محصول Published از رده‌های متمایز
    /// (اولین محصول هر رده پس از OrderBy CategoryId/ProductId).
    /// </summary>
    Task<IReadOnlyList<Guid>> TakePublishedProductIdsFromDistinctCategoriesAsync(
        int take,
        CancellationToken cancellationToken);
}
