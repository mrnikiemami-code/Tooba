using Tooba.BuildingBlocks;
using Tooba.AccessControl.Application;
using Tooba.AccessControl.Domain;
using Tooba.Order.Endpoints;

namespace Tooba.Host.Admin;

internal sealed class HostOrderAdminAuthorizer(
    CurrentAuthenticatedSession session,
    ICurrentTenant tenant,
    IAuthorizationGuard guard,
    IHostEnvironment environment,
    IAccessControlDirectory access) : IOrderAdminAuthorizer
{
    public async Task<Guid> RequirePermissionAsync(
        HttpContext context,
        string permissionId,
        CancellationToken cancellationToken)
    {
        var actor = await AdminPanelAccess.RequireAuthorizedAsync(
            context.Request, session, tenant, guard, environment, cancellationToken);
        var scope = new AccessOwnerScope(
            AccessOwnerScopeKind.Platform,
            null,
            tenant.Current?.TenantId.Value);
        var effective = await access.GetEffectiveAccessAsync(actor, scope, cancellationToken);
        var granted = effective.Permissions.Any(permission =>
            !permission.DeniedByCeiling
            && string.Equals(permission.PermissionId, permissionId, StringComparison.OrdinalIgnoreCase));
        if (!granted)
        {
            throw new PlatformHttpException(
                StatusCodes.Status403Forbidden,
                "مجوز انجام این عملیات وجود ندارد.",
                "order.operation.denied");
        }

        return actor;
    }
}
