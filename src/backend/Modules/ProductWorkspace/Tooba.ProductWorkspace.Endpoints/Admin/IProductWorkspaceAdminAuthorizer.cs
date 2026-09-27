using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Security;

namespace Tooba.ProductWorkspace.Endpoints.Admin;

/// <summary>
/// Neutral ProductWorkspace Admin auth seam for Endpoints (Host keeps <see cref="IAdminPanelAccess"/>).
/// No route consumes this in W18.
/// </summary>
public interface IProductWorkspaceAdminAuthorizer
{
    /// <summary>Requires an authorized admin panel actor for ProductWorkspace Admin HTTP.</summary>
    Task<Guid> RequireAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken);
}

/// <summary>
/// Module-owned ProductWorkspace admin authorizer over the neutral platform
/// <see cref="IAdminPanelAccess"/> seam. Host remains the IAdminPanelAccess implementer.
/// </summary>
public sealed class ProductWorkspaceAdminAuthorizer(IAdminPanelAccess adminAccess) : IProductWorkspaceAdminAuthorizer
{
    /// <inheritdoc />
    public Task<Guid> RequireAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return adminAccess.RequireAuthorizedAsync(httpContext.Request, cancellationToken);
    }
}
