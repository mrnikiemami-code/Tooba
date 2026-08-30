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
    /// رده می‌سازد (نام‌ها + auto-slug در ترجمه؛ LocalizedText برای سازگاری عقب‌رو).
    /// </summary>
    Task<CategoryReference> CreateCategoryAsync(Guid? parentCategoryId, IReadOnlyDictionary<string, string> localizedNames, CancellationToken cancellationToken);

    /// <summary>
    /// رده با فیلدهای هسته و ترجمه‌های صریح می‌سازد.
    /// </summary>
    Task<CategoryReference> CreateCategoryAsync(CategoryCreateRequest request, CancellationToken cancellationToken);

    /// <summary>هستهٔ غیرمحلی رده را به‌روز می‌کند؛ Parent فقط از Move.</summary>
    Task UpdateCategoryCoreAsync(Guid categoryId, CategoryCoreUpdateRequest request, CancellationToken cancellationToken);

    /// <summary>ترجمهٔ locale را درج/به‌روز می‌کند؛ تغییر slug تاریخچه می‌سازد.</summary>
    Task<CategoryTranslationDto> UpsertCategoryTranslationAsync(
        Guid categoryId,
        CategoryTranslationUpsertRequest request,
        CancellationToken cancellationToken);

    /// <summary>رده را زیر والد جدید جابه‌جا می‌کند با جلوگیری از حلقه.</summary>
    Task MoveCategoryAsync(Guid categoryId, Guid? newParentId, DateTimeOffset? expectedUpdatedAt, CancellationToken cancellationToken);

    /// <summary>ترتیب خواهر/برادرها را بازنویسی می‌کند.</summary>
    Task ReorderCategorySiblingsAsync(Guid? parentId, IReadOnlyList<Guid> orderedCategoryIds, CancellationToken cancellationToken);

    /// <summary>درخت رده را یک‌باره برای locale می‌خواند (بدون N+1).</summary>
    Task<IReadOnlyList<CategoryTreeNodeDto>> GetCategoryTreeAsync(string locale, string? search, CancellationToken cancellationToken);

    /// <summary>خلاصهٔ workspace رده برای Admin.</summary>
    Task<CategoryWorkspaceSummaryDto?> GetCategoryWorkspaceAsync(Guid categoryId, string? locale, CancellationToken cancellationToken);

    /// <summary>مسیر locale+slug را به رده جاری یا redirect تاریخی resolve می‌کند.</summary>
    Task<CategoryRouteResolveResult?> ResolveCategoryRouteAsync(
        string locale,
        string slug,
        bool forStorefront,
        CancellationToken cancellationToken);

    /// <summary>رده را آرشیو می‌کند.</summary>
    Task ArchiveCategoryAsync(Guid categoryId, CancellationToken cancellationToken);

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
        CategoryAttributeAssignmentFlags flags,
        CancellationToken cancellationToken);

    /// <summary>
    /// رفتار assignment محلی رده را به‌روزرسانی می‌کند.
    /// </summary>
    Task UpdateCategoryAttributeBindingAsync(
        Guid categoryId,
        Guid definitionId,
        CategoryAttributeAssignmentFlags flags,
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
    /// facetهای مؤثر رده برای PLP را برمی‌گرداند.
    /// </summary>
    Task<IReadOnlyList<EffectiveCategoryFacet>> GetEffectiveCategoryFacetsAsync(
        Guid categoryId,
        string locale,
        CancellationToken cancellationToken);

    /// <summary>
    /// پیکربندی‌های facet محلی رده را برمی‌گرداند.
    /// </summary>
    Task<IReadOnlyList<CategoryFacetConfigurationView>> ListLocalFacetConfigurationsAsync(
        Guid categoryId,
        CancellationToken cancellationToken);

    /// <summary>
    /// پیکربندی facet رده را درج یا به‌روزرسانی می‌کند.
    /// </summary>
    Task UpsertCategoryFacetConfigurationAsync(
        Guid categoryId,
        Guid definitionId,
        CategoryFacetConfigurationInput input,
        CancellationToken cancellationToken);

    /// <summary>
    /// override محلی facet را حذف می‌کند (بازگشت به والد).
    /// </summary>
    Task RemoveCategoryFacetOverrideAsync(
        Guid categoryId,
        Guid definitionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// ترتیب facetهای محلی رده را بازنویسی می‌کند.
    /// </summary>
    Task ReorderCategoryFacetConfigurationsAsync(
        Guid categoryId,
        IReadOnlyList<Guid> orderedDefinitionIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// پیکربندی مگامنو برای یک رده را برمی‌گرداند.
    /// </summary>
    Task<CategoryMegaMenuConfigurationView> GetCategoryMegaMenuConfigurationAsync(
        Guid categoryId,
        string locale,
        CancellationToken cancellationToken);

    /// <summary>
    /// رده را به مگامنو متصل می‌کند یا به‌روزرسانی می‌کند.
    /// </summary>
    Task UpsertCategoryMegaMenuBindingAsync(
        Guid categoryId,
        string locale,
        CategoryMegaMenuBindingInput input,
        CancellationToken cancellationToken);

    /// <summary>
    /// اتصال رده را از مگامنو برمی‌دارد (رده حذف نمی‌شود).
    /// </summary>
    Task RemoveCategoryMegaMenuBindingAsync(Guid categoryId, CancellationToken cancellationToken);

    /// <summary>
    /// گزینه‌های والد presentation برای selector Admin.
    /// </summary>
    Task<IReadOnlyList<MegaMenuPlacementOption>> ListMegaMenuPlacementOptionsAsync(
        Guid categoryId,
        string locale,
        CancellationToken cancellationToken);

    /// <summary>
    /// درخت مگامنو قابل نمایش ویترین را برمی‌گرداند.
    /// </summary>
    Task<IReadOnlyList<StorefrontMegaMenuItem>> GetStorefrontMegaMenuAsync(
        string locale,
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
    /// مرجع مات رسانه با alt و ترتیب اولیه می‌گذارد.
    /// </summary>
    Task AttachMediaReferenceAsync(
        Guid productId,
        Guid mediaAssetId,
        string? altText,
        CancellationToken cancellationToken);

    /// <summary>
    /// شناسهٔ مات نمایشی می‌سازد و به محصول وصل می‌کند (بدون باینری؛ کتابخانهٔ Media هنوز نیست).
    /// </summary>
    Task<Guid> AttachGeneratedPlaceholderMediaAsync(
        Guid productId,
        string? altText,
        CancellationToken cancellationToken);

    /// <summary>
    /// حالت ویرایشگر گالری رسانهٔ محصول با ترتیب و آمادگی.
    /// </summary>
    Task<ProductMediaEditorState> GetProductMediaEditorStateAsync(
        Guid productId,
        CancellationToken cancellationToken);

    /// <summary>
    /// ترتیب گالری را بازنویسی می‌کند؛ فهرست باید دقیقاً همهٔ دارایی‌های فعلی باشد.
    /// </summary>
    Task ReorderProductMediaAsync(
        Guid productId,
        IReadOnlyList<Guid> orderedMediaAssetIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// تصویر اصلی را روی یک مرجع موجود تنظیم می‌کند (یکتایی اجباری).
    /// </summary>
    Task SetProductPrimaryMediaAsync(
        Guid productId,
        Guid mediaAssetId,
        CancellationToken cancellationToken);

    /// <summary>
    /// متن جایگزین زمینه‌ای روی انتساب محصول را به‌روز می‌کند.
    /// </summary>
    Task PatchProductMediaAltAsync(
        Guid productId,
        Guid mediaAssetId,
        string? altText,
        CancellationToken cancellationToken);

    /// <summary>
    /// انتساب رسانه را از محصول جدا می‌کند؛ دارایی مشترک حذف نمی‌شود.
    /// </summary>
    Task DetachProductMediaAsync(
        Guid productId,
        Guid mediaAssetId,
        CancellationToken cancellationToken);

    /// <summary>
    /// آمادگی گالری برای انتشار بعدی (تصویر اصلی + تعداد).
    /// </summary>
    Task<ProductMediaReadiness> GetProductMediaReadinessAsync(
        Guid productId,
        CancellationToken cancellationToken);

    /// <summary>
    /// فرادادهٔ SEO محصول برای یک locale (SlugSeam سراسری + LocalizedText).
    /// </summary>
    Task<ProductSeoDetail> GetProductSeoAsync(
        Guid productId,
        string locale,
        CancellationToken cancellationToken);

    /// <summary>
    /// به‌روزرسانی SEO محصول: SlugSeam سراسری + seo_title/seo_description محلی.
    /// </summary>
    Task<ProductSeoDetail> UpdateProductSeoAsync(
        Guid productId,
        ProductSeoUpdateInput input,
        CancellationToken cancellationToken);

    /// <summary>
    /// آمادگی SEO محصول برای یک locale (بدون Offer/Price/Stock).
    /// </summary>
    Task<ProductSeoReadiness> GetProductSeoReadinessAsync(
        Guid productId,
        string locale,
        CancellationToken cancellationToken);

    /// <summary>
    /// مشخصهٔ غیرمحور روی محصول می‌گذارد (upsert). JSON آزاد نیست.
    /// </summary>
    Task SetProductAttributeAsync(Guid productId, Guid definitionId, string rawValue, Guid? enumOptionId, CancellationToken cancellationToken);

    /// <summary>
    /// حالت ویرایشگر ویژگی‌های محصول بر اساس schema مؤثر ردهٔ اصلی.
    /// </summary>
    Task<ProductAttributeEditorState> GetProductAttributeEditorStateAsync(
        Guid productId,
        string locale,
        CancellationToken cancellationToken);

    /// <summary>
    /// مقادیر ویژگی محصول را در یک تراکنش می‌گذارد/پاک می‌کند؛ محورهای تنوع رد می‌شوند.
    /// </summary>
    Task SetProductAttributesAsync(
        Guid productId,
        IReadOnlyList<ProductAttributeValueInput> values,
        CancellationToken cancellationToken);

    /// <summary>
    /// آمادگی مقادیر الزامی schema مؤثر برای محصول.
    /// </summary>
    Task<ProductAttributeReadiness> GetProductAttributeReadinessAsync(
        Guid productId,
        CancellationToken cancellationToken);

    /// <summary>
    /// محورهای Variant انتخاب‌شدهٔ محصول را جایگزین می‌کند؛ ماتریس کامل تولید نمی‌شود.
    /// </summary>
    Task SetProductVariantAxesAsync(
        Guid productId,
        IReadOnlyList<Guid> orderedDefinitionIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// حالت ویرایشگر ماتریس تنوع‌های محصول را بر اساس schema مؤثر برمی‌گرداند.
    /// </summary>
    Task<ProductVariantEditorState> GetProductVariantEditorStateAsync(
        Guid productId,
        string locale,
        CancellationToken cancellationToken);

    /// <summary>
    /// ترکیب‌های مطلوب را پیش‌نمایش می‌کند بدون نوشتن.
    /// </summary>
    Task<ProductVariantPreviewResult> PreviewProductVariantCombinationsAsync(
        Guid productId,
        IReadOnlyList<ProductVariantSelectedAxisInput> selectedAxes,
        string locale,
        CancellationToken cancellationToken);

    /// <summary>
    /// ماتریس تنوع را reconcile می‌کند؛ حذف سخت انجام نمی‌شود (آرشیو ترجیح داده می‌شود).
    /// </summary>
    Task<ProductVariantApplyResult> ApplyProductVariantMatrixAsync(
        Guid productId,
        ProductVariantApplyInput input,
        CancellationToken cancellationToken);

    /// <summary>
    /// آمادگی تنوع‌های محصول برای جریان انتشار بعدی.
    /// </summary>
    Task<ProductVariantReadiness> GetProductVariantReadinessAsync(
        Guid productId,
        CancellationToken cancellationToken);

    /// <summary>
    /// تأثیر تغییر رده را بدون حذف مقادیر گزارش می‌کند.
    /// </summary>
    Task<CategoryChangeImpact> PreviewCategoryChangeAsync(
        Guid productId,
        Guid newCategoryId,
        CancellationToken cancellationToken);

    /// <summary>
    /// گزارش انسانی تأثیر تغییر رده با برچسب‌ها و خلاصهٔ فارسی؛ حذف خاموش ندارد.
    /// </summary>
    Task<CategoryChangeImpactReport> PreviewCategoryChangeReportAsync(
        Guid productId,
        Guid newCategoryId,
        string locale,
        CancellationToken cancellationToken);

    /// <summary>
    /// ردهٔ اصلی محصول را عوض می‌کند پس از گزارش تأثیر؛ orphanها را حذف خاموش نمی‌کند.
    /// </summary>
    Task<CategoryChangeImpact> ReplaceProductPrimaryCategoryAsync(
        Guid productId,
        Guid newCategoryId,
        CancellationToken cancellationToken);

    /// <summary>
    /// دستهٔ اضافی کشف/PLP را به محصول اضافه می‌کند؛ schema را تغییر نمی‌دهد.
    /// </summary>
    Task AddProductAdditionalCategoryAsync(
        Guid productId,
        Guid categoryId,
        CancellationToken cancellationToken);

    /// <summary>
    /// دستهٔ اضافی را حذف می‌کند؛ حذف دستهٔ اصلی مجاز نیست.
    /// </summary>
    Task RemoveProductAdditionalCategoryAsync(
        Guid productId,
        Guid categoryId,
        CancellationToken cancellationToken);

    /// <summary>
    /// فهرست پیوندهای دستهٔ محصول (اصلی + اضافی) با مسیر انسانی.
    /// </summary>
    Task<IReadOnlyList<ProductCategoryAssignmentInfo>> ListProductCategoryAssignmentsAsync(
        Guid productId,
        string locale,
        CancellationToken cancellationToken);

    /// <summary>
    /// الزام‌های schema مؤثر را برای مقادیر فعلی محصول بررسی می‌کند.
    /// </summary>
    Task ValidateProductAttributesAsync(Guid productId, CancellationToken cancellationToken);

    /// <summary>
    /// آمادگی تجمیعی انتشار محصول — فقط Catalog؛ بدون Offer/Price/Stock.
    /// </summary>
    Task<ProductPublishReadiness> GetProductPublishReadinessAsync(
        Guid productId,
        string? locale,
        CancellationToken cancellationToken);

    /// <summary>
    /// محصول را در Catalog منتشر می‌کند نه در Offer. آمادگی تجمیعی را اجباری می‌کند.
    /// </summary>
    Task PublishProductAsync(Guid productId, CancellationToken cancellationToken);

    /// <summary>
    /// محصول منتشرشده را به پیش‌نویس برمی‌گرداند. آرشیو جدا است.
    /// </summary>
    Task UnpublishProductAsync(Guid productId, CancellationToken cancellationToken);

    /// <summary>
    /// محصول را در Catalog آرشیو می‌کند؛ حذف سخت نیست.
    /// </summary>
    Task ArchiveProductAsync(Guid productId, CancellationToken cancellationToken);

    /// <summary>
    /// بازیابی صریح از بایگانی به پیش‌نویس.
    /// </summary>
    Task RestoreProductAsync(Guid productId, CancellationToken cancellationToken);

    /// <summary>
    /// فهرست تاریخچهٔ محصول (جدیدترین اول) با صفحه‌بندی قطعی.
    /// </summary>
    Task<ProductHistoryPage> ListProductHistoryAsync(
        Guid productId,
        string? section,
        int skip,
        int take,
        CancellationToken cancellationToken);

    /// <summary>
    /// ثبت صریح یک رویداد تاریخچه (برای مسیرهای Host که همین SaveChanges را ندارند).
    /// </summary>
    Task AppendProductHistoryAsync(
        Guid productId,
        string eventType,
        string section,
        string summaryFa,
        string? beforeSummary,
        string? afterSummary,
        CancellationToken cancellationToken);

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
/// پرچم‌های رفتار category-specific روی assignment.
/// </summary>
public sealed record CategoryAttributeAssignmentFlags(
    bool IsRequired,
    bool IsFilterable,
    bool IsVariantAxis,
    bool IsComparable);

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
    bool IsVariantAxis,
    string? Unit,
    bool IsRequired,
    bool IsFilterable,
    bool IsComparable,
    bool IsMultivalue,
    int DisplayOrder,
    Guid InheritedFromCategoryId,
    bool DefinitionIsActive);

/// <summary>
/// ورودی پیکربندی facet رده.
/// </summary>
public sealed record CategoryFacetConfigurationInput(
    CatalogFacetDisplayType DisplayType,
    int SortOrder,
    bool IsVisible,
    bool IsSearchable,
    bool IsCollapsedByDefault,
    bool ShowCounts);

/// <summary>
/// نمای محلی پیکربندی facet.
/// </summary>
public sealed record CategoryFacetConfigurationView(
    Guid FacetConfigurationId,
    Guid CategoryId,
    Guid DefinitionId,
    string Code,
    CatalogAttributeValueKind ValueKind,
    CatalogFacetDisplayType DisplayType,
    int SortOrder,
    bool IsVisible,
    bool IsSearchable,
    bool IsCollapsedByDefault,
    bool ShowCounts);

/// <summary>
/// facet مؤثر رده برای Admin/Storefront.
/// </summary>
public sealed record EffectiveCategoryFacet(
    Guid DefinitionId,
    string Code,
    string LocalizedName,
    CatalogAttributeValueKind ValueKind,
    CatalogFacetDisplayType DisplayType,
    int SortOrder,
    bool IsVisible,
    bool IsSearchable,
    bool IsCollapsedByDefault,
    bool ShowCounts,
    Guid SourceCategoryId,
    bool IsInherited);

/// <summary>ورودی bind/update مگامنو برای یک رده.</summary>
public sealed record CategoryMegaMenuBindingInput(
    Guid? ParentMegaMenuItemId,
    int SortOrder,
    bool IsVisible,
    bool IsFeatured,
    Guid? ImageMediaAssetId,
    Guid? IconMediaAssetId,
    string? TitleOverride,
    string? BadgeText,
    string? ShortLabel);

/// <summary>نمای Admin پیکربندی مگامنو یک رده.</summary>
public sealed record CategoryMegaMenuConfigurationView(
    Guid CategoryId,
    bool IsBound,
    Guid? MegaMenuItemId,
    Guid? ParentMegaMenuItemId,
    string? ParentMenuPath,
    int SortOrder,
    bool IsVisible,
    bool IsFeatured,
    Guid? ImageMediaAssetId,
    Guid? IconMediaAssetId,
    string DisplayTitle,
    string? TitleOverride,
    string? BadgeText,
    string? ShortLabel,
    string DestinationPreview,
    int PresentationLevel,
    bool CategoryPublished,
    bool CategoryVisible);

/// <summary>گزینهٔ والد presentation با مسیر انسانی.</summary>
public sealed record MegaMenuPlacementOption(
    Guid MegaMenuItemId,
    Guid CategoryId,
    string Label,
    string MenuPath,
    int Level);

/// <summary>آیتم flat مگامنو برای Storefront.</summary>
public sealed record StorefrontMegaMenuItem(
    Guid MegaMenuItemId,
    Guid? ParentMegaMenuItemId,
    Guid CategoryId,
    string Title,
    string Destination,
    bool IsFeatured,
    Guid? IconMediaAssetId,
    Guid? ImageMediaAssetId,
    int SortOrder);

/// <summary>
/// گزارش تأثیر تغییر رده؛ حذف خاموش انجام نمی‌شود.
/// </summary>
public sealed record CategoryChangeImpact(
    Guid ProductId,
    Guid NewCategoryId,
    IReadOnlyList<OrphanProductAttributeValue> OrphanAttributeValues,
    IReadOnlyList<Guid> InvalidVariantAxisDefinitionIds);

/// <summary>نقش پیوند محصول↔رده در لایهٔ Application.</summary>
public enum ProductCategoryAssignmentRole
{
    /// <summary>دسته اصلی.</summary>
    Primary = 0,

    /// <summary>دسته اضافی.</summary>
    Additional = 1,
}

/// <summary>پیوند دستهٔ محصول با مسیر انسانی.</summary>
public sealed record ProductCategoryAssignmentInfo(
    Guid CategoryId,
    string CategoryPath,
    ProductCategoryAssignmentRole Role);

/// <summary>
/// مقدار ویژگی محصول که در schema جدید جایی ندارد.
/// </summary>
public sealed record OrphanProductAttributeValue(Guid DefinitionId, string CanonicalValue);

/// <summary>
/// ورودی تنظیم/پاک‌سازی یک مقدار ویژگی محصول.
/// برای Enumeration چندمقداری: RawValue = شناسه‌های گزینه با کاما (N یا D)؛ EnumOptionId برای تک‌گزینه.
/// </summary>
public sealed record ProductAttributeValueInput(
    Guid DefinitionId,
    string? RawValue,
    Guid? EnumOptionId,
    bool Clear);

/// <summary>گزینهٔ شمارشی برای ویرایشگر ویژگی.</summary>
public sealed record ProductAttributeEditorOption(
    Guid OptionId,
    string LocalizedLabel,
    bool IsActive);

/// <summary>
/// فیلد ویرایشگر ویژگی محصول؛ محورهای تنوع ورودی ویرایش ندارند.
/// </summary>
public sealed record ProductAttributeEditorField(
    Guid DefinitionId,
    string Code,
    string LocalizedName,
    CatalogAttributeValueKind ValueKind,
    string? Unit,
    bool IsRequired,
    bool IsVariantAxis,
    bool IsFilterable,
    bool IsComparable,
    bool IsMultivalue,
    int DisplayOrder,
    IReadOnlyList<ProductAttributeEditorOption> Options,
    string? CurrentCanonicalValue,
    Guid? CurrentEnumOptionId,
    string? DisplayValue,
    bool IsMissingRequired);

/// <summary>حالت کامل ویرایشگر ویژگی محصول.</summary>
public sealed record ProductAttributeEditorState(
    Guid ProductId,
    Guid? CategoryId,
    string? CategoryPath,
    IReadOnlyList<ProductAttributeEditorField> Fields,
    ProductAttributeReadiness Readiness);

/// <summary>آمادگی مقادیر ویژگی برای انتشار بعدی.</summary>
public sealed record ProductAttributeReadiness(
    bool IsComplete,
    IReadOnlyList<string> MissingRequiredCodes,
    IReadOnlyList<string> InvalidValues);

/// <summary>انتساب مات رسانه روی محصول؛ باینری ذخیره نمی‌شود.</summary>
public sealed record ProductMediaAssignment(
    Guid MediaAssetId,
    bool IsPrimary,
    int DisplayOrder,
    string? AltText);

/// <summary>حالت ویرایشگر گالری رسانهٔ محصول.</summary>
public sealed record ProductMediaEditorState(
    Guid ProductId,
    IReadOnlyList<ProductMediaAssignment> Items,
    ProductMediaReadiness Readiness);

/// <summary>آمادگی گالری رسانه برای انتشار بعدی (T016).</summary>
public sealed record ProductMediaReadiness(
    bool HasPrimaryImage,
    int MediaCount,
    bool IsReady,
    string? MessageFa);

/// <summary>ورودی به‌روزرسانی SEO محصول (locale + قفل خوش‌بینانه).</summary>
public sealed record ProductSeoUpdateInput(
    string Locale,
    string? Slug,
    string? SeoTitle,
    string? SeoDescription,
    DateTimeOffset ExpectedUpdatedAt);

/// <summary>آمادگی SEO محصول — جدا از Media و بدون وابستگی تجاری.</summary>
public sealed record ProductSeoReadiness(
    bool HasValidSlug,
    bool HasSeoTitleOrFallback,
    bool HasSeoDescription,
    bool HasLocalizedIdentity,
    bool IsReady,
    string? MessageFa);

/// <summary>مورد ناقص آمادگی انتشار با هدایت به تب Workspace.</summary>
public sealed record ProductPublishMissingRequirement(
    string Code,
    string MessageFa,
    string WorkspaceTab);

/// <summary>
/// آمادگی تجمیعی انتشار Product Master — فقط Catalog.
/// Offer / Pricing / Inventory عمداً خارج‌اند.
/// </summary>
public sealed record ProductPublishReadiness(
    bool IsReady,
    bool CategoryReady,
    bool TranslationReady,
    bool AttributeReady,
    bool VariantReady,
    bool MediaReady,
    bool SeoReady,
    IReadOnlyList<ProductPublishMissingRequirement> MissingRequirements,
    string MessageFa);

/// <summary>یک ردیف تاریخچهٔ انسانی محصول برای Admin.</summary>
public sealed record ProductHistoryEntryDto(
    Guid HistoryId,
    Guid ProductId,
    string EventType,
    string Section,
    string SectionLabelFa,
    string SummaryFa,
    string? BeforeSummary,
    string? AfterSummary,
    Guid? ActorUserId,
    string ActorDisplayName,
    DateTimeOffset OccurredAt);

/// <summary>صفحهٔ تاریخچهٔ محصول.</summary>
public sealed record ProductHistoryPage(
    IReadOnlyList<ProductHistoryEntryDto> Items,
    int TotalCount,
    int Skip,
    int Take);

/// <summary>جزئیات SEO محصول برای Admin و پیش‌نمایش مسیر عمومی.</summary>
public sealed record ProductSeoDetail(
    Guid ProductId,
    string Locale,
    string? Slug,
    string? SeoTitle,
    string? SeoDescription,
    string? ProductName,
    string? TitleFallback,
    string PublicPath,
    ProductSeoReadiness Readiness,
    DateTimeOffset UpdatedAt);

/// <summary>خلاصهٔ یک مقدار یتیم پس از تغییر رده.</summary>
public sealed record CategoryChangeOrphanSummary(
    Guid DefinitionId,
    string LocalizedName,
    string DisplayValue);

/// <summary>
/// گزارش انسانی تأثیر تغییر رده برای تأیید Admin.
/// </summary>
public sealed record CategoryChangeImpactReport(
    Guid ProductId,
    Guid NewCategoryId,
    int CompatiblePreservedCount,
    int OrphanCount,
    int NewlyRequiredMissingCount,
    IReadOnlyList<CategoryChangeOrphanSummary> OrphanSummaries,
    IReadOnlyList<string> NewlyRequiredLabels,
    IReadOnlyList<Guid> InvalidVariantAxisDefinitionIds,
    string MessageFa,
    int ImpactedVariantCount = 0,
    string? VariantImpactMessageFa = null);

/// <summary>گزینهٔ محور تنوع در ویرایشگر.</summary>
public sealed record ProductVariantAxisOption(
    Guid OptionId,
    string LocalizedLabel,
    string Code,
    bool IsActive);

/// <summary>فیلد محور تنوع برای انتخاب مقادیر محصول.</summary>
public sealed record ProductVariantAxisEditorField(
    Guid DefinitionId,
    string Code,
    string LocalizedName,
    CatalogAttributeValueKind ValueKind,
    IReadOnlyList<ProductVariantAxisOption> Options,
    IReadOnlyList<Guid> SelectedOptionIds);

/// <summary>برچسب خوانا یک مقدار محور.</summary>
public sealed record ProductVariantAxisLabel(string DefinitionName, string ValueLabel);

/// <summary>ردیف فهرست تنوع‌ها؛ OfferCount اختیاری توسط Host پر می‌شود.</summary>
public sealed record ProductVariantListItem(
    Guid VariantId,
    string Fingerprint,
    CatalogPublicationStatus Status,
    int SortOrder,
    bool IsDefault,
    string? CatalogCodeSeam,
    IReadOnlyList<ProductVariantAxisLabel> AxisLabels,
    int? OfferCount);

/// <summary>حالت کامل ویرایشگر ماتریس تنوع.</summary>
public sealed record ProductVariantEditorState(
    Guid ProductId,
    string? CategoryPath,
    IReadOnlyList<ProductVariantAxisEditorField> Axes,
    IReadOnlyList<ProductVariantListItem> Variants,
    ProductVariantReadiness Readiness,
    int MaxCombinations,
    string? MessageFa);

/// <summary>عمل پیش‌نمایش برای یک ترکیب.</summary>
public enum ProductVariantCombinationAction
{
    /// <summary>ترکیب موجود بدون تغییر.</summary>
    Unchanged = 0,

    /// <summary>ترکیب جدید باید ساخته شود.</summary>
    New = 1,

    /// <summary>ترکیب دیگر انتخاب نشده و باید غیرفعال شود.</summary>
    Deactivate = 2,
}

/// <summary>یک ترکیب در پیش‌نمایش ماتریس.</summary>
public sealed record ProductVariantCombinationPreview(
    string DesiredFingerprint,
    IReadOnlyList<ProductVariantAxisLabel> AxisLabels,
    Guid? ExistingVariantId,
    ProductVariantCombinationAction Action,
    bool? ReferencedByOffers);

/// <summary>نتیجهٔ پیش‌نمایش ماتریس تنوع.</summary>
public sealed record ProductVariantPreviewResult(
    IReadOnlyList<ProductVariantCombinationPreview> Combinations,
    int UnchangedCount,
    int NewCount,
    int DeactivateCount,
    int TotalDesired,
    bool Capped,
    string? WarningFa,
    string? MessageFa);

/// <summary>محور انتخاب‌شده با گزینه‌های محصول.</summary>
public sealed record ProductVariantSelectedAxisInput(
    Guid DefinitionId,
    IReadOnlyList<Guid> OptionIds);

/// <summary>پچ اختیاری روی تنوع موجود هنگام اعمال.</summary>
public sealed record ProductVariantPatchInput(
    Guid VariantId,
    CatalogPublicationStatus? Status,
    string? CatalogCodeSeam,
    int? SortOrder,
    bool? IsDefault);

/// <summary>ورودی اعمال ماتریس تنوع.</summary>
public sealed record ProductVariantApplyInput(
    string? Locale,
    IReadOnlyList<ProductVariantSelectedAxisInput> SelectedAxes,
    Guid? DefaultVariantId,
    IReadOnlyList<ProductVariantPatchInput>? VariantPatches);

/// <summary>نتیجهٔ اعمال ماتریس تنوع.</summary>
public sealed record ProductVariantApplyResult(
    int Created,
    int Unchanged,
    int Deactivated,
    IReadOnlyList<ProductVariantListItem> Variants);

/// <summary>آمادگی تنوع‌ها برای انتشار بعدی.</summary>
public sealed record ProductVariantReadiness(
    bool IsValid,
    IReadOnlyList<string> MissingAxes,
    IReadOnlyList<string> InvalidVariants,
    IReadOnlyList<string> DuplicateCombinations,
    bool? NoDefaultVariant);

/// <summary>گره درخت Admin Category برای Ant Tree آینده.</summary>
public sealed record CategoryTreeNodeDto(
    Guid Id,
    Guid? ParentId,
    string Name,
    string Slug,
    CatalogPublicationStatus Status,
    int SortOrder,
    bool IsVisible,
    bool HasChildren,
    int? ProductCount);

/// <summary>ترجمهٔ رده بدون نشت EF.</summary>
public sealed record CategoryTranslationDto(
    Guid CategoryId,
    string Locale,
    string Name,
    string Slug,
    string? ShortDescription,
    string? Description,
    string? SeoTitle,
    string? SeoDescription,
    string? MetaKeywords,
    DateTimeOffset UpdatedAt);

/// <summary>خلاصهٔ workspace رده برای Admin.</summary>
public sealed record CategoryWorkspaceSummaryDto(
    Guid CategoryId,
    Guid? ParentCategoryId,
    CatalogPublicationStatus Status,
    int SortOrder,
    bool IsVisible,
    Guid? ImageMediaAssetId,
    Guid? IconMediaAssetId,
    Guid? BannerMediaAssetId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<CategoryTranslationDto> Translations);

/// <summary>نتیجهٔ resolve مسیر رده.</summary>
public sealed record CategoryRouteResolveResult(
    Guid CategoryId,
    string Locale,
    string CurrentSlug,
    bool IsRedirect,
    string CanonicalPath);

/// <summary>درخواست ایجاد رده با ترجمه‌های صریح.</summary>
public sealed record CategoryCreateRequest(
    Guid? ParentCategoryId,
    int SortOrder,
    bool IsVisible,
    Guid? ImageMediaAssetId,
    Guid? IconMediaAssetId,
    Guid? BannerMediaAssetId,
    IReadOnlyList<CategoryTranslationUpsertRequest> Translations);

/// <summary>به‌روزرسانی هستهٔ غیرمحلی.</summary>
public sealed record CategoryCoreUpdateRequest(
    CatalogPublicationStatus? Status,
    int? SortOrder,
    bool? IsVisible,
    Guid? ImageMediaAssetId,
    Guid? IconMediaAssetId,
    Guid? BannerMediaAssetId = null,
    bool ClearImage = false,
    bool ClearIcon = false,
    bool ClearBanner = false,
    DateTimeOffset? ExpectedUpdatedAt = null);

/// <summary>درج/به‌روزرسانی ترجمهٔ یک locale.</summary>
public sealed record CategoryTranslationUpsertRequest(
    string Locale,
    string Name,
    string Slug,
    string? ShortDescription = null,
    string? Description = null,
    string? SeoTitle = null,
    string? SeoDescription = null,
    string? MetaKeywords = null);
