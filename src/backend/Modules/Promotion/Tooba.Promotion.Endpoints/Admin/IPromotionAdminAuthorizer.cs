using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Security;

namespace Tooba.Promotion.Endpoints.Admin;

/// <summary>Neutral Promotion admin auth seam for Endpoints (Host keeps <see cref="IAdminPanelAccess"/>).</summary>
public interface IPromotionAdminAuthorizer
{
    Task RequireAuthorizedAsync(HttpContext context, CancellationToken cancellationToken);
}

/// <summary>Module-owned Promotion admin authorizer over <see cref="IAdminPanelAccess"/>.</summary>
public sealed class PromotionAdminAuthorizer(IAdminPanelAccess adminAccess) : IPromotionAdminAuthorizer
{
    public async Task RequireAuthorizedAsync(HttpContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        await adminAccess.RequireAuthorizedAsync(context.Request, cancellationToken);
    }
}
