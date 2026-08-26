using Microsoft.Extensions.Options;

namespace Tooba.Host;

/// <summary>
/// نقاط پایانی liveness/readiness با سازگاری عقب‌رو برای /health و /ready.
/// </summary>
internal static class HostHealthEndpoints
{
    /// <summary>
    /// مسیرهای health را روی Host ثبت می‌کند.
    /// </summary>
    internal static void Map(WebApplication app)
    {
        app.MapGet("/health/live", () => Results.Json(new { status = "ok" }));
        app.MapGet("/health", () => Results.Json(new { status = "ok" }));

        app.MapGet("/health/ready", EvaluateReadiness);
        app.MapGet("/ready", EvaluateReadiness);
    }

    /// <summary>
    /// readiness را بدون باز کردن DB یا افشای credential برمی‌گرداند.
    /// </summary>
    private static IResult EvaluateReadiness(
        ControlPlaneRegistry registry,
        IOptions<ToobaPlatformOptions> platformOptions,
        IOptions<MessagingHostOptions> messagingOptions,
        IOptions<AuthorizationHostOptions> authorizationOptions,
        IServiceProvider services)
    {
        var evaluation = HostReadinessEvaluator.Evaluate(
            registry,
            platformOptions.Value,
            messagingOptions.Value,
            authorizationOptions.Value,
            services);

        if (!evaluation.Ready)
        {
            return Results.Json(new
            {
                status = "not-ready",
                checks = evaluation.Checks,
            }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Json(new
        {
            status = "ready",
            checks = evaluation.Checks,
        });
    }
}
