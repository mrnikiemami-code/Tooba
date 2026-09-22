using Microsoft.AspNetCore.Http;

namespace Tooba.Fulfillment.Endpoints.Admin;

/// <summary>Neutral admin auth seam for Fulfillment Endpoints (Host implements).</summary>
public interface IFulfillmentAdminAuthorizer
{
    Task<Guid> RequireAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken);
}
