using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Security;
using Tooba.Order.Endpoints;
using Tooba.Order.Endpoints.Errors;

namespace Tooba.Host.Admin.Access.Authorizers;

/// <summary>
/// Host transport adapter for Order admin Endpoints auth + capability checks.
/// Panel gate delegates to <see cref="IAdminPanelAccess"/>; the capability gate uses the neutral
/// authorization abstraction and fails closed on <see cref="AuthorizationDecisionKind.Unavailable"/>.
/// </summary>
internal sealed class HostOrderAdminAuthorizer(
    IAdminPanelAccess adminAccess,
    IAuthorizationService authz,
    ICurrentTenant tenant) : IOrderAdminAuthorizer
{
    /// <inheritdoc />
    public Task<Guid> RequireAdminAsync(HttpContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        return adminAccess.RequireAuthorizedAsync(context.Request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Guid> RequirePermissionAsync(
        HttpContext context,
        string permissionId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionId);

        var actor = await adminAccess.RequireAuthorizedAsync(context.Request, cancellationToken);
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
            throw new SemanticException(new SemanticError(OrderErrorCodes.AuthorizationUnavailable));
        }

        throw new SemanticException(new SemanticError(OrderErrorCodes.OperationDenied));
    }
}
