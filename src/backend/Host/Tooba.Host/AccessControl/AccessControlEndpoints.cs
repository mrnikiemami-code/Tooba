using Tooba.AccessControl.Application;
using Tooba.AccessControl.Application.Authorization;
using Tooba.AccessControl.Domain;
using Tooba.BuildingBlocks;
using Tooba.Host.Admin;

namespace Tooba.Host.AccessControl;

/// <summary>
/// مرز HTTP مرکز کنترل دسترسی Admin و Seller.
/// </summary>
public static class AccessControlEndpoints
{
    /// <summary>مسیرهای access-control را ثبت می‌کند.</summary>
    public static void MapAccessControlEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/v1/admin/access-control");
        admin.MapGet("/demo-preview", AdminDemoPreviewAsync);
    }

    private static string? Trace(HttpRequest request) =>
        request.Headers.TryGetValue("X-Request-Id", out var v) ? v.ToString() : null;

    private static IResult MapError(Exception ex) =>
        ex is AccessControlException ace
            ? Results.Json(new { title = ace.Message, code = ace.Code }, statusCode: ace.Code.Contains("escalation", StringComparison.Ordinal) || ace.Code.Contains("ceiling", StringComparison.Ordinal) ? 403 : 400)
            : Results.Json(new { title = "access.error", code = "access.error" }, statusCode: 500);

    #region Admin platform

    private static IResult AdminDemoPreviewAsync(IHostEnvironment env)
    {
        if (!env.IsDevelopment())
        {
            return Results.NotFound();
        }

        var demo = AccessControlDemoSnapshot.Current;
        return demo is null
            ? Results.Json(new { title = "ACC demo seed not ready", code = "access.demo.not_ready" }, statusCode: StatusCodes.Status503ServiceUnavailable)
            : Results.Json(demo);
    }

    #endregion
}
