using Tooba.BuildingBlocks.Security;
using Tooba.UserPreference.Endpoints.Admin;

namespace Tooba.Host.Admin.Access.Authorizers;

/// <summary>Host transport adapter for UserPreference admin Endpoints auth.</summary>
public sealed class HostUserPreferenceAdminAuthorizer(IAdminPanelAccess adminAccess) : IUserPreferenceAdminAuthorizer
{
    /// <inheritdoc />
    public Task<Guid> RequireAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return adminAccess.RequireAuthorizedAsync(httpContext.Request, cancellationToken);
    }
}
