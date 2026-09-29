using Tooba.BuildingBlocks.Security;
using Tooba.Notification.Endpoints.Seller;

namespace Tooba.Host.Security.Seller;

/// <summary>Host transport adapter for Notification seller Endpoints auth.</summary>
public sealed class HostNotificationSellerAuthorizer(ISellerPanelAccess sellerAccess) : INotificationSellerAuthorizer
{
    /// <inheritdoc />
    public Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
        HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return sellerAccess.RequireAuthorizedAsync(httpContext.Request, cancellationToken);
    }
}
