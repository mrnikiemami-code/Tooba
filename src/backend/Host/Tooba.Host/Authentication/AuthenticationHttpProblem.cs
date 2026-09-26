using System.Text.Json;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Identity.Contracts.Problems;

namespace Tooba.Host;

/// <summary>
/// Tenant-spoof and throttle decisions stay auth-specific, but every error response is presented
/// through the canonical <see cref="ApiResponseFactory"/> so status codes, machine codes, trace/
/// correlation ids, and localization come from the shared pipeline. Secrets and account existence
/// are never exposed.
/// </summary>
internal static class AuthenticationHttpProblem
{
    private static readonly HashSet<string> ForbiddenTenantKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "X-Tenant-Id", "X-TenantId", "TenantId", "tenantId", "tenant_id",
    };

    /// <summary>
    /// Rejects a client-presented tenant from header, query, cookie, body, or extension data.
    /// </summary>
    public static IResult? RejectUntrustedTenant(
        HttpContext http,
        string? bodyTenantId,
        IReadOnlyDictionary<string, JsonElement>? extra)
    {
        if (!string.IsNullOrWhiteSpace(bodyTenantId))
        {
            return BadRequest(http, IdentityErrorCodes.TenantUntrusted);
        }

        if (extra is not null)
        {
            foreach (var key in extra.Keys)
            {
                if (ForbiddenTenantKeys.Contains(key))
                {
                    return BadRequest(http, IdentityErrorCodes.TenantUntrusted);
                }
            }
        }

        foreach (var key in ForbiddenTenantKeys)
        {
            if (http.Request.Headers.ContainsKey(key))
            {
                return BadRequest(http, IdentityErrorCodes.TenantUntrusted);
            }
        }

        if (http.Request.Query.ContainsKey("tenantId") || http.Request.Query.ContainsKey("tenant_id") || http.Request.Query.ContainsKey("TenantId"))
        {
            return BadRequest(http, IdentityErrorCodes.TenantUntrusted);
        }

        if (http.Request.Cookies.ContainsKey("tenantId")
            || http.Request.Cookies.ContainsKey("TenantId")
            || http.Request.Cookies.ContainsKey("tenant_id"))
        {
            return BadRequest(http, IdentityErrorCodes.TenantUntrusted);
        }

        return null;
    }

    /// <summary>
    /// Returns an enumeration-safe 429 when the current operation window is exhausted.
    /// </summary>
    public static IResult? RejectIfThrottled(HttpContext http, IAuthenticationThrottleSeam throttle, string operation)
    {
        if (throttle.TryAcquire(http, operation))
        {
            return null;
        }

        return AuthProblem(http, IdentityErrorCodes.RateLimited);
    }

    /// <summary>ProblemDetails 400 with the auth boundary machine error code.</summary>
    public static IResult BadRequest(HttpContext http, string errorCode) =>
        AuthProblem(http, errorCode);

    /// <summary>ProblemDetails 401 with the auth boundary machine error code.</summary>
    public static IResult Unauthorized(HttpContext http, string errorCode) =>
        AuthProblem(http, errorCode);

    /// <summary>
    /// Canonical ProblemDetails presentation for an auth boundary machine code. Status, title,
    /// localization, and trace/correlation ids come from the shared factory; no parallel pipeline.
    /// </summary>
    public static IResult AuthProblem(HttpContext http, string errorCode)
    {
        var factory = ResolveFactory(http);
        if (factory is not null)
        {
            return factory.FromFailure(new SemanticError(errorCode));
        }

        // Composition fallback: never happens in the composed Host, keeps the method total.
        return Results.Problem(
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Request failed.",
            extensions: new Dictionary<string, object?> { ["errorCode"] = errorCode });
    }

    private static ApiResponseFactory? ResolveFactory(HttpContext http) =>
        http.RequestServices.GetService(typeof(ApiResponseFactory)) as ApiResponseFactory;
}
