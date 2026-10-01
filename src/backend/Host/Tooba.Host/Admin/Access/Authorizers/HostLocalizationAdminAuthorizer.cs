using Tooba.BuildingBlocks.Security;
using Tooba.Localization.Endpoints.Admin;

namespace Tooba.Host.Admin.Access.Authorizers;

/// <summary>Host transport adapter for Localization admin Endpoints auth.</summary>
public sealed class HostLocalizationAdminAuthorizer(IAdminPanelAccess adminAccess) : ILocalizationAdminAuthorizer
{
    /// <inheritdoc />
    public Task<Guid> RequireAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return adminAccess.RequireAuthorizedAsync(httpContext.Request, cancellationToken);
    }
}
