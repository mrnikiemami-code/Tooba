using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;

namespace Tooba.Host.Admin;

/// <summary>
/// مسیرهای Admin برای پیکربندی facet فیلتر PLP رده.
/// </summary>
public static class CatalogFacetEndpoints
{
    /// <summary>
    /// مسیرهای facet رده را ثبت می‌کند.
    /// </summary>
    public static void MapCatalogFacetEndpoints(this WebApplication app)
    {
        var categories = app.MapGroup("/v1/admin/catalog/categories/{categoryId:guid}/facets");
        categories.MapGet("/effective", GetEffectiveFacetsAsync);
        categories.MapGet("/local", ListLocalFacetsAsync);
        categories.MapPut("/{definitionId:guid}", UpsertFacetAsync);
        categories.MapDelete("/{definitionId:guid}", RemoveFacetOverrideAsync);
        categories.MapPut("/order", ReorderFacetsAsync);

        app.MapGet("/v1/storefront/categories/{categoryId:guid}/facets", GetStorefrontFacetsAsync);
    }

    private static async Task<IResult> GetEffectiveFacetsAsync(
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
            var locale = request.Query.TryGetValue("locale", out var values) && values.Count > 0
                ? values[0]!
                : "fa-IR";
            return Results.Json(await catalog.GetEffectiveCategoryFacetsAsync(categoryId, locale, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> ListLocalFacetsAsync(
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
            return Results.Json(await catalog.ListLocalFacetConfigurationsAsync(categoryId, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> UpsertFacetAsync(
        Guid categoryId,
        Guid definitionId,
        UpsertCategoryFacetRequest body,
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
            await catalog.UpsertCategoryFacetConfigurationAsync(
                categoryId,
                definitionId,
                new CategoryFacetConfigurationInput(
                    body.DisplayType,
                    body.SortOrder,
                    body.IsVisible,
                    body.IsSearchable,
                    body.IsCollapsedByDefault,
                    body.ShowCounts),
                cancellationToken);
            return Results.NoContent();
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = "catalog.facet.invalid" }, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> RemoveFacetOverrideAsync(
        Guid categoryId,
        Guid definitionId,
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
            await catalog.RemoveCategoryFacetOverrideAsync(categoryId, definitionId, cancellationToken);
            return Results.NoContent();
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = "catalog.facet.missing" }, statusCode: StatusCodes.Status404NotFound);
        }
    }

    private static async Task<IResult> ReorderFacetsAsync(
        Guid categoryId,
        ReorderCategoryFacetsRequest body,
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
            await catalog.ReorderCategoryFacetConfigurationsAsync(
                categoryId,
                body.OrderedDefinitionIds ?? Array.Empty<Guid>(),
                cancellationToken);
            return Results.NoContent();
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = "catalog.facet.invalid" }, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> GetStorefrontFacetsAsync(
        Guid categoryId,
        ICatalogDirectory catalog,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        var locale = request.Query.TryGetValue("locale", out var values) && values.Count > 0
            ? values[0]!
            : "fa-IR";
        try
        {
            return Results.Json(await catalog.GetEffectiveCategoryFacetsAsync(categoryId, locale, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = "catalog.facet.invalid" }, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Message, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
}

internal sealed record UpsertCategoryFacetRequest(
    CatalogFacetDisplayType DisplayType,
    int SortOrder,
    bool IsVisible,
    bool IsSearchable,
    bool IsCollapsedByDefault,
    bool ShowCounts);

internal sealed record ReorderCategoryFacetsRequest(IReadOnlyList<Guid>? OrderedDefinitionIds);
