using Microsoft.AspNetCore.Http;
using Tooba.Fulfillment.Application.Commands.SellerMutateFulfillment;

namespace Tooba.Fulfillment.Endpoints.Seller;

/// <summary>Neutral seller auth seam for Fulfillment Endpoints (Host implements).</summary>
public interface IFulfillmentSellerAuthorizer
{
    Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
        HttpContext httpContext, CancellationToken cancellationToken);

    Task<SellerHandlePermissionInput> GetHandlePermissionAsync(
        Guid actorUserId, Guid sellerPartyId, CancellationToken cancellationToken);
}
