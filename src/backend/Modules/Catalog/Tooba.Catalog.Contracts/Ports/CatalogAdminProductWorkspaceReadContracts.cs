namespace Tooba.Catalog.Contracts.Ports;

/// <summary>
/// Catalog-owned Admin ProductWorkspace aggregate read boundary.
/// Returns a semantic projection — never EF/domain entities or queryables.
/// </summary>
public interface ICatalogAdminProductWorkspaceReadGateway
{
    /// <summary>
    /// Loads the Catalog slice required to compose Admin aggregate GET for one product.
    /// Returns null when the product does not exist.
    /// </summary>
    Task<CatalogAdminProductWorkspaceSnapshot?> GetAggregateSnapshotAsync(
        Guid productId,
        CancellationToken cancellationToken);
}

/// <summary>Cohesive Catalog-owned snapshot for ProductWorkspace aggregate GET composition.</summary>
public sealed record CatalogAdminProductWorkspaceSnapshot(
    Guid ProductId,
    string Title,
    string Status,
    string Kind,
    string? BrandName,
    Guid? BrandId,
    string? SlugSeam,
    string? SeoTitleSeam,
    string? ShortDescription,
    DateTimeOffset UpdatedAt,
    Guid? UnitOfMeasureId,
    int QuantityDecimalPlaces,
    decimal? QuantityStep,
    string? UnitCode,
    string? UnitDisplayName,
    IReadOnlyList<CatalogAdminUnitOption> Units,
    IReadOnlyList<string> CategoryNames,
    Guid? PrimaryCategoryId,
    string? CategoryPath,
    bool IsPrimaryCategoryAssignable,
    string? PrimaryCategoryAssignableWarningFa,
    IReadOnlyList<CatalogAdminCategoryAssignment> CategoryAssignments,
    IReadOnlyList<CatalogAdminProductAttribute> Attributes,
    IReadOnlyList<CatalogAdminProductVariant> Variants,
    IReadOnlyList<CatalogAdminProductMedia> Media,
    IReadOnlyList<CatalogAdminProductTranslation> Translations,
    CatalogAdminPublishReadiness PublishReadiness,
    IReadOnlyList<string> CatalogReadinessMessages,
    IReadOnlyList<CatalogAdminHistoryItem> Activity,
    IReadOnlyList<CatalogAdminHistoryItem> Audit);

/// <summary>Unit-of-measure option for Admin workspace quantity policy UI.</summary>
public sealed record CatalogAdminUnitOption(Guid UnitOfMeasureId, string Code, string Name, string ShortName);

/// <summary>Category assignment with full path and role label.</summary>
public sealed record CatalogAdminCategoryAssignment(Guid CategoryId, string CategoryPath, string Role);

/// <summary>Product or variant-axis attribute projection.</summary>
public sealed record CatalogAdminProductAttribute(string Code, string Value, bool VariantAxis);

/// <summary>Catalog variant fields without commercial OfferCount/LocationCount.</summary>
public sealed record CatalogAdminProductVariant(
    Guid VariantId,
    string Fingerprint,
    string Status,
    string? CatalogCodeSeam);

/// <summary>Media reference projection.</summary>
public sealed record CatalogAdminProductMedia(
    Guid MediaAssetId,
    bool Primary,
    int DisplayOrder,
    string? AltText);

/// <summary>Localized product translation projection.</summary>
public sealed record CatalogAdminProductTranslation(
    string Locale,
    string Name,
    string? Slug,
    string? ShortDescription,
    string? Description,
    string? SeoTitle,
    string? SeoDescription);

/// <summary>Catalog publish readiness shell (no Offer/Price/Stock).</summary>
public sealed record CatalogAdminPublishReadiness(
    bool IsReady,
    bool CategoryReady,
    bool TranslationReady,
    bool AttributeReady,
    bool VariantReady,
    bool MediaReady,
    bool SeoReady,
    IReadOnlyList<CatalogAdminPublishMissingRequirement> MissingRequirements,
    string MessageFa);

/// <summary>Missing publish requirement checklist row.</summary>
public sealed record CatalogAdminPublishMissingRequirement(
    string Code,
    string MessageFa,
    string WorkspaceTab);

/// <summary>Activity/Audit history shell row.</summary>
public sealed record CatalogAdminHistoryItem(
    string Kind,
    string Summary,
    DateTimeOffset At,
    string Actor = "سیستم",
    string? Section = null,
    string? BeforeSummary = null,
    string? AfterSummary = null,
    Guid? HistoryId = null);
