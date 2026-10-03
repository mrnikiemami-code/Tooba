using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Ports;

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
    /// برچسب تاکسونومی می‌سازد (نام‌های محلی؛ کد پایدار اختیاری با تولید خودکار).
    /// </summary>
    Task<TagView> CreateTagAsync(
        string? code,
        string? slugSeam,
        IReadOnlyDictionary<string, string> localizedNames,
        string? displayLocale,
        CancellationToken cancellationToken);

    /// <summary>
    /// فهرست/جستجوی برچسب‌ها بر اساس نام محلی.
    /// </summary>
    Task<IReadOnlyList<TagView>> ListTagsAsync(string locale, string? search, CancellationToken cancellationToken);

    /// <summary>یک برچسب را با نام محلی برمی‌گرداند.</summary>
    Task<TagView?> GetTagAsync(Guid tagId, string? locale, CancellationToken cancellationToken);

    /// <summary>برچسب را منتشر می‌کند (تحریری؛ صفحهٔ عمومی خودکار نیست).</summary>
    Task PublishTagAsync(Guid tagId, CancellationToken cancellationToken);

    /// <summary>برچسب را به محصول اختصاص می‌دهد؛ تکراری رد می‌شود.</summary>
    Task AssignProductTagAsync(Guid productId, Guid tagId, CancellationToken cancellationToken);

    /// <summary>پیوند محصول-برچسب را حذف می‌کند.</summary>
    Task RemoveProductTagAsync(Guid productId, Guid tagId, CancellationToken cancellationToken);

    /// <summary>برچسب‌های اختصاص‌یافته به محصول.</summary>
    Task<IReadOnlyList<TagView>> ListProductTagsAsync(Guid productId, string? locale, CancellationToken cancellationToken);

    /// <summary>برچسب را به رده اختصاص می‌دهد؛ تکراری رد می‌شود.</summary>
    Task AssignCategoryTagAsync(Guid categoryId, Guid tagId, CancellationToken cancellationToken);

    /// <summary>پیوند رده-برچسب را حذف می‌کند.</summary>
    Task RemoveCategoryTagAsync(Guid categoryId, Guid tagId, CancellationToken cancellationToken);

    /// <summary>برچسب‌های اختصاص‌یافته به رده.</summary>
    Task<IReadOnlyList<TagView>> ListCategoryTagsAsync(Guid categoryId, string? locale, CancellationToken cancellationToken);

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
    /// پیش‌نمایش غیرمخرب اثر غیرفعال‌کردن capability محور تنوع.
    /// </summary>
    Task<VariantAxisCapabilityDisableImpactView> PreviewVariantAxisCapabilityDisableImpactAsync(
        Guid definitionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// قابلیت محور تنوع تعریف را به‌روز می‌کند؛ bindingهای رده را خودکار تغییر نمی‌دهد.
    /// </summary>
    Task SetAttributeDefinitionVariantAxisCapabilityAsync(
        Guid definitionId,
        bool isVariantAxisAllowed,
        CancellationToken cancellationToken);

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
    /// مهاجرت تراکنشی دستهٔ اصلی: حفظ مقادیر سازگار، حذف orphanها، بازبینی تنوع‌های ناسازگار، و Unpublish در صورت ناسازگاری ساختاری روی محصول Published.
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
