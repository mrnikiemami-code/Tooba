using Tooba.BuildingBlocks.Security;
using Tooba.Returns.Endpoints.Seller;

namespace Tooba.Host.Security.Seller;

/// <summary>Host transport adapter for Returns seller Endpoints auth.</summary>
public sealed class HostReturnSellerAuthorizer(ISellerPanelAccess sellerAccess) : IReturnSellerAuthorizer
{
    /// <inheritdoc />
    public Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
        HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return sellerAccess.RequireAuthorizedAsync(httpContext.Request, cancellationToken);
    }
}
