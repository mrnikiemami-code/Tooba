using Tooba.BuildingBlocks;
using Tooba.Host.Admin;

namespace Tooba.Host.Localization;

/// <summary>API Admin برای رجیستری زبان/محلیه.</summary>
public static class LocaleAdminEndpoints
{
    /// <summary>مسیرهای زبان Admin را ثبت می‌کند.</summary>
    public static void MapLocaleAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/admin/languages");
        group.MapGet("/", ListAsync);
        group.MapPatch("/{code}", PatchAsync);
    }

    private static async Task<IResult> ListAsync(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        SupportedLocaleRegistry registry,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            return Results.Json(registry.List());
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> PatchAsync(
        string code,
        LocalePatchRequest body,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        SupportedLocaleRegistry registry,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            var updated = registry.Patch(code, new SupportedLocalePatch(body.Active, body.IsDefault, body.SortOrder));
            return Results.Json(updated);
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(
                new { title = ex.Message, errorCode = ex.Message },
                statusCode: StatusCodes.Status400BadRequest);
        }
    }
}

/// <summary>بدنهٔ PATCH زبان.</summary>
public sealed record LocalePatchRequest(bool? Active, bool? IsDefault, int? SortOrder);
