using Microsoft.AspNetCore.Http;

namespace Tooba.Notification.Endpoints.Seller;

/// <summary>Neutral seller auth seam for Notification Endpoints (Host implements).</summary>
public interface INotificationSellerAuthorizer
{
    /// <summary>Requires seller panel authorization; returns actor + seller party.</summary>
    Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
        HttpContext httpContext, CancellationToken cancellationToken);
}
