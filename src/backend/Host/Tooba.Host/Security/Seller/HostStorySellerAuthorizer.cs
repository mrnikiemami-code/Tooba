using Tooba.BuildingBlocks.Security;
using Tooba.Story.Endpoints.Seller;

namespace Tooba.Host.Security.Seller;

/// <summary>
/// Host transport adapter for Story seller Endpoints auth seam over ISellerPanelAccess.
/// </summary>
public sealed class HostStorySellerAuthorizer(ISellerPanelAccess sellerAccess) : IStorySellerAuthorizer
{
    /// <inheritdoc />
    public Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return sellerAccess.RequireAuthorizedAsync(httpContext.Request, cancellationToken);
    }
}
