using Microsoft.Extensions.Options;

namespace Tooba.Host;

/// <summary>
/// هدرهای امنیتی پایه بدون CSP سخت که Shopeiva را نشکند.
/// </summary>
internal sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AuthSecurityHostOptions _options;
    private readonly IHostEnvironment _environment;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        IOptions<AuthSecurityHostOptions> options,
        IHostEnvironment environment)
    {
        _next = next;
        _options = options.Value;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_options.EnableSecurityHeaders)
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["X-Frame-Options"] = "SAMEORIGIN";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            if (_options.EnableCspReportOnly)
            {
                headers["Content-Security-Policy-Report-Only"] =
                    "default-src 'self'; img-src 'self' data: https:; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; frame-ancestors 'self'";
            }

            if (_environment.IsProduction() && _options.EnableHsts)
            {
                headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            }
        }

        await _next(context);
    }
}
