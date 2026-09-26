using Tooba.Identity.Application;

namespace Tooba.Host;

/// <summary>
/// اعتبارسنجی Bearer به‌عنوان SessionId مات. JWT سفارشی ساخته نمی‌شود و هدر Authorization لاگ نمی‌شود.
/// </summary>
internal sealed class SessionAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// میان‌افزار را به pipeline وصل می‌کند.
    /// </summary>
    public SessionAuthenticationMiddleware(RequestDelegate next) => _next = next;

    /// <summary>
    /// نشست زنده را Resolve می‌کند. حساب Disabled/Locked یا مهر ناهماهنگ اصل نمی‌سازد.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, CurrentAuthenticatedSession current)
    {
        var path = context.Request.Path;
        if (path.StartsWithSegments("/health") || path.StartsWithSegments("/ready"))
        {
            await _next(context);
            return;
        }

        if (TryReadSessionId(context, out var sessionId))
        {
            var sessions = context.RequestServices.GetRequiredService<IIdentitySessionResolver>();
            var identity = await sessions.ResolveAsync(sessionId, context.RequestAborted);
            if (identity is not null)
            {
                current.Assign(identity);
            }
        }

        await _next(context);
    }

    private static bool TryReadSessionId(HttpContext context, out Guid sessionId)
    {
        sessionId = Guid.Empty;
        if (context.Request.Headers.TryGetValue("Authorization", out var header))
        {
            var raw = header.ToString();
            if (raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                && Guid.TryParse(raw["Bearer ".Length..].Trim(), out sessionId))
            {
                return true;
            }
        }

        return context.Request.Cookies.TryGetValue("tooba_session", out var cookie)
            && Guid.TryParse(cookie, out sessionId);
    }
}
