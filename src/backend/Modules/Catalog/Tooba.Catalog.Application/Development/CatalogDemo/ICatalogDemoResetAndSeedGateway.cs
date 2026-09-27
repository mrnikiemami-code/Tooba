namespace Tooba.Catalog.Application.Development.CatalogDemo;

/// <summary>نتیجهٔ شمارشی reset.</summary>
public sealed record CatalogDemoResetResult(
    int ProductsRemoved,
    int CategoriesRemoved,
    int BrandsRemoved,
    int TagsRemoved,
    int AttributesRemoved,
    int MediaRemoved,
    int MegaMenuRemoved);

/// <summary>شمارش‌های پس از seed برای API و شواهد.</summary>
public sealed record CatalogDemoSeedCounts(
    int Roots,
    int L2,
    int L3,
    int Brands,
    int Tags,
    int AttributeDefinitions,
    int AttributeOptions,
    int CategoryAttributeBindings,
    int Facets,
    int CategoryMediaAssignments,
    int MegaMenuPlacements,
    int Products,
    bool IdempotentReplay);

/// <summary>گزارش ممیزی انتساب محصول↔دسته (سطح و یکتایی Primary).</summary>
public sealed record CatalogAssignmentIntegrityAudit(
    int TotalProducts,
    int PrimaryAtL1OrL2,
    int DisplayAtL1OrL2,
    int DuplicatePrimaryAndAdditional,
    int MultiplePrimary,
    int MissingPrimary,
    int OrphanAssignments,
    IReadOnlyList<Guid> PrimaryAtL1OrL2ProductIds,
    IReadOnlyList<Guid> DisplayAtL1OrL2ProductIds,
    IReadOnlyList<Guid> DuplicateProductIds,
    IReadOnlyList<Guid> MultiplePrimaryProductIds,
    IReadOnlyList<Guid> MissingPrimaryProductIds,
    IReadOnlyList<Guid> OrphanAssignmentProductIds);

/// <summary>نتیجهٔ پاکسازی غیر Production.</summary>
public sealed record CatalogAssignmentIntegrityCleanupResult(
    CatalogAssignmentIntegrityAudit Before,
    CatalogAssignmentIntegrityAudit After,
    int InvalidDisplayRemoved,
    int DuplicateAdditionalRemoved,
    int PrimariesRepairedToL3,
    int ProductsDeleted,
    IReadOnlyList<Guid> DeletedProductIds);

/// <summary>نتیجهٔ کامل reset+seed با اعتبارسنجی یکپارچگی.</summary>
public sealed record CatalogDemoResetAndSeedResult(
    CatalogDemoResetResult Reset,
    CatalogDemoSeedCounts Counts,
    IReadOnlyList<string> Plan,
    CatalogAssignmentIntegrityAudit AssignmentIntegrity);

/// <summary>Application port for Catalog Demo Development reset/seed HTTP and Host composition.</summary>
public interface ICatalogDemoResetAndSeedGateway
{
    /// <summary>Safety gate shared by HTTP and programmatic callers.</summary>
    void EnsureSafetyOrThrow();

    /// <summary>Full reset+seed.</summary>
    Task<CatalogDemoResetAndSeedResult> ExecuteAsync(bool printPlan, CancellationToken cancellationToken);

    /// <summary>Seed only.</summary>
    Task<CatalogDemoSeedCounts> SeedOnlyAsync(CancellationToken cancellationToken);

    /// <summary>Assignment integrity audit.</summary>
    Task<CatalogAssignmentIntegrityAudit> AuditAssignmentsAsync(CancellationToken cancellationToken);

    /// <summary>Assignment integrity cleanup.</summary>
    Task<CatalogAssignmentIntegrityCleanupResult> CleanupAssignmentsAsync(CancellationToken cancellationToken);

    /// <summary>Demo status snapshot for Admin evidence.</summary>
    Task<object> GetStatusAsync(CancellationToken cancellationToken);
}
