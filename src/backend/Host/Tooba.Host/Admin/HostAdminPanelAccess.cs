using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Security;

namespace Tooba.Host.Admin;

/// <summary>
/// درز عمومی Host برای دسترسی پنل مدیر: Single-Store از Tenant موجود و در Development/Marketplace
/// از Tenant synthetic پلتفرم. هیچ سیاست ماژولی اینجا نیست.
/// </summary>
internal sealed class HostAdminPanelAccess(
    CurrentAuthenticatedSession session,
    ICurrentTenant tenant,
    ControlPlaneRegistry registry,
    IAuthorizationGuard guard,
    IHostEnvironment environment) : IAdminPanelAccess
{
    /// <summary>شناسه tenant synthetic پلتفرم Marketplace در Development.</summary>
    public const string MarketplacePlatformTenantId = "marketplace-platform";

    /// <inheritdoc />
    public async Task<Guid> RequireAuthorizedAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (tenant.Current is not null)
        {
            return await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
        }

        if (!environment.IsDevelopment() || registry.Edition != ToobaEdition.Marketplace)
        {
            throw new PlatformHttpException(503, "زمینهٔ فروشگاه در دسترس نیست.", "admin.tenant.missing");
        }

        var actorUserId = AdminPanelAccess.ResolveActorUserId(request, session, environment);
        var decision = await guard.AuthorizeUseCaseAsync(
            new AuthorizationCheck
            {
                Subject = AuthorizationSubject.ForUser(actorUserId),
                Resource = new AuthorizationResource
                {
                    Type = AuthorizationObjectTypes.Tenant,
                    Id = MarketplacePlatformTenantId,
                },
                Permission = AuthorizationRelations.View,
                CallContext = new AuthorizationCallContext
                {
                    Edition = ToobaEdition.Marketplace,
                },
            },
            cancellationToken);

        if (decision.Kind == AuthorizationDecisionKind.Allow)
        {
            return actorUserId;
        }

        if (decision.Kind == AuthorizationDecisionKind.Unavailable)
        {
            throw new PlatformHttpException(503, "سرویس مجوز در دسترس نیست.", "admin.authorization.unavailable");
        }

        throw new PlatformHttpException(403, "دسترسی مدیریت marketplace مجاز نیست.", "admin.authorization.denied");
    }
}
