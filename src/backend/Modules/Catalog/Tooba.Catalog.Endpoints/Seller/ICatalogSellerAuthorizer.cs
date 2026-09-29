namespace Tooba.Catalog.Endpoints.Seller;

/// <summary>
/// Neutral Catalog seller auth seam for Endpoints. The module owns no seller platform security
/// policy; Host remains the implementer of the neutral <c>ISellerPanelAccess</c> platform seam.
/// </summary>
public interface ICatalogSellerAuthorizer
{
    /// <summary>Requires an authorized seller panel actor and returns (ActorUserId, SellerPartyId).</summary>
    Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken);
}
