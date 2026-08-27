using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;

namespace Tooba.Host.Tests;

/// <summary>
/// درز Catalog آزمایشی برای Access Control بدون DbContext واقعی Catalog.
/// </summary>
internal sealed class FakeCatalogLookupGateway : ICatalogLookupGateway
{
    private readonly Dictionary<Guid, CategoryReference> _categories = new();
    private readonly Dictionary<Guid, string> _categoryNames = new();
    private readonly Dictionary<Guid, Guid?> _variantCategories = new();

    /// <summary>ردهٔ شناخته‌شده را ثبت می‌کند.</summary>
    public void AddCategory(Guid categoryId, string name, Guid? parentCategoryId = null, CatalogPublicationStatus status = CatalogPublicationStatus.Published)
    {
        _categories[categoryId] = new CategoryReference(categoryId, parentCategoryId, status);
        _categoryNames[categoryId] = name;
    }

    /// <summary>نگاشت گونه→رده برای backfill تست.</summary>
    public void MapVariant(Guid variantId, Guid? categoryId) => _variantCategories[variantId] = categoryId;

    /// <inheritdoc />
    public Task<ProductReference?> FindProductAsync(Guid productId, CancellationToken cancellationToken) =>
        Task.FromResult<ProductReference?>(null);

    /// <inheritdoc />
    public Task<VariantReference?> FindVariantAsync(Guid variantId, CancellationToken cancellationToken) =>
        Task.FromResult<VariantReference?>(null);

    /// <inheritdoc />
    public Task<CategoryReference?> FindCategoryAsync(Guid categoryId, CancellationToken cancellationToken) =>
        Task.FromResult(_categories.TryGetValue(categoryId, out var c) ? c : null);

    /// <inheritdoc />
    public Task<ReviewableProductReference?> FindReviewableProductBySlugAsync(string slug, CancellationToken cancellationToken) =>
        Task.FromResult<ReviewableProductReference?>(null);

    /// <inheritdoc />
    public Task<ReviewableProductReference?> FindReviewableProductByIdAsync(Guid productId, CancellationToken cancellationToken) =>
        Task.FromResult<ReviewableProductReference?>(null);

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<Guid, string>> GetProductTitlesAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<Guid, string>> GetCategoryNamesAsync(
        IReadOnlyCollection<Guid> categoryIds,
        CancellationToken cancellationToken)
    {
        var result = categoryIds
            .Where(_categoryNames.ContainsKey)
            .ToDictionary(id => id, id => _categoryNames[id]);
        return Task.FromResult<IReadOnlyDictionary<Guid, string>>(result);
    }

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<Guid, ReviewableProductReference>> GetReviewableProductsByIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, ReviewableProductReference>>(new Dictionary<Guid, ReviewableProductReference>());

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<Guid, Guid?>> GetPrimaryCategoryIdsByVariantIdsAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken)
    {
        var result = variantIds.ToDictionary(
            id => id,
            id => _variantCategories.GetValueOrDefault(id));
        return Task.FromResult<IReadOnlyDictionary<Guid, Guid?>>(result);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AccessControlCategoryItem>> ListCategoriesForAccessControlAsync(
        string? search,
        CancellationToken cancellationToken)
    {
        IEnumerable<AccessControlCategoryItem> items = _categories.Values.Select(c =>
            new AccessControlCategoryItem(
                c.CategoryId,
                c.ParentCategoryId,
                _categoryNames.GetValueOrDefault(c.CategoryId) ?? "رده",
                c.Status.ToString()));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var needle = search.Trim();
            items = items.Where(i => i.Name.Contains(needle, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult<IReadOnlyList<AccessControlCategoryItem>>(items.ToList());
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AccessControlBrandItem>> ListBrandsForAccessControlAsync(
        string? search,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AccessControlBrandItem>>([]);

    /// <inheritdoc />
    public Task<IReadOnlyList<AccessControlProductItem>> ListProductsForAccessControlAsync(
        string? search,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AccessControlProductItem>>([]);
}
