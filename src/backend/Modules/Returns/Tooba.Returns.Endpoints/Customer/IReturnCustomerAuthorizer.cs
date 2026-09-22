using Microsoft.AspNetCore.Http;

namespace Tooba.Returns.Endpoints.Customer;

/// <summary>Neutral customer auth seam for Returns Endpoints (Host implements).</summary>
public interface IReturnCustomerAuthorizer
{
    /// <summary>Resolves customer actor user id, or null when missing.</summary>
    Guid? TryResolveActor(HttpContext httpContext);
}
