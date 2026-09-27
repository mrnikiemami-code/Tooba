using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.Host.Grid;
using Tooba.ProductWorkspace.Application.Composition.Models;

namespace Tooba.Host.Admin;

/// <summary>
/// مسیرهای HTTP ترکیب Workspace محصول. SQL بین‌ماژولی اینجا نوشته نمی‌شود.
/// W19: aggregate GET evacuated to ProductWorkspace.Endpoints.
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
        group.MapPost("/", CreateAsync);
        group.MapPatch("/{productId:guid}/catalog-title", PatchTitleAsync);
        group.MapPatch("/{productId:guid}/core", PatchCoreAsync);
        group.MapPatch("/{productId:guid}/quantity-policy", PatchQuantityPolicyAsync);
        group.MapPut("/{productId:guid}/category", AssignCategoryAsync);
        group.MapPost("/{productId:guid}/categories/additional", AddAdditionalCategoryAsync);
        group.MapDelete("/{productId:guid}/categories/additional/{categoryId:guid}", RemoveAdditionalCategoryAsync);
        group.MapPut("/{productId:guid}/brand", AssignBrandAsync);
        group.MapPost("/{productId:guid}/publish", PublishAsync);
        group.MapPost("/{productId:guid}/unpublish", UnpublishAsync);
        group.MapPost("/{productId:guid}/archive", ArchiveAsync);
        group.MapPost("/{productId:guid}/restore", RestoreAsync);
        group.MapDelete("/{productId:guid}", DeleteAsync);

        group.MapPost("/{productId:guid}/variants", CreateVariantAsync);
        group.MapPatch("/{productId:guid}/variants/{variantId:guid}", PatchVariantAsync);
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

    private static async Task<IResult> CreateAsync(
        AdminProductCreateRequest body,
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
            var workspace = await composer.CreateSimpleProductAsync(body, ReadPermissions(request), cancellationToken);
            return Results.Json(workspace, statusCode: StatusCodes.Status201Created);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> PatchTitleAsync(
        Guid productId,
        CatalogTitlePatch body,
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
            var workspace = await composer.UpdateCatalogTitleAsync(
                productId,
                body.Locale,
                body.Title,
                body.ExpectedUpdatedAt,
                ReadPermissions(request),
                cancellationToken);
            return Results.Json(workspace);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> PatchCoreAsync(
        Guid productId,
        AdminProductCoreUpdateRequest body,
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
            return Results.Json(await composer.UpdateProductCoreAsync(productId, body, ReadPermissions(request), cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> PatchQuantityPolicyAsync(
        Guid productId,
        AdminProductQuantityPolicyRequest body,
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
            return Results.Json(await composer.UpdateQuantityPolicyAsync(productId, body, ReadPermissions(request), cancellationToken));
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

    private static async Task<IResult> PublishAsync(
        Guid productId,
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
            return Results.Json(await composer.PublishAsync(productId, ReadPermissions(request), cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> UnpublishAsync(
        Guid productId,
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
            return Results.Json(await composer.UnpublishAsync(productId, ReadPermissions(request), cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> ArchiveAsync(
        Guid productId,
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
            return Results.Json(await composer.ArchiveAsync(productId, ReadPermissions(request), cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> RestoreAsync(
        Guid productId,
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
            return Results.Json(await composer.RestoreAsync(productId, ReadPermissions(request), cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> DeleteAsync(
        Guid productId,
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
            await composer.DeleteOrSoftArchiveAsync(productId, ReadPermissions(request), cancellationToken);
            return Results.NoContent();
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }


    private static async Task<IResult> CreateVariantAsync(
        Guid productId,
        AdminProductVariantCreateRequest body,
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
            var workspace = await composer.CreateVariantAsync(
                productId,
                body,
                ReadPermissions(request),
                cancellationToken);
            return Results.Json(workspace, statusCode: StatusCodes.Status201Created);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> PatchVariantAsync(
        Guid productId,
        Guid variantId,
        AdminProductVariantPatchRequest body,
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
            return Results.Json(await composer.PatchVariantAsync(
                productId,
                variantId,
                body,
                ReadPermissions(request),
                cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
}

/// <summary>
/// بدنهٔ به‌روزرسانی عنوان Catalog با قفل خوش‌بینانه.
/// </summary>
public sealed record CatalogTitlePatch(string Locale, string Title, DateTimeOffset ExpectedUpdatedAt);
