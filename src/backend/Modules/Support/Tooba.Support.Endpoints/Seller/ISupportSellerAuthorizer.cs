using Microsoft.AspNetCore.Http;

namespace Tooba.Support.Endpoints.Seller;

/// <summary>Neutral seller auth seam for Support Endpoints (Host implements).</summary>
public interface ISupportSellerAuthorizer
{
    /// <summary>
    /// Requires seller panel authorization plus the named Support capability
    /// (support.view / support.create / support.reply).
    /// </summary>
    Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
        HttpContext httpContext, string permissionId, CancellationToken cancellationToken);
}
