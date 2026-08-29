namespace Tooba.Host.Admin;

/// <summary>
/// پرچم مجوز Workspace از لایهٔ ویژگی Host. SpiceDB در کامپوننت عمومی UI صدا زده نمی‌شود.
/// </summary>
public sealed record ProductWorkspacePermissions(
    bool CanView,
    bool CanEditCatalog,
    bool CanEditCommercial,
    bool CanEditInventory,
    bool CanPublish);

/// <summary>
/// فرمان ایجاد محصول Catalog به‌صورت پیش‌نویس؛ قیمت و موجودی اینجا نیست.
/// </summary>
public sealed record AdminProductCreateRequest(
    string Title,
    string? Slug,
    Guid? CategoryId,
    string? Locale);

/// <summary>ترجمهٔ محلی محصول از LocalizedText + SlugSeam (معماری locale-based بدون NameFa/NameEn).</summary>
public sealed record ProductTranslationView(
    string Locale,
    string Name,
    string? Slug,
    string? ShortDescription,
    string? Description,
    string? SeoTitle,
    string? SeoDescription);

/// <summary>
/// به‌روزرسانی هستهٔ محصول در یک locale (عنوان، slug انسانی، شرح‌ها، SEO).
/// </summary>
public sealed record AdminProductCoreUpdateRequest(
    string Locale,
    string Title,
    string? Slug,
    string? ShortDescription,
    string? Description,
    string? SeoTitle,
    string? SeoDescription,
    DateTimeOffset ExpectedUpdatedAt);

/// <summary>انتساب ردهٔ محصول با تأیید صریح تغییر.</summary>
public sealed record AdminProductCategoryAssignRequest(
    Guid CategoryId,
    bool ConfirmSchemaImpact,
    DateTimeOffset ExpectedUpdatedAt);

/// <summary>
/// ردیف فهرست Admin. مبلغ و واحد قابل‌فروش از Offer/Price/Inventory ترکیب می‌شوند؛ روی هویت Product نیستند.
/// </summary>
public sealed record AdminProductListItem(
    Guid ProductId,
    string Title,
    string Status,
    int VariantCount,
    int OfferCount,
    string CategorySummary,
    string OfferAmountRange,
    int SellableUnits,
    int LocationCount,
    DateTimeOffset UpdatedAt,
    Guid? PrimaryMediaAssetId);

/// <summary>
/// مدل نمایش ترکیب‌شده. aggregate دامنه نیست.
/// IsPrimaryCategoryAssignable: دستهٔ اصلی سطح سوم است (بدون دسته = false).
/// </summary>
public sealed record ProductWorkspaceView(
    Guid ProductId,
    string Title,
    string Status,
    string Kind,
    string? BrandName,
    IReadOnlyList<string> CategoryNames,
    IReadOnlyList<ProductAttributeView> Attributes,
    IReadOnlyList<ProductVariantView> Variants,
    IReadOnlyList<ProductMediaView> Media,
    IReadOnlyList<ProductOfferView> Offers,
    IReadOnlyList<ProductPriceView> Prices,
    IReadOnlyList<ProductTaxView> TaxClassifications,
    IReadOnlyList<ProductStockView> Stock,
    ProductSeoView Seo,
    ProductPublicationView Publication,
    IReadOnlyList<ProductHistoryItem> Activity,
    IReadOnlyList<ProductHistoryItem> Audit,
    ProductWorkspacePermissions Permissions,
    DateTimeOffset CatalogUpdatedAt,
    IReadOnlyList<string> ReadinessWarnings,
    IReadOnlyList<string> UnsupportedMutations,
    Guid? PrimaryCategoryId = null,
    string? CategoryPath = null,
    string? Slug = null,
    string? ShortDescription = null,
    IReadOnlyList<ProductTranslationView>? Translations = null,
    bool IsPrimaryCategoryAssignable = false);

/// <summary>مشخصهٔ Catalog.</summary>
public sealed record ProductAttributeView(string Code, string Value, bool VariantAxis);

/// <summary>گونهٔ Catalog بدون قیمت.</summary>
public sealed record ProductVariantView(
    Guid VariantId,
    string Fingerprint,
    string Status,
    string? CatalogCodeSeam,
    int OfferCount,
    int LocationCount);

/// <summary>مرجع رسانهٔ مات با ترتیب و تصویر اصلی.</summary>
public sealed record ProductMediaView(
    Guid MediaAssetId,
    bool Primary,
    int DisplayOrder,
    string? AltText);

/// <summary>Offer فروشنده جدا از Product. SellerDisplayName برچسب انسانی است نه کلید دامنه.</summary>
public sealed record ProductOfferView(
    Guid OfferId,
    Guid CatalogVariantId,
    Guid SellerPartyId,
    string SellerDisplayName,
    string Status,
    string Channel,
    string? SellerSku);

