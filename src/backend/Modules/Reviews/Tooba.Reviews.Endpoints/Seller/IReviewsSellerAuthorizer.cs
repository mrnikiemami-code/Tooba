namespace Tooba.Reviews.Endpoints.Seller;

/// <summary>Neutral Reviews seller auth seam. Host implements via ISellerPanelAccess.</summary>
public interface IReviewsSellerAuthorizer
{
    Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
        Microsoft.AspNetCore.Http.HttpContext httpContext,
        CancellationToken cancellationToken);
}
