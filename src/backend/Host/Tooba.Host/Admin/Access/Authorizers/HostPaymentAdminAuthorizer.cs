#pragma warning disable CS1591
using Tooba.BuildingBlocks.Security;
using Tooba.Payment.Endpoints.Admin;

namespace Tooba.Host.Admin.Access.Authorizers;

/// <summary>Host transport adapter for Payment admin Endpoints auth (thin panel-gate adapter).</summary>
public sealed class HostPaymentAdminAuthorizer(IAdminPanelAccess adminAccess) : IPaymentAdminAuthorizer
{
    public Task RequireAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return adminAccess.RequireAuthorizedAsync(httpContext.Request, cancellationToken);
    }
}
