using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Ports;

/// <summary>
/// درز خواندن Catalog برای ماژول‌های دیگر. Search منبع حقیقت نمی‌شود.
/// </summary>
public interface ICatalogLookupGateway
{
    /// <summary>
    /// محصول را در پایگاه Tenant/Marketplace جاری پیدا می‌کند؛ Host parse نمی‌شود.
    /// </summary>
    Task<ProductReference?> FindProductAsync(Guid productId, CancellationToken cancellationToken);

    /// <summary>
    /// گونه را پیدا می‌کند.
    /// </summary>
    Task<VariantReference?> FindVariantAsync(Guid variantId, CancellationToken cancellationToken);

    /// <summary>رده را برای اعتبارسنجی scope پیدا می‌کند.</summary>
    Task<CategoryReference?> FindCategoryAsync(Guid categoryId, CancellationToken cancellationToken);

    /// <summary>محصول را با slug پایدار همراه شناسهٔ گونه‌ها برای اثبات خرید پیدا می‌کند.</summary>
    Task<ReviewableProductReference?> FindReviewableProductBySlugAsync(string slug, CancellationToken cancellationToken);

    /// <summary>محصول قابل بررسی را با شناسهٔ Catalog پیدا می‌کند.</summary>
    Task<ReviewableProductReference?> FindReviewableProductByIdAsync(Guid productId, CancellationToken cancellationToken);

    /// <summary>عنوان امن و محلی محصولات را برای ترکیب صف مدیریت به‌صورت گروهی می‌خواند.</summary>
    Task<IReadOnlyDictionary<Guid, string>> GetProductTitlesAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken);

    /// <summary>نام محلی رده‌ها را گروهی می‌خواند (اولویت fa سپس en).</summary>
    Task<IReadOnlyDictionary<Guid, string>> GetCategoryNamesAsync(
        IReadOnlyCollection<Guid> categoryIds,
        CancellationToken cancellationToken);

    /// <summary>مرجع قابل‌نمایش محصولات منتشرشده را برای ترکیب نظرات خانه به‌صورت گروهی می‌خواند.</summary>
    Task<IReadOnlyDictionary<Guid, ReviewableProductReference>> GetReviewableProductsByIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// ردهٔ اصلی هر گونه را از product→اولین ProductCategories.CategoryId به‌صورت دسته‌ای برمی‌گرداند (بدون N+1).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, Guid?>> GetPrimaryCategoryIdsByVariantIdsAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken);

    /// <summary>فهرست رده‌ها برای انتخابگر Access Control با جستجوی نام.</summary>
    Task<IReadOnlyList<AccessControlCategoryItem>> ListCategoriesForAccessControlAsync(
        string? search,
        CancellationToken cancellationToken);

    /// <summary>فهرست برندها برای انتخابگر Access Control.</summary>
    Task<IReadOnlyList<AccessControlBrandItem>> ListBrandsForAccessControlAsync(
        string? search,
        CancellationToken cancellationToken);

    /// <summary>فهرست محصولات منتشرشده برای انتخابگر Access Control.</summary>
    Task<IReadOnlyList<AccessControlProductItem>> ListProductsForAccessControlAsync(
        string? search,
        CancellationToken cancellationToken);

    /// <summary>
    /// سیاست مؤثر مقدار را از Product + واحد + تنظیم سراسری می‌خواند؛ N+1 ندارد.
    /// </summary>
    Task<EffectiveQuantityPolicy?> GetEffectiveQuantityPolicyForVariantAsync(
        Guid variantId,
        CancellationToken cancellationToken);

    /// <summary>سیاست مؤثر چند گونه را یکجا می‌خواند.</summary>
    Task<IReadOnlyDictionary<Guid, EffectiveQuantityPolicy>> GetEffectiveQuantityPoliciesForVariantIdsAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken);

    /// <summary>یک GlobalRoundingMode فروشگاه.</summary>
    Task<QuantityRoundingMode> GetGlobalRoundingModeAsync(CancellationToken cancellationToken);
}
