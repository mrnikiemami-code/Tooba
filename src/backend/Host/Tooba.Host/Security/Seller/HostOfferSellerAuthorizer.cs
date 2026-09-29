using Tooba.BuildingBlocks.Security;
using Tooba.Offer.Endpoints.Seller;

namespace Tooba.Host.Security.Seller;

/// <summary>
/// اتصال Host به درز احراز Offer Endpoints.
/// </summary>
public sealed class HostOfferSellerAuthorizer(ISellerPanelAccess sellerAccess) : IOfferSellerAuthorizer
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
