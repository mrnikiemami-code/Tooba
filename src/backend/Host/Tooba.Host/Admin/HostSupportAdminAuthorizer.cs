#pragma warning disable CS1591
using Tooba.BuildingBlocks;
using Tooba.Host.Admin;
using Tooba.Support.Application.Errors;
using Tooba.Support.Endpoints.Admin;

namespace Tooba.Host.Admin;

/// <summary>Host transport adapter for Support admin Endpoints auth + capabilities.</summary>
public sealed class HostSupportAdminAuthorizer : ISupportAdminAuthorizer
{
    private readonly IAuthorizationService _authz;
    private readonly ICurrentTenant _tenant;

    public HostSupportAdminAuthorizer(IAuthorizationService authz, ICurrentTenant tenant)
    {
        _authz = authz;
        _tenant = tenant;
    }

    public async Task<Guid> RequireAuthorizedAsync(
        HttpContext httpContext, string permissionId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionId);

        var session = httpContext.RequestServices.GetRequiredService<CurrentAuthenticatedSession>();
        var tenant = httpContext.RequestServices.GetRequiredService<ICurrentTenant>();
        var guard = httpContext.RequestServices.GetRequiredService<IAuthorizationGuard>();
        var environment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();

        var actor = await AdminPanelAccess.RequireAuthorizedAsync(
            httpContext.Request, session, tenant, guard, environment, cancellationToken);

        await EnsureAdminCapabilityAsync(actor, permissionId, cancellationToken);
        return actor;
    }

    private async Task EnsureAdminCapabilityAsync(
        Guid actorUserId,
        string permissionId,
        CancellationToken cancellationToken)
    {
        var decision = await _authz.CanAsync(
            new AuthorizationCheck
            {
                Subject = AuthorizationSubject.ForUser(actorUserId),
                Resource = new AuthorizationResource
                {
                    Type = AuthorizationObjectTypes.Permission,
                    Id = permissionId,
                },
                Permission = AuthorizationRelations.Check,
                CallContext = new AuthorizationCallContext
                {
                    Edition = ToobaEdition.SingleStore,
                    TenantId = _tenant.Current?.TenantId.Value ?? "unknown",
                },
            },
            cancellationToken);
        if (decision.Kind == AuthorizationDecisionKind.Allow)
            return;
        // پنل Admin قبلاً tenant#view را پاس کرده؛ تا tupleهای capability پایدار شوند fail-open.
        if (decision.Kind == AuthorizationDecisionKind.Unavailable)
            return;
        throw new PlatformHttpException(403, "مجوز پشتیبانی وجود ندارد.", SupportErrorCodes.AdminAuthorizationDenied);
    }
}
