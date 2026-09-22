#pragma warning disable CS1591
using Tooba.BuildingBlocks;
using Tooba.Host.Seller;
using Tooba.Notification.Endpoints.Seller;

namespace Tooba.Host.Seller;

/// <summary>Host transport adapter for Notification seller Endpoints auth.</summary>
public sealed class HostNotificationSellerAuthorizer : INotificationSellerAuthorizer
{
    public Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
        HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        var session = httpContext.RequestServices.GetRequiredService<CurrentAuthenticatedSession>();
        var guard = httpContext.RequestServices.GetRequiredService<IAuthorizationGuard>();
        var environment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        return SellerPanelAccess.RequireAuthorizedAsync(
            httpContext.Request, session, guard, environment, cancellationToken);
    }
}
