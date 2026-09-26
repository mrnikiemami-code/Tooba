using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Tooba.Host;

/// <summary>
/// خطاهای مرز احراز به‌صورت ProblemDetails با traceId و errorCode. راز و وجود حساب لو نمی‌رود.
/// </summary>
internal static class AuthenticationHttpProblem
{
    private static readonly HashSet<string> ForbiddenTenantKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "X-Tenant-Id", "X-TenantId", "TenantId", "tenantId", "tenant_id",
    };

    /// <summary>
    /// Tenant جعلی از هدر، کوئری، کوکی، بدنه یا extension-data را رد می‌کند.
    /// </summary>
    public static IResult? RejectUntrustedTenant(
        HttpContext http,
        string? bodyTenantId,
        IReadOnlyDictionary<string, JsonElement>? extra)
    {
        if (!string.IsNullOrWhiteSpace(bodyTenantId))
        {
            return BadRequest(http, "identity.tenant.untrusted");
        }

        if (extra is not null)
        {
            foreach (var key in extra.Keys)
            {
                if (ForbiddenTenantKeys.Contains(key))
                {
                    return BadRequest(http, "identity.tenant.untrusted");
                }
            }
        }

        foreach (var key in ForbiddenTenantKeys)
        {
            if (http.Request.Headers.ContainsKey(key))
            {
                return BadRequest(http, "identity.tenant.untrusted");
            }
        }

        if (http.Request.Query.ContainsKey("tenantId") || http.Request.Query.ContainsKey("tenant_id") || http.Request.Query.ContainsKey("TenantId"))
        {
            return BadRequest(http, "identity.tenant.untrusted");
        }

        if (http.Request.Cookies.ContainsKey("tenantId")
            || http.Request.Cookies.ContainsKey("TenantId")
            || http.Request.Cookies.ContainsKey("tenant_id"))
        {
            return BadRequest(http, "identity.tenant.untrusted");
        }

        return null;
    }

    /// <summary>
    /// اگر permیت این operation در پنجرهٔ جاری تمام شده باشد 429 enumeration-safe برمی‌گرداند.
    /// </summary>
    public static IResult? RejectIfThrottled(HttpContext http, IAuthenticationThrottleSeam throttle, string operation)
    {
        if (throttle.TryAcquire(http, operation))
        {
            return null;
        }

        return AuthProblem(http, StatusCodes.Status429TooManyRequests, "Too Many Requests", "identity.rate_limited");
    }

    /// <summary>ProblemDetails 400 با کد خطای مرز احراز.</summary>
    public static IResult BadRequest(HttpContext http, string errorCode) =>
        AuthProblem(http, StatusCodes.Status400BadRequest, "Bad Request", errorCode);

    /// <summary>ProblemDetails 401 با کد خطای مرز احراز.</summary>
    public static IResult Unauthorized(HttpContext http, string errorCode) =>
        AuthProblem(http, StatusCodes.Status401Unauthorized, "Unauthorized", errorCode);

    /// <summary>ProblemDetails بدون نشت راز؛ traceId از Activity جاری می‌آید.</summary>
    public static IResult AuthProblem(HttpContext http, int status, string title, string errorCode)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? http.TraceIdentifier;
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = "about:blank",
        };
        problem.Extensions["traceId"] = traceId;
        problem.Extensions["errorCode"] = errorCode;
        return Results.Json(problem, statusCode: status, contentType: "application/problem+json");
    }
}
