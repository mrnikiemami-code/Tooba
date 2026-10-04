using Microsoft.AspNetCore.Http;
using Tooba.Fulfillment.Application.Fulfillments.Commands;

namespace Tooba.Fulfillment.Endpoints.Seller;

/// <summary>Neutral seller auth seam for Fulfillment Endpoints (Host implements).</summary>
public interface IFulfillmentSellerAuthorizer
{
    Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
        HttpContext httpContext, CancellationToken cancellationToken);

    Task<SellerHandlePermissionInput> GetHandlePermissionAsync(
        Guid actorUserId, Guid sellerPartyId, CancellationToken cancellationToken);
}
