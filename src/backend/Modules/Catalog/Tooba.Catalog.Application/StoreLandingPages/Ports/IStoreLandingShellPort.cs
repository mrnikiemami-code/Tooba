using Tooba.Catalog.Application.StoreLandingPages.Models;

namespace Tooba.Catalog.Application.StoreLandingPages.Ports;

/// <summary>
/// درز ویترین برای shell دادهٔ Landing (کارت محصول / رده / برند / مقاله / نظر).
/// پیاده‌سازی Host روی StorefrontComposer؛ Catalog به Host/Content وابسته نمی‌شود.
/// </summary>
public interface IStoreLandingShellPort
{
    /// <summary>کارت‌های محصول برای شناسه‌های بخش.</summary>
    Task<IReadOnlyDictionary<Guid, StoreLandingShellProductCard>> ComposeProductCardsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken);

    /// <summary>رده‌های منتشرشده.</summary>
    Task<IReadOnlyList<StoreLandingShellCategoryItem>> ListCategoriesAsync(CancellationToken cancellationToken);

    /// <summary>برندهای منتشرشده.</summary>
    Task<IReadOnlyList<StoreLandingShellBrandItem>> ListBrandsAsync(CancellationToken cancellationToken);

    /// <summary>آخرین مقالات برای لوکیل صفحهٔ Landing.</summary>
    Task<IReadOnlyList<StoreLandingShellArticleItem>> BuildLatestArticlesAsync(
        string pageLocale,
        CancellationToken cancellationToken);

    /// <summary>نظرهای برجستهٔ خانه.</summary>
    Task<IReadOnlyList<StoreLandingShellFeaturedReviewItem>> BuildFeaturedReviewsAsync(
        CancellationToken cancellationToken);
}