/// <summary>قیمت نوشته‌شده جدا از Offer. مبلغ بدون مالیات است.</summary>
public sealed record ProductPriceView(
    Guid PriceId,
    Guid OfferId,
    string Market,
    string Currency,
    decimal AmountExclusiveOfTax,
    string Status,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo);

/// <summary>طبقهٔ مالیاتی Offer.</summary>
public sealed record ProductTaxView(Guid OfferId, Guid CategoryId, string CategoryCode, string DisplayName);

/// <summary>موجودی محل‌دار روی Offer. LocationName برچسب عملیاتی است؛ حقیقت موجودی روی Offer می‌ماند.</summary>
public sealed record ProductStockView(
    Guid OfferId,
    Guid LocationId,
    string LocationCode,
    string LocationName,
    int OnHand,
    int Reserved,
    int Available);

/// <summary>درز SEO. ترکیب صفحه نیست.</summary>
public sealed record ProductSeoView(string? SlugSeam, string? SeoTitleSeam, string SemanticNote);

/// <summary>آمادگی انتشار UI. Published با قابل‌خرید یکی نیست. Checks فقط Catalog است.</summary>
public sealed record ProductPublicationView(
    string CatalogStatus,
    bool PurchasableHint,
    IReadOnlyList<string> Checks,
    ProductPublishReadinessView AggregateReadiness,
    DateTimeOffset StatusUpdatedAt);

/// <summary>مورد ناقص آمادگی انتشار برای چک‌لیست فارسی.</summary>
public sealed record ProductPublishMissingRequirementView(
    string Code,
    string MessageFa,
    string WorkspaceTab);

/// <summary>آمادگی تجمیعی انتشار — بدون Offer/Price/Stock.</summary>
public sealed record ProductPublishReadinessView(
    bool IsReady,
    bool CategoryReady,
    bool TranslationReady,
    bool AttributeReady,
    bool VariantReady,
    bool MediaReady,
    bool SeoReady,
    IReadOnlyList<ProductPublishMissingRequirementView> MissingRequirements,
    string MessageFa);

/// <summary>رویداد Activity یا Audit.</summary>
public sealed record ProductHistoryItem(string Kind, string Summary, DateTimeOffset At);

/// <summary>بدنهٔ افزودن مرجع رسانه.</summary>
public sealed record AdminProductMediaAttachRequest(Guid MediaAssetId, string? AltText);

/// <summary>بدنهٔ افزودن تصویر نمایشی بدون Guid سمت کلاینت.</summary>
public sealed record AdminProductMediaPlaceholderRequest(string? AltText);

/// <summary>بدنهٔ ترتیب گالری.</summary>
public sealed record AdminProductMediaOrderRequest(IReadOnlyList<Guid> OrderedMediaAssetIds);

/// <summary>بدنهٔ ویرایش alt رسانه.</summary>
public sealed record AdminProductMediaPatchRequest(string? AltText);

/// <summary>آمادگی گالری رسانه برای UI و انتشار بعدی.</summary>
public sealed record ProductMediaReadinessView(
    bool HasPrimaryImage,
    int MediaCount,
    bool IsReady,
    string? MessageFa);

/// <summary>بدنهٔ به‌روزرسانی SEO محصول برای یک locale.</summary>
public sealed record AdminProductSeoUpdateRequest(
    string Locale,
    string? Slug,
    string? SeoTitle,
    string? SeoDescription,
    DateTimeOffset ExpectedUpdatedAt);

/// <summary>آمادگی SEO محصول — بدون Offer/Price/Stock.</summary>
public sealed record ProductSeoReadinessView(
    bool HasValidSlug,
    bool HasSeoTitleOrFallback,
    bool HasSeoDescription,
    bool HasLocalizedIdentity,
    bool IsReady,
    string? MessageFa);

/// <summary>جزئیات SEO محصول برای تب Workspace.</summary>
public sealed record ProductSeoDetailView(
    Guid ProductId,
    string Locale,
    string? Slug,
    string? SeoTitle,
    string? SeoDescription,
    string? ProductName,
    string? TitleFallback,
    string PublicPath,
    ProductSeoReadinessView Readiness,
    DateTimeOffset UpdatedAt);

/// <summary>محور یک گونهٔ جدید.</summary>
public sealed record AdminProductVariantAxisRequest(Guid DefinitionId, string? RawValue, Guid? EnumOptionId);

/// <summary>بدنهٔ ایجاد گونه.</summary>
public sealed record AdminProductVariantCreateRequest(
    string? CatalogCodeSeam,
    IReadOnlyList<AdminProductVariantAxisRequest> Axes);

/// <summary>بدنهٔ ویرایش وضعیت/کد گونه بدون تغییر اثرانگشت.</summary>
public sealed record AdminProductVariantPatchRequest(string? Status, string? CatalogCodeSeam);
