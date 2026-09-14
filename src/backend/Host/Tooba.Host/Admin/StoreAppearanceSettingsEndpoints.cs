using Tooba.BuildingBlocks;

namespace Tooba.Host.Admin;

/// <summary>تنظیم پالت ظاهر Store روی Admin Settings موجود.</summary>
public static class StoreAppearanceSettingsEndpoints
{
    /// <summary>مسیرهای Admin را ثبت می‌کند.</summary>
    public static void MapStoreAppearanceSettingsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/admin/settings/appearance");
        group.MapGet("/", GetAsync);
        group.MapPut("/", PutAsync);
    }

    private static async Task<IResult> GetAsync(
        StoreAppearanceSettingsComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            return Results.Json(await composer.GetAsync(cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> PutAsync(
        StoreAppearanceSettingsWriteRequest body,
        StoreAppearanceSettingsComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            return Results.Json(await composer.SaveAsync(body.PaletteKey, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }
}
