using Tooba.BuildingBlocks;

namespace Tooba.Host.Admin;

/// <summary>Admin و Storefront برای منوی فروشگاه.</summary>
public static class StoreMenuEndpoints
{
    /// <summary>مسیرها را ثبت می‌کند.</summary>
    public static void MapStoreMenuEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/v1/admin/menus");
        admin.MapGet("/", ListAsync);
        admin.MapPost("/", CreateAsync);
        admin.MapGet("/header", GetHeaderAdminAsync);
        admin.MapPut("/header", SetHeaderAsync);
        admin.MapGet("/{menuId:guid}", GetAsync);
        admin.MapPut("/{menuId:guid}", UpdateAsync);
        admin.MapPut("/{menuId:guid}/enabled", SetEnabledAsync);
        admin.MapGet("/{menuId:guid}/usage", UsageAsync);
        admin.MapDelete("/{menuId:guid}", DeleteAsync);
        admin.MapPost("/{menuId:guid}/items", AddItemAsync);
        admin.MapPut("/{menuId:guid}/items/reorder", ReorderAsync);
        admin.MapPut("/{menuId:guid}/items/{itemId:guid}", UpdateItemAsync);
        admin.MapPut("/{menuId:guid}/items/{itemId:guid}/enabled", SetItemEnabledAsync);
        admin.MapDelete("/{menuId:guid}/items/{itemId:guid}", DeleteItemAsync);

        var storefront = app.MapGroup("/v1/storefront");
        storefront.MapGet("/header-menu", GetHeaderPublicAsync);
        storefront.MapGet("/menus/{menuId:guid}", GetPublicMenuAsync);
    }

    private static async Task<IResult> ListAsync(
        StoreMenuComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            return Results.Json(await composer.ListAsync(cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> GetAsync(
        Guid menuId,
        StoreMenuComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            return Results.Json(await composer.GetAsync(menuId, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> CreateAsync(
        StoreMenuWriteRequest body,
        StoreMenuComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            return Results.Json(await composer.CreateAsync(body, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> UpdateAsync(
        Guid menuId,
        StoreMenuWriteRequest body,
        StoreMenuComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            return Results.Json(await composer.UpdateAsync(menuId, body, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> SetEnabledAsync(
        Guid menuId,
        StoreMenuEnabledRequest body,
        StoreMenuComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            return Results.Json(await composer.SetEnabledAsync(menuId, body.IsEnabled, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> UsageAsync(
        Guid menuId,
        StoreMenuComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            return Results.Json(await composer.GetUsageAsync(menuId, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> DeleteAsync(
        Guid menuId,
        StoreMenuComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            await composer.DeleteAsync(menuId, cancellationToken);
            return Results.Json(new { deleted = true });
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> AddItemAsync(
        Guid menuId,
        StoreMenuItemWriteRequest body,
        StoreMenuComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            return Results.Json(await composer.AddItemAsync(menuId, body, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> UpdateItemAsync(
        Guid menuId,
        Guid itemId,
        StoreMenuItemWriteRequest body,
        StoreMenuComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            return Results.Json(await composer.UpdateItemAsync(menuId, itemId, body, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> SetItemEnabledAsync(
        Guid menuId,
        Guid itemId,
        StoreMenuEnabledRequest body,
        StoreMenuComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            return Results.Json(await composer.SetItemEnabledAsync(menuId, itemId, body.IsEnabled, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> DeleteItemAsync(
        Guid menuId,
        Guid itemId,
        StoreMenuComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            await composer.DeleteItemAsync(menuId, itemId, cancellationToken);
            return Results.Json(new { deleted = true });
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> ReorderAsync(
        Guid menuId,
        StoreMenuItemReorderRequest body,
        StoreMenuComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            return Results.Json(await composer.ReorderItemsAsync(menuId, body.ItemIds, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> GetHeaderAdminAsync(
        StoreMenuComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            return Results.Json(await composer.GetHeaderSelectionAsync(cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> SetHeaderAsync(
        StoreHeaderMenuWriteRequest body,
        StoreMenuComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            return Results.Json(await composer.SetHeaderAsync(body.HeaderMenuId, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> GetHeaderPublicAsync(StoreMenuComposer composer, CancellationToken cancellationToken)
    {
        try
        {
            return Results.Json(await composer.GetHeaderPublicAsync(cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> GetPublicMenuAsync(Guid menuId, StoreMenuComposer composer, CancellationToken cancellationToken)
    {
        try
        {
            return Results.Json(await composer.ProjectPublicAsync(menuId, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }
}
