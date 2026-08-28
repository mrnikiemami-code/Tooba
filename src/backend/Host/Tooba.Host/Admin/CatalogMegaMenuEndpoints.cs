using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;

namespace Tooba.Host.Admin;

/// <summary>
/// مسیرهای Admin/Storefront برای پیکربندی مگامنو رده.
/// </summary>
public static class CatalogMegaMenuEndpoints
{
    /// <summary>
    /// مسیرهای مگامنو را ثبت می‌کند.
    /// </summary>
    public static void MapCatalogMegaMenuEndpoints(this WebApplication app)
    {
        var categories = app.MapGroup("/v1/admin/catalog/categories/{categoryId:guid}/mega-menu");
        categories.MapGet("", GetCategoryMegaMenuAsync);
        categories.MapGet("/placement-options", ListPlacementOptionsAsync);
        categories.MapPut("", UpsertCategoryMegaMenuAsync);
        categories.MapDelete("", RemoveCategoryMegaMenuAsync);

        app.MapGet("/v1/storefront/mega-menu", GetStorefrontMegaMenuAsync);
    }

    private static async Task<IResult> GetCategoryMegaMenuAsync(
        Guid categoryId,
        ICatalogDirectory catalog,
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
            var locale = ReadLocale(request);
            return Results.Json(await catalog.GetCategoryMegaMenuConfigurationAsync(categoryId, locale, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> ListPlacementOptionsAsync(
        Guid categoryId,
        ICatalogDirectory catalog,
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
            var locale = ReadLocale(request);
            return Results.Json(await catalog.ListMegaMenuPlacementOptionsAsync(categoryId, locale, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> UpsertCategoryMegaMenuAsync(
        Guid categoryId,
        CategoryMegaMenuBindingInput body,
        ICatalogDirectory catalog,
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
            var locale = ReadLocale(request);
            await catalog.UpsertCategoryMegaMenuBindingAsync(categoryId, locale, body, cancellationToken);
            return Results.NoContent();
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> RemoveCategoryMegaMenuAsync(
        Guid categoryId,
        ICatalogDirectory catalog,
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
            await catalog.RemoveCategoryMegaMenuBindingAsync(categoryId, cancellationToken);
            return Results.NoContent();
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> GetStorefrontMegaMenuAsync(
        ICatalogDirectory catalog,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        var locale = ReadLocale(request);
        return Results.Json(await catalog.GetStorefrontMegaMenuAsync(locale, cancellationToken));
    }

    private static string ReadLocale(HttpRequest request) =>
        request.Query.TryGetValue("locale", out var values) && values.Count > 0
            ? values[0]!
            : "fa-IR";

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Problem(ex.Message, statusCode: ex.StatusCode);
}
