#pragma warning disable CS1591
using Tooba.BuildingBlocks;
using Tooba.Host.Admin;
using Tooba.Fulfillment.Endpoints.Admin;

namespace Tooba.Host.Admin;

/// <summary>
/// اتصال Host به درز احراز Fulfillment admin Endpoints.
/// Single-Store از tenant موجود؛ Marketplace در Development از tenant پلتفرم synthetic.
/// </summary>
public sealed class HostFulfillmentAdminAuthorizer : IFulfillmentAdminAuthorizer
{
    /// <summary>شناسه tenant synthetic پلتفرم Marketplace در Development.</summary>
    public const string MarketplacePlatformTenantId = "marketplace-platform";

    /// <inheritdoc />
    public async Task<Guid> RequireAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        var session = httpContext.RequestServices.GetRequiredService<CurrentAuthenticatedSession>();
        var tenant = httpContext.RequestServices.GetRequiredService<ICurrentTenant>();
        var registry = httpContext.RequestServices.GetRequiredService<ControlPlaneRegistry>();
        var guard = httpContext.RequestServices.GetRequiredService<IAuthorizationGuard>();
        var environment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();

        if (tenant.Current is not null)
        {
            return await AdminPanelAccess.RequireAuthorizedAsync(
                httpContext.Request, session, tenant, guard, environment, cancellationToken);
        }

        if (!environment.IsDevelopment() || registry.Edition != ToobaEdition.Marketplace)
        {
            throw new PlatformHttpException(503, "زمینهٔ فروشگاه در دسترس نیست.", "admin.tenant.missing");
        }

        var actorUserId = AdminPanelAccess.ResolveActorUserId(httpContext.Request, session, environment);
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

