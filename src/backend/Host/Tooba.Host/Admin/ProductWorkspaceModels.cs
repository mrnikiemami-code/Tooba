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
    DateTimeOffset UpdatedAt);

/// <summary>
/// مدل نمایش ترکیب‌شده. aggregate دامنه نیست.
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
    IReadOnlyList<string> UnsupportedMutations);

/// <summary>مشخصهٔ Catalog.</summary>
public sealed record ProductAttributeView(string Code, string Value, bool VariantAxis);

/// <summary>گونهٔ Catalog بدون قیمت.</summary>
public sealed record ProductVariantView(Guid VariantId, string Fingerprint, string Status, int OfferCount, int LocationCount);

/// <summary>مرجع رسانهٔ مات.</summary>
public sealed record ProductMediaView(Guid MediaAssetId, bool Primary);

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

/// <summary>آمادگی انتشار UI. Published با قابل‌خرید یکی نیست.</summary>
public sealed record ProductPublicationView(string CatalogStatus, bool PurchasableHint, IReadOnlyList<string> Checks);

/// <summary>رویداد Activity یا Audit.</summary>
public sealed record ProductHistoryItem(string Kind, string Summary, DateTimeOffset At);
