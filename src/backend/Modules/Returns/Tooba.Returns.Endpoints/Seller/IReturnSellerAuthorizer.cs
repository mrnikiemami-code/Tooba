using Microsoft.AspNetCore.Http;

namespace Tooba.Returns.Endpoints.Seller;

/// <summary>Neutral seller auth seam for Returns Endpoints (Host implements).</summary>
public interface IReturnSellerAuthorizer
{
    Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
        HttpContext httpContext, CancellationToken cancellationToken);
}
