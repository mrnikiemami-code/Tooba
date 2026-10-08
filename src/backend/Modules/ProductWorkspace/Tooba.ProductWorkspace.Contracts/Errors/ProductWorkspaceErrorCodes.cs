namespace Tooba.ProductWorkspace.Contracts.Errors;

/// <summary>
/// Stable ProductWorkspace semantic error codes surfaced by the Admin product-workspace HTTP boundary.
/// Identity is the code itself — never a message string and never localized prose. The values are the
/// machine codes emitted by the owning mutation boundary; they must never be renamed or repurposed.
/// <para>
/// Ownership split (descriptor ownership is unique and must not be duplicated — this mirrors the
/// certified <c>PaymentErrorCodes</c> / <c>CartErrorCodes</c> precedent):
/// <list type="bullet">
/// <item><see cref="WorkspaceProductMissing"/> and <see cref="WorkspacePermissionDenied"/> are declared by
/// the <b>Catalog</b> product-workspace capability (<c>CatalogErrorCodes</c>) and registered once by
/// <c>CatalogErrorCatalogContributor</c> (404 / 403). ProductWorkspace is a composing surface that
/// <b>consumes</b> them through the Contracts-only mutation gateway; it declares them here so the
/// <see cref="IsKnown"/> declared-code guard recognises the faults it may surface, and it localizes them
/// with its own <see cref="ProductWorkspaceErrorResourceSet"/>. It never re-registers their descriptors.</item>
/// <item><see cref="CategoryAssignmentStale"/> is likewise declared and registered by Catalog.</item>
/// </list>
/// </para>
/// <para>
/// Transport/input-shape validation codes do <b>not</b> live here. The transport shape of every workspace
/// mutation is validated by the Catalog write capability, which returns the stable
/// <c>workspace.*</c> codes through the Contracts boundary; a module-local validator tree would fork that
/// canonical code set, so none exists.
/// </para>
/// </summary>
public static class ProductWorkspaceErrorCodes
{
    private static readonly HashSet<string> KnownCodes = new(StringComparer.Ordinal)
    {
        WorkspaceProductMissing,
        WorkspacePermissionDenied,
        CategoryAssignmentStale,
    };

    /// <summary>
    /// True when <paramref name="code"/> is a stable code this module's contract boundary is allowed to
    /// surface as a use-case fault. Used by <c>ProductWorkspaceOperation</c> so ProductWorkspace faults
    /// map to <c>Result</c> while codes owned by another module (or an unexpected fault) propagate
    /// untouched to the canonical global exception boundary. Classification is by typed code only —
    /// never by message text.
    /// </summary>
    public static bool IsKnown(string? code) =>
        !string.IsNullOrWhiteSpace(code) && KnownCodes.Contains(code);

    /// <summary>
    /// Workspace product was not found (consumed from Catalog; descriptor owned by
    /// <c>CatalogErrorCatalogContributor</c> as 404 NotFound).
    /// </summary>
    public const string WorkspaceProductMissing = "workspace.product.missing";

    /// <summary>
    /// Workspace catalog edit denied, e.g. <c>X-Tooba-Workspace-Scope=view</c> (consumed from Catalog;
    /// descriptor owned by <c>CatalogErrorCatalogContributor</c> as 403 Forbidden).
    /// </summary>
    public const string WorkspacePermissionDenied = "workspace.permission.denied";

    /// <summary>
    /// <c>expectedUpdatedAt</c> query missing on additional-category DELETE (consumed from Catalog;
    /// descriptor owned by <c>CatalogErrorCatalogContributor</c> as 400 Validation).
    /// </summary>
    public const string CategoryAssignmentStale = "catalog.category.assignment.stale";
}
