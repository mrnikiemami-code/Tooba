using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application;

/// <summary>
/// مرجع پایدار محصول توصیفی برای ماژول‌های بعدی بدون نشت EF. قیمت ندارد.
/// </summary>
public sealed record ProductReference(Guid ProductId, CatalogProductKind Kind, CatalogPublicationStatus Status);

/// <summary>
/// مرجع گونهٔ Catalog. هویت Offer فروشنده نیست.
/// </summary>
public sealed record VariantReference(Guid VariantId, Guid ProductId, string CombinationFingerprint, CatalogPublicationStatus Status);

/// <summary>
/// مرجع ردهٔ طبقه‌بندی.
/// </summary>
public sealed record CategoryReference(Guid CategoryId, Guid? ParentCategoryId, CatalogPublicationStatus Status);

/// <summary>
/// مرجع برند تحریری.
/// </summary>
public sealed record BrandReference(Guid BrandId, string? SlugSeam, CatalogPublicationStatus Status);

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
}

/// <summary>
/// درز نگهبان مجوز موردکاربرد. ماتریس نهایی Catalog و SDK اسپایس‌دی‌بی اینجا نیست.
/// </summary>
public interface ICatalogUseCaseGuard
{
    /// <summary>
    /// اجازهٔ نوشتن foundation را بررسی می‌کند. پیاده‌سازی فعلی فقط درز است.
    /// </summary>
    Task EnsureCanMutateAsync(CancellationToken cancellationToken);
}

/// <summary>
/// نوشتن foundation Catalog. UI تجاری و Offer اینجا نیست.
/// </summary>
public interface ICatalogDirectory
{
    /// <summary>
    /// رده می‌سازد.
    /// </summary>
    Task<CategoryReference> CreateCategoryAsync(Guid? parentCategoryId, IReadOnlyDictionary<string, string> localizedNames, CancellationToken cancellationToken);

    /// <summary>
    /// برند تحریری می‌سازد.
    /// </summary>
    Task<BrandReference> CreateBrandAsync(string? slugSeam, IReadOnlyDictionary<string, string> localizedNames, CancellationToken cancellationToken);

    /// <summary>
    /// تعریف ویژگی تایپ‌شده می‌سازد.
    /// </summary>
    Task<Guid> CreateAttributeDefinitionAsync(string code, CatalogAttributeValueKind valueKind, bool isVariantAxis, IReadOnlyDictionary<string, string> localizedNames, CancellationToken cancellationToken);

    /// <summary>
    /// گزینهٔ شمارشی اضافه می‌کند.
    /// </summary>
    Task<Guid> AddAttributeOptionAsync(Guid definitionId, string code, IReadOnlyDictionary<string, string> localizedNames, CancellationToken cancellationToken);

    /// <summary>
    /// محصول توصیفی می‌سازد.
    /// </summary>
    Task<ProductReference> CreateProductAsync(CatalogProductKind kind, string? slugSeam, Guid? brandId, IReadOnlyDictionary<string, string> localizedNames, CancellationToken cancellationToken);

    /// <summary>
    /// محصول را به رده وصل می‌کند.
    /// </summary>
    Task AssignCategoryAsync(Guid productId, Guid categoryId, CancellationToken cancellationToken);

    /// <summary>
    /// مرجع مات رسانه می‌گذارد.
    /// </summary>
    Task AttachMediaReferenceAsync(Guid productId, Guid mediaAssetId, CancellationToken cancellationToken);

    /// <summary>
    /// مشخصهٔ غیرمحور روی محصول می‌گذارد.
    /// </summary>
    Task SetProductAttributeAsync(Guid productId, Guid definitionId, string rawValue, Guid? enumOptionId, CancellationToken cancellationToken);

    /// <summary>
    /// محصول را در Catalog منتشر می‌کند نه در Offer.
    /// </summary>
    Task PublishProductAsync(Guid productId, CancellationToken cancellationToken);

    /// <summary>
    /// رده را برای ناوبری منتشر می‌کند. رده منتشرنشده در سطوح عمومی دیده نمی‌شود،
    /// اما انتشار رده هیچ قابلیت خریدی نمی‌سازد؛ قیمت و موجودی بیرون از Catalog می‌مانند.
    /// </summary>
    /// <param name="categoryId">شناسهٔ ردهٔ موجود در Catalog همین Tenant.</param>
    /// <param name="cancellationToken">توکن لغو عملیات.</param>
    /// <exception cref="InvalidOperationException">اگر رده در Catalog این Tenant نباشد.</exception>
    Task PublishCategoryAsync(Guid categoryId, CancellationToken cancellationToken);

    /// <summary>
    /// برند را برای سطوح عمومی برند منتشر می‌کند. انتشار برند تحریری است و
    /// مالکیت فروشنده، کمیسیون یا ادعای بازاریابی تولید نمی‌کند.
    /// </summary>
    /// <param name="brandId">شناسهٔ برند موجود در Catalog همین Tenant.</param>
    /// <param name="cancellationToken">توکن لغو عملیات.</param>
    /// <exception cref="InvalidOperationException">اگر برند در Catalog این Tenant نباشد.</exception>
    Task PublishBrandAsync(Guid brandId, CancellationToken cancellationToken);

    /// <summary>
    /// گونه با ترکیب یکتا می‌سازد.
    /// </summary>
    Task<VariantReference> CreateVariantAsync(
        Guid productId,
        string? catalogCodeSeam,
        IReadOnlyList<(Guid DefinitionId, string RawValue, Guid? EnumOptionId)> axes,
        CancellationToken cancellationToken);
}
