using Microsoft.AspNetCore.Http;

namespace Tooba.Support.Endpoints.Customer;

/// <summary>Neutral customer auth seam for Support Endpoints (Host implements).</summary>
public interface ISupportCustomerAuthorizer
{
    /// <summary>Resolves customer actor user id, or null when unauthenticated.</summary>
    Guid? TryResolveActor(HttpContext httpContext);
}
