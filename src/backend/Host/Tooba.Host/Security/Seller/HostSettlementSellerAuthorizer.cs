using Tooba.BuildingBlocks.Security;
using Tooba.Settlement.Endpoints.Seller;

namespace Tooba.Host.Security.Seller;

/// <summary>اتصال Host به درز احراز Settlement seller Endpoints.</summary>
public sealed class HostSettlementSellerAuthorizer(ISellerPanelAccess sellerAccess) : ISettlementSellerAuthorizer
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
