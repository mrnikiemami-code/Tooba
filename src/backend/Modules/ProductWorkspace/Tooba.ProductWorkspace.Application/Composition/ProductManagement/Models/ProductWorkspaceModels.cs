namespace Tooba.ProductWorkspace.Application.Composition.ProductManagement.Models;

/// <summary>Workspace permission flags derived from X-Tooba-Workspace-Scope transport policy.</summary>
public sealed record ProductWorkspacePermissions(
    bool CanView,
    bool CanEditCatalog,
    bool CanEditCommercial,
    bool CanEditInventory,
    bool CanPublish);

/// <summary>Locale-based product translation view for Admin aggregate GET.</summary>
public sealed record ProductTranslationView(
    string Locale,
    string Name,
    string? Slug,
    string? ShortDescription,
    string? Description,
    string? SeoTitle,
    string? SeoDescription);

/// <summary>Composed Admin ProductWorkspace aggregate read model (not a domain aggregate).</summary>
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
    bool IsPrimaryCategoryAssignable = false,
    Guid? BrandId = null,
    IReadOnlyList<ProductCategoryAssignmentView>? CategoryAssignments = null,
    Guid? UnitOfMeasureId = null,
    int QuantityDecimalPlaces = 0,
    decimal? QuantityStep = null,
    string? UnitCode = null,
    string? UnitDisplayName = null,
    IReadOnlyList<UnitOfMeasureOptionView>? Units = null);

/// <summary>Unit-of-measure option for Workspace quantity policy.</summary>
public sealed record UnitOfMeasureOptionView(Guid UnitOfMeasureId, string Code, string Name, string ShortName);

/// <summary>Category assignment (Primary / Additional).</summary>
public sealed record ProductCategoryAssignmentView(
    Guid CategoryId,
    string CategoryPath,
    string Role);

/// <summary>Catalog attribute projection.</summary>
public sealed record ProductAttributeView(string Code, string Value, bool VariantAxis);

/// <summary>Catalog variant without authored price.</summary>
public sealed record ProductVariantView(
    Guid VariantId,
    string Fingerprint,
    string Status,
    string? CatalogCodeSeam,
    int OfferCount,
    int LocationCount);

/// <summary>Opaque media reference with primary flag and order.</summary>
public sealed record ProductMediaView(
    Guid MediaAssetId,
    bool Primary,
    int DisplayOrder,
    string? AltText);

/// <summary>Seller offer projection. SellerDisplayName is a label, not a domain key.</summary>
public sealed record ProductOfferView(
    Guid OfferId,
    Guid CatalogVariantId,
    Guid SellerPartyId,
    string SellerDisplayName,
    string Status,
    string Channel,
    string? SellerSku);

/// <summary>Authored price exclusive of tax.</summary>
public sealed record ProductPriceView(
    Guid PriceId,
    Guid OfferId,
    string Market,
    string Currency,
    decimal AmountExclusiveOfTax,
    string Status,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo);

/// <summary>Offer tax classification projection.</summary>
public sealed record ProductTaxView(Guid OfferId, Guid CategoryId, string CategoryCode, string DisplayName);

/// <summary>Location-scoped stock on an offer.</summary>
public sealed record ProductStockView(
    Guid OfferId,
    Guid LocationId,
    string LocationCode,
    string LocationName,
    decimal OnHand,
    decimal Reserved,
    decimal Available);

/// <summary>SEO seam projection.</summary>
public sealed record ProductSeoView(string? SlugSeam, string? SeoTitleSeam, string SemanticNote);

/// <summary>Publication shell. PurchasableHint is commercial; Checks are Catalog-only.</summary>
public sealed record ProductPublicationView(
    string CatalogStatus,
    bool PurchasableHint,
    IReadOnlyList<string> Checks,
    ProductPublishReadinessView AggregateReadiness,
    DateTimeOffset StatusUpdatedAt);

/// <summary>Missing publish requirement checklist row.</summary>
public sealed record ProductPublishMissingRequirementView(
    string Code,
    string MessageFa,
    string WorkspaceTab);

/// <summary>Catalog-only publish readiness aggregation.</summary>
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

/// <summary>Activity or Audit history item.</summary>
public sealed record ProductHistoryItem(
    string Kind,
    string Summary,
    DateTimeOffset At,
    string Actor = "سیستم",
    string? Section = null,
    string? BeforeSummary = null,
    string? AfterSummary = null,
    Guid? HistoryId = null);

/// <summary>
/// Admin product list row. Offer amounts and sellable units are composed from Offer/Price/Inventory —
/// they are not authored on Catalog Product identity.
/// CategorySummary is legacy leaf-name join; Admin grid prefers PrimaryCategoryName / AdditionalCategoryNames.
/// </summary>
public sealed record AdminProductListItem(
    Guid ProductId,
    string Title,
    string Status,
    int VariantCount,
    int OfferCount,
    string CategorySummary,
    string OfferAmountRange,
    decimal SellableUnits,
    int LocationCount,
    DateTimeOffset UpdatedAt,
    Guid? PrimaryMediaAssetId,
    Guid? PrimaryCategoryId = null,
    string? BrandName = null,
    string? PrimaryCategoryName = null,
    IReadOnlyList<string>? AdditionalCategoryNames = null,
    int AdditionalCategoryCount = 0);
