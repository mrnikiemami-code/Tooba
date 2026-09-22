#pragma warning disable CS1591
using Tooba.BuildingBlocks;
using Tooba.Host.Admin;
using Tooba.Payment.Endpoints.Admin;

namespace Tooba.Host.Admin;

/// <summary>Host transport adapter for Payment admin Endpoints auth (panel gate only).</summary>
public sealed class HostPaymentAdminAuthorizer : IPaymentAdminAuthorizer
{
    public async Task RequireAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var session = httpContext.RequestServices.GetRequiredService<CurrentAuthenticatedSession>();
        var tenant = httpContext.RequestServices.GetRequiredService<ICurrentTenant>();
        var guard = httpContext.RequestServices.GetRequiredService<IAuthorizationGuard>();
        var environment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();

        await AdminPanelAccess.RequireAuthorizedAsync(
            httpContext.Request, session, tenant, guard, environment, cancellationToken);
    }
}
