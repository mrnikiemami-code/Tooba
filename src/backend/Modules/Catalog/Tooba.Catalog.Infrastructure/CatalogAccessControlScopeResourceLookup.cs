using Tooba.Catalog.Application;
using Tooba.Catalog.Contracts;

namespace Tooba.Catalog.Infrastructure;

/// <summary>
/// آداپتور نازک Catalog که درز قراردادی منابع scope Access Control را از
/// <see cref="ICatalogLookupGateway"/> بیرون می‌دهد؛ Application/Domain به Access Control نشت نمی‌کند.
/// </summary>
internal sealed class CatalogAccessControlScopeResourceLookup(ICatalogLookupGateway catalog)
    : IAccessControlScopeResourceLookup
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<AccessControlScopeResourceCategory>> ListCategoriesAsync(
        string? search,
        CancellationToken cancellationToken)
    {
        var items = await catalog.ListCategoriesForAccessControlAsync(search, cancellationToken);
        return items
            .Select(i => new AccessControlScopeResourceCategory(i.CategoryId, i.ParentCategoryId, i.Name, i.Status))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AccessControlScopeResourceBrand>> ListBrandsAsync(
        string? search,
        CancellationToken cancellationToken)
    {
        var items = await catalog.ListBrandsForAccessControlAsync(search, cancellationToken);
        return items
            .Select(i => new AccessControlScopeResourceBrand(i.BrandId, i.Name, i.Status))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AccessControlScopeResourceProduct>> ListProductsAsync(
        string? search,
        CancellationToken cancellationToken)
    {
        var items = await catalog.ListProductsForAccessControlAsync(search, cancellationToken);
        return items
            .Select(i => new AccessControlScopeResourceProduct(i.ProductId, i.Title, i.Status))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken) =>
        await catalog.FindCategoryAsync(categoryId, cancellationToken) is not null;

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, string>> GetCategoryNamesAsync(
        IReadOnlyCollection<Guid> categoryIds,
        CancellationToken cancellationToken) =>
        await catalog.GetCategoryNamesAsync(categoryIds, cancellationToken);
}
