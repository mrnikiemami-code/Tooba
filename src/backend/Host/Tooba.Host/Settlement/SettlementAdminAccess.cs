using Tooba.BuildingBlocks;
using Tooba.Host.Admin;

namespace Tooba.Host.Settlement;

/// <summary>
/// مجوز admin تسویه: Single-Store از tenant موجود؛ Marketplace در Development از tenant پلتفرم synthetic.
/// </summary>
internal static class SettlementAdminAccess
{
    internal const string MarketplacePlatformTenantId = "marketplace-platform";

    public static async Task<Guid> RequireAuthorizedAsync(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant currentTenant,
        ControlPlaneRegistry registry,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (currentTenant.Current is not null)
        {
            return await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, currentTenant, guard, environment, cancellationToken);
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
