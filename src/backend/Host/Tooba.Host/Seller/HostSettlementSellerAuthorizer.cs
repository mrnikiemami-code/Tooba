using Tooba.BuildingBlocks;
using Tooba.Settlement.Endpoints.Seller;

namespace Tooba.Host.Seller;

/// <summary>اتصال Host به درز احراز Settlement seller Endpoints.</summary>
public sealed class HostSettlementSellerAuthorizer : ISettlementSellerAuthorizer
{
    /// <inheritdoc />
    public Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        var session = httpContext.RequestServices.GetRequiredService<CurrentAuthenticatedSession>();
        var guard = httpContext.RequestServices.GetRequiredService<IAuthorizationGuard>();
        var environment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        return SellerPanelAccess.RequireAuthorizedAsync(
            httpContext.Request,
            session,
            guard,
            environment,
            cancellationToken);
    }
}
