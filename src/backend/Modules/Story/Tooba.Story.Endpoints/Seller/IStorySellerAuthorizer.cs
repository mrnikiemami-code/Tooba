namespace Tooba.Story.Endpoints.Seller;

/// <summary>
/// Neutral Story seller auth seam. Host implements via ISellerPanelAccess.
/// </summary>
public interface IStorySellerAuthorizer
{
    Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
        Microsoft.AspNetCore.Http.HttpContext httpContext,
        CancellationToken cancellationToken);
}
