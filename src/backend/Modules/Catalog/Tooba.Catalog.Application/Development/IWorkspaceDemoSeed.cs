namespace Tooba.Catalog.Application.Development;

/// <summary>
/// Catalog-owned Development entry point for the live workspace demo dataset
/// (demo product, two-seller marketplace demo, operator-facing copy refresh, Admin R3 preview).
/// Host invokes this as composition only; all data authority stays in Catalog.
/// </summary>
public interface IWorkspaceDemoSeed
{
    /// <summary>True when the live workspace demo product already exists.</summary>
    Task<bool> IsLiveProductSeededAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeds the demo product and the two-seller marketplace demo around its variant.
    /// Idempotent. Never runs outside Development.
    /// </summary>
    Task SeedNewProductAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rewrites legacy operator-facing Catalog and seller copy on the existing demo dataset.
    /// Idempotent. Never runs outside Development.
    /// </summary>
    Task RefreshExistingCopyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Idempotent Admin R3 preview enrichment (five-image gallery, archived and draft products).
    /// Never runs outside Development.
    /// </summary>
    Task EnsureAdminR3PreviewAsync(CancellationToken cancellationToken = default);
}
