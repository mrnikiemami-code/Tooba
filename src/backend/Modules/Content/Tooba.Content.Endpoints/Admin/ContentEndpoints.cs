using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Security;
using Tooba.BuildingBlocks.Grid;
using Tooba.Content.Application;
using Tooba.Content.Domain;

namespace Tooba.Content.Endpoints.Admin;

/// <summary>مرزهای HTTP مدیریتی مقالات Content.</summary>
public static class ContentEndpoints
{
    /// <summary>مسیرهای Admin مقالات Content را ثبت می‌کند.</summary>
    public static void MapContentEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/v1/admin/content");
        admin.MapGet("/articles", AdminListAsync);
        admin.MapPost("/articles/query", AdminQueryGridAsync);
        admin.MapGet("/articles/{id:guid}", AdminGetAsync);
        admin.MapGet("/articles/{id:guid}/publish/readiness", AdminGetPublishReadinessAsync);
        admin.MapGet("/articles/{id:guid}/preview", AdminGetPreviewAsync);
        admin.MapGet("/articles/{id:guid}/history", AdminListHistoryAsync);
        admin.MapPost("/articles", AdminCreateAsync);
        admin.MapPut("/articles/{id:guid}", AdminUpdateAsync);
        admin.MapPost("/articles/{id:guid}/publish", AdminPublishAsync);
        admin.MapPost("/articles/{id:guid}/unpublish", AdminUnpublishAsync);
        admin.MapPost("/articles/{id:guid}/archive", AdminArchiveAsync);
        admin.MapDelete("/articles/{id:guid}", AdminDeleteAsync);
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);

    private static async Task<IResult> AdminListAsync(
        ContentPanelComposer composer,
        HttpRequest request,
        IAdminPanelAccess adminPanelAccess,
        ICurrentTenant tenant,
        IAuthorizationService authz,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await ContentAdminAccess.RequireAsync(
                request, adminPanelAccess, tenant, authz, ContentAdminAccess.View, cancellationToken);
            return Results.Json(await composer.ListAllAsync(page, pageSize, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static async Task<IResult> AdminQueryGridAsync(
        GridQueryRequest body,
        ContentPanelComposer composer,
        HttpRequest request,
        IAdminPanelAccess adminPanelAccess,
        ICurrentTenant tenant,
        IAuthorizationService authz,
        CancellationToken cancellationToken)
    {
        try
        {
            await ContentAdminAccess.RequireAsync(
                request, adminPanelAccess, tenant, authz, ContentAdminAccess.View, cancellationToken);
            return Results.Json(await composer.QueryGridAsync(body, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static async Task<IResult> AdminGetAsync(
        Guid id,
        ContentPanelComposer composer,
        HttpRequest request,
        IAdminPanelAccess adminPanelAccess,
        ICurrentTenant tenant,
        IAuthorizationService authz,
        CancellationToken cancellationToken)
    {
        try
        {
            await ContentAdminAccess.RequireAsync(
                request, adminPanelAccess, tenant, authz, ContentAdminAccess.View, cancellationToken);
            var article = await composer.GetByIdAsync(id, cancellationToken);
            return article is null ? Results.NotFound() : Results.Json(article);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static async Task<IResult> AdminGetPublishReadinessAsync(
        Guid id,
        ContentPanelComposer composer,
        HttpRequest request,
        IAdminPanelAccess adminPanelAccess,
        ICurrentTenant tenant,
        IAuthorizationService authz,
        CancellationToken cancellationToken)
    {
        try
        {
            await ContentAdminAccess.RequireAsync(
                request, adminPanelAccess, tenant, authz, ContentAdminAccess.View, cancellationToken);
            return Results.Json(await composer.GetPublishReadinessAsync(id, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex)
        {
            var missing = ex.Message.Contains("یافت نشد", StringComparison.Ordinal);
            return Results.Json(
                new
                {
                    title = missing ? "Not Found" : "Bad Request",
                    errorCode = missing ? "content.article.missing" : ResolveArticleUpdateErrorCode(ex.Message, false),
                    detail = ex.Message,
                },
                statusCode: missing ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> AdminGetPreviewAsync(
        Guid id,
        ContentPanelComposer composer,
        HttpRequest request,
        IAdminPanelAccess adminPanelAccess,
        ICurrentTenant tenant,
        IAuthorizationService authz,
        CancellationToken cancellationToken)
    {
        try
        {
            await ContentAdminAccess.RequireAsync(
                request, adminPanelAccess, tenant, authz, ContentAdminAccess.View, cancellationToken);
            var preview = await composer.GetPreviewAsync(id, cancellationToken);
            if (preview is null)
            {
                return Results.Json(
                    new { title = "Not Found", errorCode = ArticlePublicationCodes.PreviewUnavailable },
                    statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Json(preview);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static async Task<IResult> AdminListHistoryAsync(
        Guid id,
        ContentPanelComposer composer,
        HttpRequest request,
        IAdminPanelAccess adminPanelAccess,
        ICurrentTenant tenant,
        IAuthorizationService authz,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await ContentAdminAccess.RequireAsync(
                request, adminPanelAccess, tenant, authz, ContentAdminAccess.View, cancellationToken);
            return Results.Json(await composer.ListHistoryAsync(id, skip, take, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex)
        {
            var missing = ex.Message.Contains("یافت نشد", StringComparison.Ordinal);
            return Results.Json(
                new { title = missing ? "Not Found" : "Bad Request", errorCode = "content.article.missing" },
                statusCode: missing ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> AdminCreateAsync(
        CreateArticleBody body,
        ContentPanelComposer composer,
        HttpRequest request,
        IAdminPanelAccess adminPanelAccess,
        ICurrentTenant tenant,
        IAuthorizationService authz,
        CancellationToken cancellationToken)
    {
        try
        {
            await ContentAdminAccess.RequireAsync(
                request, adminPanelAccess, tenant, authz, ContentAdminAccess.Create, cancellationToken);
            var created = await composer.CreateAsync(body, cancellationToken);
            return Results.Json(created, statusCode: StatusCodes.Status201Created);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex)
        {
            var conflict = ex.Message.Contains("تکراری", StringComparison.Ordinal);
            var errorCode = conflict
                ? "content.slug.duplicate"
                : ResolveArticleUpdateErrorCode(ex.Message, missing: false);
            if (errorCode == "content.update.rejected")
            {
                errorCode = "content.create.rejected";
            }

            return Results.Json(
                new { title = conflict ? "Conflict" : "Bad Request", errorCode, detail = ex.Message },
                statusCode: conflict ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> AdminUpdateAsync(
        Guid id,
        UpdateArticleBody body,
        ContentPanelComposer composer,
        HttpRequest request,
        IAdminPanelAccess adminPanelAccess,
        ICurrentTenant tenant,
        IAuthorizationService authz,
        CancellationToken cancellationToken)
    {
        try
        {
            await ContentAdminAccess.RequireAsync(
                request, adminPanelAccess, tenant, authz, ContentAdminAccess.Edit, cancellationToken);
            return Results.Json(await composer.UpdateAsync(id, body, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex)
        {
            var missing = ex.Message.Contains("یافت نشد", StringComparison.Ordinal);
            var errorCode = ResolveArticleUpdateErrorCode(ex.Message, missing);
            return Results.Json(
                new
                {
                    title = missing ? "Not Found" : "Bad Request",
                    errorCode,
                    detail = ex.Message,
                },
                statusCode: missing ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> AdminPublishAsync(
        Guid id,
        ContentPanelComposer composer,
        HttpRequest request,
        IAdminPanelAccess adminPanelAccess,
        ICurrentTenant tenant,
        IAuthorizationService authz,
        CancellationToken cancellationToken) =>
        await AdminLifecycleAsync(
            id, composer, request, adminPanelAccess, tenant, authz, cancellationToken,
            ContentAdminAccess.Publish, composer.PublishAsync);

    private static async Task<IResult> AdminUnpublishAsync(
        Guid id,
        ContentPanelComposer composer,
        HttpRequest request,
        IAdminPanelAccess adminPanelAccess,
        ICurrentTenant tenant,
        IAuthorizationService authz,
        CancellationToken cancellationToken) =>
        await AdminLifecycleAsync(
            id, composer, request, adminPanelAccess, tenant, authz, cancellationToken,
            ContentAdminAccess.Publish, composer.UnpublishAsync);

    private static async Task<IResult> AdminArchiveAsync(
        Guid id,
        ContentPanelComposer composer,
        HttpRequest request,
        IAdminPanelAccess adminPanelAccess,
        ICurrentTenant tenant,
        IAuthorizationService authz,
        CancellationToken cancellationToken) =>
        await AdminLifecycleAsync(
            id, composer, request, adminPanelAccess, tenant, authz, cancellationToken,
            ContentAdminAccess.Edit, composer.ArchiveAsync);

    private static async Task<IResult> AdminDeleteAsync(
        Guid id,
        ContentPanelComposer composer,
        HttpRequest request,
        IAdminPanelAccess adminPanelAccess,
        ICurrentTenant tenant,
        IAuthorizationService authz,
        CancellationToken cancellationToken)
    {
        try
        {
            await ContentAdminAccess.RequireAsync(
                request, adminPanelAccess, tenant, authz, ContentAdminAccess.Edit, cancellationToken);
            await composer.DeleteDraftAsync(id, cancellationToken);
            return Results.NoContent();
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex)
        {
            var notFound = ex.Message.Contains("یافت نشد", StringComparison.Ordinal);
            var notAllowed = ex.Message.Contains(ContentArticleErrorCodes.DeleteNotAllowed, StringComparison.Ordinal);
            return Results.Json(
                new
                {
                    title = notFound ? "Not Found" : "Bad Request",
                    errorCode = notFound
                        ? "content.article.missing"
                        : notAllowed
                            ? ContentArticleErrorCodes.DeleteNotAllowed
                            : "content.delete.rejected",
                    detail = ex.Message,
                },
                statusCode: notFound ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> AdminLifecycleAsync(
        Guid id,
        ContentPanelComposer composer,
        HttpRequest request,
        IAdminPanelAccess adminPanelAccess,
        ICurrentTenant tenant,
        IAuthorizationService authz,
        CancellationToken cancellationToken,
        string permissionId,
        Func<Guid, CancellationToken, Task<AdminArticleSnapshot>> action)
    {
        try
        {
            await ContentAdminAccess.RequireAsync(
                request, adminPanelAccess, tenant, authz, permissionId, cancellationToken);
            return Results.Json(await action(id, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex)
        {
            var missing = ex.Message.Contains("یافت نشد", StringComparison.Ordinal);
            if (missing)
                return Results.Json(new { title = "Not Found", errorCode = "content.article.missing" }, statusCode: StatusCodes.Status404NotFound);

            var errorCode = ResolveArticleUpdateErrorCode(ex.Message, false);
            return Results.Json(
                new { title = "Bad Request", errorCode, detail = ex.Message },
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>
    /// ارتقای کد دامنه از Message استثناء به errorCode پایدار — نه فقط content.update.rejected.
    /// </summary>
    private static string ResolveArticleUpdateErrorCode(string message, bool missing)
    {
        if (missing) return "content.article.missing";
        if (string.IsNullOrWhiteSpace(message)) return "content.update.rejected";

        if (message.StartsWith(ArticlePublicationCodes.NotReady, StringComparison.Ordinal))
            return ArticlePublicationCodes.NotReady;

        string[] known =
        [
            ContentArticleErrorCodes.LocaleLocked,
            ContentArticleErrorCodes.AlreadyArchived,
            ContentArticleErrorCodes.ArchiveNotAllowed,
            ContentArticleErrorCodes.UnsafeBodyMedia,
            ContentArticleErrorCodes.MediaNotFound,
            ContentArticleErrorCodes.DeleteNotAllowed,
            ContentCategoryErrorCodes.LanguageMismatch,
            ContentCategoryErrorCodes.NotFound,
            ContentCategoryErrorCodes.InvalidLanguage,
            ContentAuthorErrorCodes.Inactive,
            ContentAuthorErrorCodes.NotFound,
            ContentAuthorErrorCodes.RequiredForPublish,
            ArticlePublicationCodes.NotReady,
            ArticlePublicationCodes.InvalidSchedule,
            ArticlePublicationCodes.PublishForbidden,
            ArticlePublicationCodes.UnpublishInvalid,
            ArticlePublicationCodes.PreviewUnavailable,
            "localization.language.inactive",
            "localization.language.not_found",
        ];

        foreach (var code in known)
        {
            if (message.Contains(code, StringComparison.Ordinal))
                return code;
        }

        // Message خود ممکن است دقیقاً کد نقطه‌دار باشد.
        if (message.Contains('.', StringComparison.Ordinal)
            && !message.Contains(' ', StringComparison.Ordinal)
            && message.Length < 120)
        {
            return message.Trim();
        }

        return "content.update.rejected";
    }
}

/// <summary>بدنهٔ ایجاد مقاله از مرز admin.</summary>
public sealed record CreateArticleBody(
    string Slug,
    string Title,
    string Excerpt,
    string Body,
    Guid? CoverMediaAssetId,
    Guid? AuthorId,
    IReadOnlyList<string>? Tags,
    bool IsFeatured,
    DateTimeOffset? PublishDate,
    string? Locale,
    string? SeoTitle,
    string? SeoDescription,
    string? Category,
    Guid? CategoryId);

/// <summary>بدنهٔ به‌روزرسانی مقاله از مرز admin.</summary>
public sealed record UpdateArticleBody(
    string Title,
    string Excerpt,
    string Body,
    Guid? CoverMediaAssetId,
    Guid? AuthorId,
    IReadOnlyList<string>? Tags,
    bool IsFeatured,
    string? Locale,
    string? SeoTitle,
    string? SeoDescription,
    string? Category,
    Guid? CategoryId,
    DateTimeOffset? PublishDate);
