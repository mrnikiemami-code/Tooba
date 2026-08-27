using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application;

/// <summary>
/// مرجع پایدار محصول توصیفی برای ماژول‌های بعدی بدون نشت EF. قیمت ندارد.
/// </summary>
public sealed record ProductReference(Guid ProductId, CatalogProductKind Kind, CatalogPublicationStatus Status);

/// <summary>مرجع محصول برای Reviews شامل گونه‌های Catalog و بدون نشت موجودیت EF.</summary>
public sealed record ReviewableProductReference(
    Guid ProductId,
    string Slug,
    string Title,
    CatalogPublicationStatus Status,
    IReadOnlyList<Guid> VariantIds);

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
/// ردهٔ Catalog برای انتخابگر scope در Access Control (نام محلی؛ نه JOIN از Order).
/// </summary>
/// <param name="CategoryId">شناسهٔ رده.</param>
/// <param name="ParentCategoryId">والد اختیاری.</param>
/// <param name="Name">نام محلی (اولویت fa سپس en).</param>
/// <param name="Status">وضعیت انتشار.</param>
public sealed record AccessControlCategoryItem(
    Guid CategoryId,
    Guid? ParentCategoryId,
    string Name,
    string Status);

/// <summary>
/// برند برای انتخابگر scope Access Control.
/// </summary>
/// <param name="BrandId">شناسه.</param>
/// <param name="Name">نام محلی.</param>
/// <param name="Status">وضعیت.</param>
public sealed record AccessControlBrandItem(Guid BrandId, string Name, string Status);

