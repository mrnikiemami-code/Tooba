using Microsoft.AspNetCore.Http;

namespace Tooba.Identity.Endpoints.Auth;

/// <summary>Auth-sensitive rate-limit seam consumed by Identity Endpoints; Host binds implementation.</summary>
public interface IIdentityAuthThrottle
{
    bool TryAcquire(HttpContext context, string operation);
}
