using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.Host.Grid;

namespace Tooba.Host.Admin;

/// <summary>
/// مسیرهای HTTP ترکیب Workspace محصول. SQL بین‌ماژولی اینجا نوشته نمی‌شود.
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
        group.MapGet("/brand-options", ListBrandOptionsAsync);
        group.MapPost("/query", QueryGridAsync);
        group.MapPost("/", CreateAsync);
        group.MapGet("/{productId:guid}", GetAsync);
        group.MapGet("/{productId:guid}/history", GetHistoryAsync);
        group.MapPatch("/{productId:guid}/catalog-title", PatchTitleAsync);
        group.MapPatch("/{productId:guid}/core", PatchCoreAsync);
        group.MapPut("/{productId:guid}/category", AssignCategoryAsync);
        group.MapPut("/{productId:guid}/brand", AssignBrandAsync);
        group.MapPost("/{productId:guid}/publish", PublishAsync);
        group.MapPost("/{productId:guid}/unpublish", UnpublishAsync);
        group.MapPost("/{productId:guid}/archive", ArchiveAsync);
        group.MapPost("/{productId:guid}/restore", RestoreAsync);
        group.MapGet("/{productId:guid}/publish/readiness", GetPublishReadinessAsync);
        group.MapDelete("/{productId:guid}", DeleteAsync);

        group.MapGet("/{productId:guid}/media", ListMediaAsync);
        group.MapGet("/{productId:guid}/media/readiness", GetMediaReadinessAsync);
        group.MapPost("/{productId:guid}/media", AttachMediaAsync);
        group.MapPost("/{productId:guid}/media/placeholder", AttachPlaceholderMediaAsync);
        group.MapPut("/{productId:guid}/media/order", ReorderMediaAsync);
        group.MapPut("/{productId:guid}/media/{assetId:guid}/primary", SetPrimaryMediaAsync);
        group.MapPatch("/{productId:guid}/media/{assetId:guid}", PatchMediaAsync);
        group.MapDelete("/{productId:guid}/media/{assetId:guid}", DetachMediaAsync);

        group.MapGet("/{productId:guid}/seo", GetSeoAsync);
        group.MapPut("/{productId:guid}/seo", PutSeoAsync);
        group.MapGet("/{productId:guid}/seo/readiness", GetSeoReadinessAsync);

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

    private static async Task<IResult> GetAsync(
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
            var workspace = await composer.GetAsync(productId, ReadPermissions(request), cancellationToken);
            return workspace is null
                ? Results.Json(new { title = "Not Found", errorCode = "workspace.product.missing" }, statusCode: StatusCodes.Status404NotFound)
                : Results.Json(workspace);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> GetHistoryAsync(
        Guid productId,
        string? section,
        int? skip,
        int? take,
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
            return Results.Json(await composer.GetHistoryPageAsync(
                productId,
                section,
                skip ?? 0,
                take ?? 50,
                ReadPermissions(request),
                cancellationToken));
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

    private static async Task<IResult> ListBrandOptionsAsync(
        string? q,
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
            return Results.Json(await composer.ListBrandOptionsAsync(q, cancellationToken));
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

    private static async Task<IResult> GetPublishReadinessAsync(
        Guid productId,
        string? locale,
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
            return Results.Json(await composer.GetPublishReadinessAsync(
                productId,
                locale,
                ReadPermissions(request),
                cancellationToken));
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

    private static async Task<IResult> ListMediaAsync(
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
            return Results.Json(await composer.ListMediaAsync(productId, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> GetMediaReadinessAsync(
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
            return Results.Json(await composer.GetMediaReadinessAsync(productId, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> AttachMediaAsync(
        Guid productId,
        AdminProductMediaAttachRequest body,
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
            var media = await composer.AttachMediaAsync(productId, body, ReadPermissions(request), cancellationToken);
            return Results.Json(media, statusCode: StatusCodes.Status201Created);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> AttachPlaceholderMediaAsync(
        Guid productId,
        AdminProductMediaPlaceholderRequest? body,
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
            var media = await composer.AttachPlaceholderMediaAsync(
                productId,
                body?.AltText,
                ReadPermissions(request),
                cancellationToken);
            return Results.Json(media, statusCode: StatusCodes.Status201Created);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> ReorderMediaAsync(
        Guid productId,
        AdminProductMediaOrderRequest body,
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
            return Results.Json(await composer.ReorderMediaAsync(
                productId,
                body.OrderedMediaAssetIds ?? [],
                ReadPermissions(request),
                cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> SetPrimaryMediaAsync(
        Guid productId,
        Guid assetId,
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
            return Results.Json(await composer.SetPrimaryMediaAsync(
                productId,
                assetId,
                ReadPermissions(request),
                cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> PatchMediaAsync(
        Guid productId,
        Guid assetId,
        AdminProductMediaPatchRequest body,
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
            return Results.Json(await composer.PatchMediaAltAsync(
                productId,
                assetId,
                body.AltText,
                ReadPermissions(request),
                cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> DetachMediaAsync(
        Guid productId,
        Guid assetId,
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
            return Results.Json(await composer.DetachMediaAsync(
                productId,
                assetId,
                ReadPermissions(request),
                cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> GetSeoAsync(
        Guid productId,
        string? locale,
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
            return Results.Json(await composer.GetSeoAsync(
                productId,
                locale,
                ReadPermissions(request),
                cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> PutSeoAsync(
        Guid productId,
        AdminProductSeoUpdateRequest body,
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
            return Results.Json(await composer.UpdateSeoAsync(
                productId,
                body,
                ReadPermissions(request),
                cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> GetSeoReadinessAsync(
        Guid productId,
        string? locale,
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
            return Results.Json(await composer.GetSeoReadinessAsync(
                productId,
                locale,
                ReadPermissions(request),
                cancellationToken));
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
