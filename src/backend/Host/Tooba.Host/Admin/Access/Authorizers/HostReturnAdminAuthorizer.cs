#pragma warning disable CS1591
using Tooba.BuildingBlocks.Security;
using Tooba.Returns.Endpoints.Admin;

namespace Tooba.Host.Admin.Access.Authorizers;

/// <summary>Host transport adapter for Returns admin Endpoints auth (thin panel-gate adapter).</summary>
public sealed class HostReturnAdminAuthorizer(IAdminPanelAccess adminAccess) : IReturnAdminAuthorizer
{
    public Task<Guid> RequireAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return adminAccess.RequireAuthorizedAsync(httpContext.Request, cancellationToken);
    }
}
