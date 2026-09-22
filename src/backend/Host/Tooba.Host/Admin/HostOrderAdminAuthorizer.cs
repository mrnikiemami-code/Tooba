using Tooba.BuildingBlocks;
using Tooba.Order.Endpoints;

namespace Tooba.Host.Admin;

internal sealed class HostOrderAdminAuthorizer(
    CurrentAuthenticatedSession session,
    ICurrentTenant tenant,
    IAuthorizationGuard guard,
    IHostEnvironment environment) : IOrderAdminAuthorizer
{
    public Task<Guid> RequireAuthorizedAsync(HttpContext context, CancellationToken cancellationToken) =>
        AdminPanelAccess.RequireAuthorizedAsync(
            context.Request, session, tenant, guard, environment, cancellationToken);
}
