#pragma warning disable CS1591
using Tooba.BuildingBlocks.Security;
using Tooba.Promotion.Endpoints.Admin;

namespace Tooba.Host.Admin.Access.Authorizers;

/// <summary>Host transport adapter for Promotion admin Endpoints auth (thin panel-gate adapter).</summary>
public sealed class HostPromotionAdminAuthorizer(IAdminPanelAccess adminAccess) : IPromotionAdminAuthorizer
{
    public async Task RequireAuthorizedAsync(HttpContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        await adminAccess.RequireAuthorizedAsync(context.Request, cancellationToken);
    }
}
