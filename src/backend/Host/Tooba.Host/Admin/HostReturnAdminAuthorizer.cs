#pragma warning disable CS1591
using Tooba.BuildingBlocks;
using Tooba.Host.Admin;
using Tooba.Returns.Endpoints.Admin;

namespace Tooba.Host.Admin;

/// <summary>Host transport adapter for Returns admin Endpoints auth.</summary>
public sealed class HostReturnAdminAuthorizer : IReturnAdminAuthorizer
{
    public Task<Guid> RequireAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        var session = httpContext.RequestServices.GetRequiredService<CurrentAuthenticatedSession>();
        var tenant = httpContext.RequestServices.GetRequiredService<ICurrentTenant>();
        var guard = httpContext.RequestServices.GetRequiredService<IAuthorizationGuard>();
        var environment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        return AdminPanelAccess.RequireAuthorizedAsync(
            httpContext.Request, session, tenant, guard, environment, cancellationToken);
    }
}
