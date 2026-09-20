using Tooba.BuildingBlocks;
using Tooba.Offer.Application;
using Tooba.Offer.Endpoints;

namespace Tooba.Host.Seller;

/// <summary>
/// اتصال Host به درز احراز Offer Endpoints.
/// </summary>
public sealed class HostOfferSellerAuthorizer : IOfferSellerAuthorizer
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
