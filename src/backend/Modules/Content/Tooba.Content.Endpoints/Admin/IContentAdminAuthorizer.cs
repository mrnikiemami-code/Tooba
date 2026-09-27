using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Security;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Endpoints.Admin;

/// <summary>Thin Content admin authorization seam — stable codes, no localized transport text.</summary>
public interface IContentAdminAuthorizer
{
    Task<Guid> RequireAsync(HttpContext httpContext, string permissionId, CancellationToken cancellationToken);
}

/// <summary>Content admin permission IDs (view/create/edit/publish).</summary>
public static class ContentAdminPermissions
{
    public const string View = "content.view";
    public const string Create = "content.create";
    public const string Edit = "content.edit";
    public const string Publish = "content.publish";
}

/// <summary>Module-owned Content admin authorizer over neutral BuildingBlocks security abstractions.</summary>
public sealed class ContentAdminAuthorizer(
    IAdminPanelAccess adminPanelAccess,
    ICurrentTenant tenant,
    IAuthorizationService authz) : IContentAdminAuthorizer
{
    /// <inheritdoc />
    public async Task<Guid> RequireAsync(
        HttpContext httpContext,
        string permissionId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        var actorUserId = await adminPanelAccess.RequireAuthorizedAsync(httpContext.Request, cancellationToken);

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
            return actorUserId;

        if (decision.Kind == AuthorizationDecisionKind.Unavailable)
        {
            throw new PlatformHttpException(
                StatusCodes.Status503ServiceUnavailable,
                "Authorization unavailable",
                ContentErrorCodes.AuthorizationUnavailable);
        }

        throw new PlatformHttpException(
            StatusCodes.Status403Forbidden,
            "Authorization denied",
            ContentErrorCodes.AuthorizationDenied);
    }
}
