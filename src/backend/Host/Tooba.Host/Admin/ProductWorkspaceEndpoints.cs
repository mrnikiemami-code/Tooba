using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.Host.Grid;
using Tooba.ProductWorkspace.Application.Composition.Models;

namespace Tooba.Host.Admin;

/// <summary>
/// مسیرهای HTTP ترکیب Workspace محصول. SQL بین‌ماژولی اینجا نوشته نمی‌شود.
/// W19: aggregate GET evacuated to ProductWorkspace.Endpoints.
/// W26: lifecycle POSTs evacuated to ProductWorkspace.Endpoints.
/// W27: variant create/patch evacuated to ProductWorkspace.Endpoints.
/// W28: product DELETE evacuated to Catalog.Endpoints.
/// W29: create / catalog-title / core / quantity-policy evacuated to ProductWorkspace.Endpoints.
/// Host ProductWorkspace surface after brand options Catalog ownership.
/// </summary>
public static class ProductWorkspaceEndpoints
{
    /// <summary>
    /// مسیرهای Admin Product Workspace را ثبت می‌کند.
    /// </summary>
    public static void MapProductWorkspaceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/admin/products");
        group.AddEndpointFilter(CatalogActorHttpBinding.BindAsync);
        group.MapGet("/", ListAsync);
        group.MapPost("/query", QueryGridAsync);
        group.MapPut("/{productId:guid}/category", AssignCategoryAsync);
        group.MapPost("/{productId:guid}/categories/additional", AddAdditionalCategoryAsync);
        group.MapDelete("/{productId:guid}/categories/additional/{categoryId:guid}", RemoveAdditionalCategoryAsync);
        group.MapPut("/{productId:guid}/brand", AssignBrandAsync);
    }

    private static ProductWorkspacePermissions ReadPermissions(HttpRequest request)
    {
        var scope = request.Headers["X-Tooba-Workspace-Scope"].ToString();
        if (string.Equals(scope, "view", StringComparison.OrdinalIgnoreCase))
        {
            return new ProductWorkspacePermissions(true, false, false, false, false);
        }

        return new ProductWorkspacePermissions(true, true, true, true, true);
    }

    private static async Task<IResult> QueryGridAsync(
        GridQueryRequest body,
        ProductWorkspaceComposer composer,
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
            var normalized = AdminProductGridQueryPolicy.Normalize(body);
            return Results.Json(await composer.QueryGridAsync(normalized, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> ListAsync(
        ProductWorkspaceComposer composer,
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
            return Results.Json(await composer.ListAsync(cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> AssignCategoryAsync(
        Guid productId,
        AdminProductCategoryAssignRequest body,
        ProductWorkspaceComposer composer,
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
            return Results.Json(await composer.AssignProductCategoryAsync(productId, body, ReadPermissions(request), cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> AddAdditionalCategoryAsync(
        Guid productId,
        AdminProductAdditionalCategoryRequest body,
        ProductWorkspaceComposer composer,
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
            return Results.Json(await composer.AddAdditionalCategoryAsync(productId, body, ReadPermissions(request), cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> RemoveAdditionalCategoryAsync(
        Guid productId,
        Guid categoryId,
        DateTimeOffset? expectedUpdatedAt,
        ProductWorkspaceComposer composer,
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
            if (expectedUpdatedAt is null)
            {
                return Results.Json(
                    new { title = "expectedUpdatedAt لازم است.", errorCode = "catalog.category.assignment.stale" },
                    statusCode: StatusCodes.Status400BadRequest);
            }

            return Results.Json(await composer.RemoveAdditionalCategoryAsync(
                productId, categoryId, expectedUpdatedAt.Value, ReadPermissions(request), cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> AssignBrandAsync(
        Guid productId,
        AdminProductBrandAssignRequest body,
        ProductWorkspaceComposer composer,
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
            return Results.Json(await composer.AssignProductBrandAsync(productId, body, ReadPermissions(request), cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
}
