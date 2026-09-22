using Microsoft.AspNetCore.Http;

namespace Tooba.Support.Endpoints.Admin;

/// <summary>Neutral admin auth seam for Support Endpoints (Host implements).</summary>
public interface ISupportAdminAuthorizer
{
    /// <summary>
    /// Requires admin panel authorization plus the named Support capability
    /// (support.view / support.manage). Preserves Unavailable fail-open compatibility.
    /// </summary>
    Task<Guid> RequireAuthorizedAsync(
        HttpContext httpContext, string permissionId, CancellationToken cancellationToken);
}
