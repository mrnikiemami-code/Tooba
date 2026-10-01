using Tooba.BuildingBlocks.Security;
using Tooba.Reviews.Endpoints.Seller;

namespace Tooba.Host.Security.Seller;

/// <summary>
/// Host transport adapter for Reviews seller Endpoints auth seam over ISellerPanelAccess.
/// </summary>
public sealed class HostReviewsSellerAuthorizer(ISellerPanelAccess sellerAccess) : IReviewsSellerAuthorizer
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
