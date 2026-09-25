namespace Tooba.Catalog.Contracts;

/// <summary>
/// دستهٔ انتخابگر scope Access Control — قرارداد بی‌طرف Catalog.
/// </summary>
/// <param name="CategoryId">شناسهٔ دسته.</param>
/// <param name="ParentCategoryId">شناسهٔ دستهٔ والد در صورت وجود.</param>
/// <param name="Name">نام محلی.</param>
/// <param name="Status">وضعیت.</param>
public sealed record AccessControlScopeResourceCategory(
    Guid CategoryId,
    Guid? ParentCategoryId,
    string Name,
    string Status);

/// <summary>
/// برند انتخابگر scope Access Control — قرارداد بی‌طرف Catalog.
/// </summary>
/// <param name="BrandId">شناسهٔ برند.</param>
/// <param name="Name">نام.</param>
/// <param name="Status">وضعیت.</param>
public sealed record AccessControlScopeResourceBrand(Guid BrandId, string Name, string Status);

/// <summary>
/// محصول انتخابگر scope Access Control — قرارداد بی‌طرف Catalog.
/// </summary>
/// <param name="ProductId">شناسهٔ محصول.</param>
/// <param name="Title">عنوان محلی.</param>
/// <param name="Status">وضعیت.</param>
public sealed record AccessControlScopeResourceProduct(Guid ProductId, string Title, string Status);

/// <summary>
/// درز قراردادی Catalog برای منابع scope Access Control.
/// مالکیت پیاده‌سازی با Catalog است؛ Access Control به Application/Domain آن وابسته نمی‌شود.
/// </summary>
public interface IAccessControlScopeResourceLookup
{
    /// <summary>دسته‌های قابل انتخاب را برمی‌گرداند.</summary>
    /// <param name="search">عبارت جستجو در صورت وجود.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<IReadOnlyList<AccessControlScopeResourceCategory>> ListCategoriesAsync(
        string? search,
        CancellationToken cancellationToken);

    /// <summary>برندهای قابل انتخاب را برمی‌گرداند.</summary>
    /// <param name="search">عبارت جستجو در صورت وجود.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<IReadOnlyList<AccessControlScopeResourceBrand>> ListBrandsAsync(
        string? search,
        CancellationToken cancellationToken);

    /// <summary>محصولات قابل انتخاب را برمی‌گرداند.</summary>
    /// <param name="search">عبارت جستجو در صورت وجود.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<IReadOnlyList<AccessControlScopeResourceProduct>> ListProductsAsync(
        string? search,
        CancellationToken cancellationToken);

    /// <summary>آیا ردهٔ موردنظر وجود دارد.</summary>
    /// <param name="categoryId">شناسهٔ رده.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken);

    /// <summary>نام محلی رده‌ها به تفکیک شناسه.</summary>
    /// <param name="categoryIds">شناسهٔ رده‌ها.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<IReadOnlyDictionary<Guid, string>> GetCategoryNamesAsync(
        IReadOnlyCollection<Guid> categoryIds,
        CancellationToken cancellationToken);
}