/// <summary>
/// محصول منتشرشده برای انتخابگر scope Access Control.
/// </summary>
/// <param name="ProductId">شناسه.</param>
/// <param name="Title">عنوان محلی.</param>
/// <param name="Status">وضعیت.</param>
public sealed record AccessControlProductItem(Guid ProductId, string Title, string Status);

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
    /// فرادادهٔ تعریف ویژگی را به‌روز می‌کند (نه Code/ValueKind/IsVariantAxis).
    /// </summary>
    Task UpdateAttributeDefinitionAsync(
        Guid definitionId,
        string? unit,
        bool isRequired,
        bool isFilterable,
        bool isComparable,
        bool isMultivalue,
        int displayOrder,
        decimal? validationMin,
        decimal? validationMax,
        int? validationMaxLength,
        bool isActive,
        CancellationToken cancellationToken);

    /// <summary>
    /// فهرست تعاریف ویژگی.
    /// </summary>
    Task<IReadOnlyList<AttributeDefinitionView>> ListAttributeDefinitionsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// یک تعریف ویژگی را برمی‌گرداند.
    /// </summary>
    Task<AttributeDefinitionView?> GetAttributeDefinitionAsync(Guid definitionId, CancellationToken cancellationToken);

    /// <summary>
    /// گزینهٔ شمارشی اضافه می‌کند.
    /// </summary>
    Task<Guid> AddAttributeOptionAsync(Guid definitionId, string code, IReadOnlyDictionary<string, string> localizedNames, CancellationToken cancellationToken);

    /// <summary>
    /// تعریف را به schema رده پیوند می‌دهد.
    /// </summary>
    Task BindCategoryAttributeAsync(
        Guid categoryId,
        Guid definitionId,
        int displayOrder,
        bool? isRequiredOverride,
        CancellationToken cancellationToken);

    /// <summary>
    /// پیوند schema رده را برمی‌دارد؛ مقادیر محصول را حذف نمی‌کند.
    /// </summary>
    Task UnbindCategoryAttributeAsync(Guid categoryId, Guid definitionId, CancellationToken cancellationToken);

    /// <summary>
    /// ترتیب پیوندهای schema رده را بازنویسی می‌کند.
    /// </summary>
    Task ReorderCategoryAttributeBindingsAsync(
        Guid categoryId,
        IReadOnlyList<Guid> orderedDefinitionIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// schema مؤثر رده (با ارث والدین) را برمی‌گرداند.
    /// </summary>
    Task<IReadOnlyList<EffectiveSchemaEntry>> GetEffectiveCategorySchemaAsync(
        Guid categoryId,
        CancellationToken cancellationToken);

    /// <summary>
    /// محصول توصیفی می‌سازد.
    /// </summary>
    Task<ProductReference> CreateProductAsync(CatalogProductKind kind, string? slugSeam, Guid? brandId, IReadOnlyDictionary<string, string> localizedNames, CancellationToken cancellationToken);

    /// <summary>
    /// متن محلی یک فیلد مجاز محصول را درج یا به‌روزرسانی می‌کند؛ این درز فقط محتوای Catalog را می‌نویسد و قیمت یا موجودی نمی‌پذیرد.
    /// </summary>
    /// <param name="productId">شناسهٔ محصول در Catalog جاری.</param>
    /// <param name="fieldKey">کلید محتوایی مجاز؛ در حال حاضر short_description و full_description.</param>
    /// <param name="localizedValues">مقادیر غیرخالی بر اساس locale استاندارد.</param>
    /// <param name="cancellationToken">توکن لغو عملیات.</param>
    Task UpsertProductLocalizedFieldAsync(
        Guid productId,
        string fieldKey,
        IReadOnlyDictionary<string, string> localizedValues,
        CancellationToken cancellationToken);

    /// <summary>
    /// محصول را به رده وصل می‌کند.
    /// </summary>
    Task AssignCategoryAsync(Guid productId, Guid categoryId, CancellationToken cancellationToken);

    /// <summary>
    /// مرجع مات رسانه می‌گذارد.
    /// </summary>
    Task AttachMediaReferenceAsync(Guid productId, Guid mediaAssetId, CancellationToken cancellationToken);

    /// <summary>
    /// مشخصهٔ غیرمحور روی محصول می‌گذارد (upsert). JSON آزاد نیست.
    /// </summary>
    Task SetProductAttributeAsync(Guid productId, Guid definitionId, string rawValue, Guid? enumOptionId, CancellationToken cancellationToken);

    /// <summary>
    /// محورهای Variant انتخاب‌شدهٔ محصول را جایگزین می‌کند؛ ماتریس کامل تولید نمی‌شود.
    /// </summary>
    Task SetProductVariantAxesAsync(
        Guid productId,
        IReadOnlyList<Guid> orderedDefinitionIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// تأثیر تغییر رده را بدون حذف مقادیر گزارش می‌کند.
    /// </summary>
    Task<CategoryChangeImpact> PreviewCategoryChangeAsync(
        Guid productId,
        Guid newCategoryId,
        CancellationToken cancellationToken);

    /// <summary>
    /// ردهٔ اصلی محصول را عوض می‌کند پس از گزارش تأثیر؛ orphanها را حذف خاموش نمی‌کند.
    /// </summary>
    Task<CategoryChangeImpact> ReplaceProductPrimaryCategoryAsync(
        Guid productId,
        Guid newCategoryId,
        CancellationToken cancellationToken);

    /// <summary>
    /// الزام‌های schema مؤثر را برای مقادیر فعلی محصول بررسی می‌کند.
    /// </summary>
    Task ValidateProductAttributesAsync(Guid productId, CancellationToken cancellationToken);

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

/// <summary>
/// نمای تعریف ویژگی برای schema authoring بدون نشت EF.
/// </summary>
public sealed record AttributeDefinitionView(
    Guid DefinitionId,
    string Code,
    CatalogAttributeValueKind ValueKind,
    bool IsVariantAxisAllowed,
    string? Unit,
    bool IsRequired,
    bool IsFilterable,
    bool IsComparable,
    bool IsMultivalue,
    int DisplayOrder,
    decimal? ValidationMin,
    decimal? ValidationMax,
    int? ValidationMaxLength,
    bool IsActive,
    DateTimeOffset CreatedAt);

/// <summary>
/// ردیف schema مؤثر رده پس از ارث والدین.
/// </summary>
public sealed record EffectiveSchemaEntry(
    Guid DefinitionId,
    string Code,
    CatalogAttributeValueKind ValueKind,
    bool IsVariantAxisAllowed,
    string? Unit,
    bool IsRequired,
    bool IsFilterable,
    bool IsComparable,
    bool IsMultivalue,
    int DisplayOrder,
    Guid InheritedFromCategoryId,
    bool DefinitionIsActive);

/// <summary>
/// گزارش تأثیر تغییر رده؛ حذف خاموش انجام نمی‌شود.
/// </summary>
public sealed record CategoryChangeImpact(
    Guid ProductId,
    Guid NewCategoryId,
    IReadOnlyList<OrphanProductAttributeValue> OrphanAttributeValues,
    IReadOnlyList<Guid> InvalidVariantAxisDefinitionIds);

/// <summary>
/// مقدار ویژگی محصول که در schema جدید جایی ندارد.
/// </summary>
public sealed record OrphanProductAttributeValue(Guid DefinitionId, string CanonicalValue);
