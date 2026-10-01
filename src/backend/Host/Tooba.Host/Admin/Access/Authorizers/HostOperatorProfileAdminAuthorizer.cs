using Tooba.BuildingBlocks.Security;
using Tooba.OperatorProfile.Endpoints.Admin;

namespace Tooba.Host.Admin.Access.Authorizers;

/// <summary>Host transport adapter for OperatorProfile admin Endpoints auth.</summary>
public sealed class HostOperatorProfileAdminAuthorizer(IAdminPanelAccess adminAccess) : IOperatorProfileAdminAuthorizer
{
    /// <inheritdoc />
    public Task<Guid> RequireAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return adminAccess.RequireAuthorizedAsync(httpContext.Request, cancellationToken);
    }
}
