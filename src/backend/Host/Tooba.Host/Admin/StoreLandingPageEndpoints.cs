using Tooba.BuildingBlocks;

namespace Tooba.Host.Admin;

/// <summary>Admin و Storefront برای صفحات Landing.</summary>
public static class StoreLandingPageEndpoints
{
    /// <summary>مسیرها را ثبت می‌کند.</summary>
    public static void MapStoreLandingPageEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/v1/admin/pages");
        admin.MapGet("/", ListAsync);
        admin.MapPost("/", CreateAsync);
        admin.MapGet("/{pageId:guid}", GetAsync);
        admin.MapGet("/{pageId:guid}/preview", PreviewAsync);
        admin.MapPut("/{pageId:guid}", UpdateAsync);
        admin.MapPut("/{pageId:guid}/status", SetStatusAsync);
        admin.MapPut("/home", SetHomeAsync);
        admin.MapGet("/home", GetHomeAdminAsync);
        admin.MapGet("/{pageId:guid}/sections", ListSectionsAsync);
        admin.MapPost("/{pageId:guid}/sections", AddSectionAsync);
        admin.MapPut("/{pageId:guid}/sections/composition", ReplaceCompositionAsync);
        admin.MapPut("/{pageId:guid}/sections/reorder", ReorderSectionsAsync);
        admin.MapPut("/{pageId:guid}/sections/{sectionId:guid}", UpdateSectionAsync);
        admin.MapPut("/{pageId:guid}/sections/{sectionId:guid}/enabled", SetSectionEnabledAsync);
        admin.MapDelete("/{pageId:guid}/sections/{sectionId:guid}", DeleteSectionAsync);

        var storefront = app.MapGroup("/v1/storefront");
        storefront.MapGet("/pages", ListPublicSitemapAsync);
        storefront.MapGet("/pages/{slug}", GetPublicAsync);
        storefront.MapGet("/home-selection", GetHomePublicAsync);
    }

    private static async Task<IResult> ListAsync(
        StoreLandingPageComposer composer,
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
        Guid pageId,
        StoreLandingPageComposer composer,
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
            return Results.Json(await composer.GetAsync(pageId, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> PreviewAsync(
        Guid pageId,
        StoreLandingPageComposer composer,
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
            return Results.Json(await composer.ResolvePreviewAsync(pageId, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> CreateAsync(
        StoreLandingPageWriteRequest body,
        StoreLandingPageComposer composer,
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
        Guid pageId,
        StoreLandingPageWriteRequest body,
        StoreLandingPageComposer composer,
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
            return Results.Json(await composer.UpdateAsync(pageId, body, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> SetStatusAsync(
        Guid pageId,
        StoreLandingPageWriteRequest body,
        StoreLandingPageComposer composer,
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
            return Results.Json(await composer.SetStatusAsync(pageId, body.Status, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> SetHomeAsync(
        StoreHomeSelectionWriteRequest body,
        StoreLandingPageComposer composer,
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
            return Results.Json(await composer.SetHomeAsync(body.HomePageId, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> GetHomeAdminAsync(
        StoreLandingPageComposer composer,
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
            return Results.Json(await composer.GetHomeSelectionAsync(cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> ListSectionsAsync(
        Guid pageId,
        StoreLandingPageComposer composer,
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
            return Results.Json(await composer.ListSectionsAsync(pageId, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> AddSectionAsync(
        Guid pageId,
        StoreLandingPageSectionWriteRequest body,
        StoreLandingPageComposer composer,
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
            return Results.Json(await composer.AddSectionAsync(pageId, body, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> ReplaceCompositionAsync(
        Guid pageId,
        StoreLandingPageCompositionReplaceRequest body,
        StoreLandingPageComposer composer,
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
            return Results.Json(await composer.ReplaceCompositionAsync(pageId, body.Sections, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> UpdateSectionAsync(
        Guid pageId,
        Guid sectionId,
        StoreLandingPageSectionWriteRequest body,
        StoreLandingPageComposer composer,
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
            return Results.Json(await composer.UpdateSectionAsync(pageId, sectionId, body, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> SetSectionEnabledAsync(
        Guid pageId,
        Guid sectionId,
        StoreLandingPageSectionEnabledRequest body,
        StoreLandingPageComposer composer,
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
            return Results.Json(await composer.SetSectionEnabledAsync(pageId, sectionId, body.IsEnabled, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> ReorderSectionsAsync(
        Guid pageId,
        StoreLandingPageSectionReorderRequest body,
        StoreLandingPageComposer composer,
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
            return Results.Json(await composer.ReorderSectionsAsync(pageId, body.SectionIds, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> DeleteSectionAsync(
        Guid pageId,
        Guid sectionId,
        StoreLandingPageComposer composer,
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
            await composer.DeleteSectionAsync(pageId, sectionId, cancellationToken);
            return Results.Json(new { ok = true });
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> GetPublicAsync(
        string slug,
        StoreLandingPageComposer composer,
        string? locale,
        CancellationToken cancellationToken)
    {
        var page = await composer.ResolvePublicAsync(locale, slug, cancellationToken);
        return page is null
            ? Results.Json(new { title = "Not Found", errorCode = "landing.page.missing" }, statusCode: StatusCodes.Status404NotFound)
            : Results.Json(page);
    }

    private static async Task<IResult> ListPublicSitemapAsync(
        StoreLandingPageComposer composer,
        CancellationToken cancellationToken)
        => Results.Json(await composer.ListIndexableLandingsAsync(cancellationToken));

    private static async Task<IResult> GetHomePublicAsync(
        StoreLandingPageComposer composer,
        CancellationToken cancellationToken)
        => Results.Json(await composer.GetHomeSelectionAsync(cancellationToken));
}
