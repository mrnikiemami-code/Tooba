using Microsoft.AspNetCore.Http;

namespace Tooba.Returns.Endpoints.Admin;

/// <summary>Neutral admin auth seam for Returns Endpoints (Host implements).</summary>
public interface IReturnAdminAuthorizer
{
    Task<Guid> RequireAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken);
}
