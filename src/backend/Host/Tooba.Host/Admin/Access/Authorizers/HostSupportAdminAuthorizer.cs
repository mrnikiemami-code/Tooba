#pragma warning disable CS1591
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Security;
using Tooba.Support.Endpoints.Admin;

namespace Tooba.Host.Admin.Access.Authorizers;

/// <summary>
/// Host transport adapter for Support admin Endpoints auth + capabilities.
/// Panel gate delegates to <see cref="IAdminPanelAccess"/>; capability checks fail closed on
/// <see cref="AuthorizationDecisionKind.Unavailable"/>.
/// </summary>
public sealed class HostSupportAdminAuthorizer(
    IAdminPanelAccess adminAccess,
    IAuthorizationService authz,
    ICurrentTenant tenant) : ISupportAdminAuthorizer
{
    /// <inheritdoc />
    public async Task<Guid> RequireAuthorizedAsync(
        HttpContext httpContext, string permissionId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionId);

        var actor = await adminAccess.RequireAuthorizedAsync(httpContext.Request, cancellationToken);
        await EnsureAdminCapabilityAsync(actor, permissionId, cancellationToken);
        return actor;
    }

    private async Task EnsureAdminCapabilityAsync(
        Guid actorUserId,
        string permissionId,
        CancellationToken cancellationToken)
    {
        var decision = await authz.CanAsync(
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
                    TenantId = tenant.Current?.TenantId.Value ?? "unknown",
                },
            },
            cancellationToken);

        if (decision.Kind == AuthorizationDecisionKind.Allow)
        {
            return;
        }

        // شکست زیرساخت مجوز هرگز ALLOW نیست؛ capability باید fail-closed بماند.
        if (decision.Kind == AuthorizationDecisionKind.Unavailable)
        {
            throw new PlatformHttpException(
                503,
                "سرویس مجوز در دسترس نیست.",
                SupportAdminAuthorizationCodes.AuthorizationUnavailable);
        }

        throw new PlatformHttpException(
            403,
            "مجوز پشتیبانی وجود ندارد.",
            FoundationErrorCodes.AdminAuthorizationDenied);
    }
}
