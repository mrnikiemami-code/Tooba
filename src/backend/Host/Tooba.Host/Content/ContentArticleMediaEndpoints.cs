using Tooba.BuildingBlocks;
using Tooba.Content.Application;
using Tooba.Content.Domain;

namespace Tooba.Host.Content;

/// <summary>مسیرهای Admin رسانهٔ مقاله.</summary>
public static class ContentArticleMediaEndpoints
{
    /// <summary>مسیرهای رسانهٔ مقاله را ثبت می‌کند.</summary>
    public static void MapContentArticleMediaEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/v1/admin/content/articles/{articleId:guid}/media");
        admin.MapGet("/", GetWorkspaceAsync);
        admin.MapPut("/featured", AssignFeaturedAsync);
        admin.MapPut("/seo-image", AssignSeoImageAsync);
        admin.MapPost("/gallery", AddGalleryAsync);
        admin.MapDelete("/gallery/{mediaAssetId:guid}", RemoveGalleryAsync);
        admin.MapPut("/gallery/reorder", ReorderGalleryAsync);
        admin.MapPatch("/gallery/{mediaAssetId:guid}", PatchGalleryAsync);
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);

    private static IResult MapInvalid(InvalidOperationException ex)
    {
        var missing = ex.Message.Contains("یافت نشد", StringComparison.Ordinal);
        string[] known =
        [
            ContentArticleErrorCodes.MediaNotFound,
            ContentArticleErrorCodes.UnsafeBodyMedia,
            "content.article.missing",
        ];
        var errorCode = "content.article.media.rejected";
        if (missing) errorCode = "content.article.missing";
        else
        {
            foreach (var code in known)
            {
                if (ex.Message.Contains(code, StringComparison.Ordinal))
                {
                    errorCode = code;
                    break;
                }
            }
        }

        return Results.Json(
            new
            {
                title = missing ? "Not Found" : "Bad Request",
                errorCode,
                detail = ex.Message,
            },
            statusCode: missing ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetWorkspaceAsync(
        Guid articleId,
        ContentArticleMediaPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IAuthorizationService authz,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await ContentAdminAccess.RequireAsync(
                request, session, tenant, guard, environment, authz, ContentAdminAccess.View, cancellationToken);
            return Results.Json(await composer.GetWorkspaceAsync(articleId, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }

    private static async Task<IResult> AssignFeaturedAsync(
        Guid articleId,
        AssignArticleFeaturedBody body,
        ContentArticleMediaPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IAuthorizationService authz,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await ContentAdminAccess.RequireAsync(
                request, session, tenant, guard, environment, authz, ContentAdminAccess.Edit, cancellationToken);
            return Results.Json(await composer.AssignFeaturedAsync(articleId, body.MediaAssetId, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }

    private static async Task<IResult> AssignSeoImageAsync(
        Guid articleId,
        AssignArticleSeoImageBody body,
        ContentArticleMediaPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IAuthorizationService authz,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await ContentAdminAccess.RequireAsync(
                request, session, tenant, guard, environment, authz, ContentAdminAccess.Edit, cancellationToken);
            return Results.Json(await composer.AssignSeoImageAsync(articleId, body.MediaAssetId, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }

    private static async Task<IResult> AddGalleryAsync(
        Guid articleId,
        AddArticleGalleryBody body,
        ContentArticleMediaPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IAuthorizationService authz,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await ContentAdminAccess.RequireAsync(
                request, session, tenant, guard, environment, authz, ContentAdminAccess.Edit, cancellationToken);
            return Results.Json(await composer.AddGalleryAsync(articleId, body.MediaAssetIds ?? [], cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }

    private static async Task<IResult> RemoveGalleryAsync(
        Guid articleId,
        Guid mediaAssetId,
        ContentArticleMediaPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IAuthorizationService authz,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await ContentAdminAccess.RequireAsync(
                request, session, tenant, guard, environment, authz, ContentAdminAccess.Edit, cancellationToken);
            return Results.Json(await composer.RemoveGalleryAsync(articleId, mediaAssetId, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }

    private static async Task<IResult> ReorderGalleryAsync(
        Guid articleId,
        ReorderArticleGalleryBody body,
        ContentArticleMediaPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IAuthorizationService authz,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await ContentAdminAccess.RequireAsync(
                request, session, tenant, guard, environment, authz, ContentAdminAccess.Edit, cancellationToken);
            return Results.Json(await composer.ReorderGalleryAsync(articleId, body.OrderedMediaAssetIds ?? [], cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }

    private static async Task<IResult> PatchGalleryAsync(
        Guid articleId,
        Guid mediaAssetId,
        PatchArticleGalleryBody body,
        ContentArticleMediaPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IAuthorizationService authz,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await ContentAdminAccess.RequireAsync(
                request, session, tenant, guard, environment, authz, ContentAdminAccess.Edit, cancellationToken);
            return Results.Json(await composer.PatchGalleryAsync(articleId, mediaAssetId, body.AltText, body.Caption, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }
}

/// <summary>بدنهٔ تنظیم تصویر شاخص.</summary>
public sealed record AssignArticleFeaturedBody(Guid? MediaAssetId);

/// <summary>بدنهٔ تنظیم تصویر SEO.</summary>
public sealed record AssignArticleSeoImageBody(Guid? MediaAssetId);

/// <summary>بدنهٔ افزودن به گالری.</summary>
public sealed record AddArticleGalleryBody(IReadOnlyList<Guid>? MediaAssetIds);

/// <summary>بدنهٔ مرتب‌سازی گالری.</summary>
public sealed record ReorderArticleGalleryBody(IReadOnlyList<Guid>? OrderedMediaAssetIds);

/// <summary>بدنهٔ به‌روزرسانی متادیتای گالری.</summary>
public sealed record PatchArticleGalleryBody(string? AltText, string? Caption);
