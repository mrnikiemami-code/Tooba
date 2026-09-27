namespace Tooba.Catalog.Endpoints.Admin;

/// <summary>
/// Module-owned policy for live <c>X-Tooba-Workspace-Scope</c> transport used by Product Workspace writes.
/// </summary>
public static class CatalogWorkspaceScope
{
    /// <summary>
    /// Returns false when header is <c>view</c> (Host ReadPermissions.CanEditCatalog=false); otherwise true.
    /// </summary>
    public static bool AllowsCatalogEdit(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var scope = request.Headers["X-Tooba-Workspace-Scope"].ToString();
        return !string.Equals(scope, "view", StringComparison.OrdinalIgnoreCase);
    }
}
