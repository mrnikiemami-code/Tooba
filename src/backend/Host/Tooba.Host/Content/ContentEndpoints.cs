using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.Content.Application;
using Tooba.Content.Domain;

namespace Tooba.Host.Content;

/// <summary>مرزهای HTTP عمومی و مدیریتی Content.</summary>
public static class ContentEndpoints
{
    /// <summary>مسیرهای Content را ثبت می‌کند.</summary>
    public static void MapContentEndpoints(this WebApplication app)
    {
        app.MapGet("/v1/content/articles", ListPublishedAsync);
        app.MapGet("/v1/content/articles/{slug}", GetPublishedBySlugAsync);
        app.MapGet("/v1/content/categories", ListPublicCategoriesAsync);
        app.MapGet("/v1/content/categories/{slug}", GetPublicCategoryBySlugAsync);
        app.MapGet("/v1/content/authors", ListPublicAuthorsAsync);
        app.MapGet("/v1/content/authors/{slug}", GetPublicAuthorBySlugAsync);

        var admin = app.MapGroup("/v1/admin/content");
        admin.MapGet("/articles", AdminListAsync);
        admin.MapPost("/articles/query", AdminQueryGridAsync);
        admin.MapGet("/articles/{id:guid}", AdminGetAsync);
        admin.MapPost("/articles", AdminCreateAsync);
        admin.MapPut("/articles/{id:guid}", AdminUpdateAsync);
        admin.MapPost("/articles/{id:guid}/publish", AdminPublishAsync);
        admin.MapPost("/articles/{id:guid}/unpublish", AdminUnpublishAsync);
        admin.MapPost("/articles/{id:guid}/archive", AdminArchiveAsync);
        admin.MapDelete("/articles/{id:guid}", AdminDeleteAsync);
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);

    private static async Task<IResult> ListPublishedAsync(
        ContentPanelComposer composer,
        int page = 1,
        int pageSize = 20,
        string? category = null,
        string? locale = null,
        string? categorySlug = null,
        string? authorSlug = null,
        CancellationToken cancellationToken = default) =>
        Results.Json(await composer.ListPublishedAsync(
            page,
            pageSize,
            category,
            locale,
            categorySlug,
            authorSlug,
            cancellationToken));

    private static async Task<IResult> GetPublishedBySlugAsync(
        string slug,
        ContentPanelComposer composer,
        string? locale = null,
        CancellationToken cancellationToken = default)
    {
        var article = await composer.GetPublishedBySlugAsync(slug, locale, cancellationToken);
        return article is null ? Results.NotFound() : Results.Json(article);
    }

    private static async Task<IResult> ListPublicCategoriesAsync(
        ContentPanelComposer composer,
        string? locale = null,
        CancellationToken cancellationToken = default) =>
        Results.Json(await composer.ListPublicCategoriesAsync(locale, cancellationToken));

    private static async Task<IResult> GetPublicCategoryBySlugAsync(
        string slug,
        ContentPanelComposer composer,
        string? locale = null,
        CancellationToken cancellationToken = default)
    {
        var category = await composer.GetPublicCategoryBySlugAsync(locale, slug, cancellationToken);
        return category is null ? Results.NotFound() : Results.Json(category);
    }

    private static async Task<IResult> ListPublicAuthorsAsync(
        ContentPanelComposer composer,
        string? locale = null,
        CancellationToken cancellationToken = default) =>
        Results.Json(await composer.ListPublicAuthorsAsync(locale, cancellationToken));

    private static async Task<IResult> GetPublicAuthorBySlugAsync(
        string slug,
        ContentPanelComposer composer,
        string? locale = null,
        CancellationToken cancellationToken = default)
    {
        var author = await composer.GetPublicAuthorBySlugAsync(slug, locale, cancellationToken);
        return author is null ? Results.NotFound() : Results.Json(author);
    }

    private static async Task<IResult> AdminListAsync(
        ContentPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IAuthorizationService authz,
        IHostEnvironment environment,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await ContentAdminAccess.RequireAsync(
                request, session, tenant, guard, environment, authz, ContentAdminAccess.View, cancellationToken);
            return Results.Json(await composer.ListAllAsync(page, pageSize, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static async Task<IResult> AdminQueryGridAsync(
        GridQueryRequest body,
        ContentPanelComposer composer,
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
            return Results.Json(await composer.QueryGridAsync(body, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static async Task<IResult> AdminGetAsync(
        Guid id,
        ContentPanelComposer composer,
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
            var article = await composer.GetByIdAsync(id, cancellationToken);
            return article is null ? Results.NotFound() : Results.Json(article);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static async Task<IResult> AdminCreateAsync(
        CreateArticleBody body,
        ContentPanelComposer composer,
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
                request, session, tenant, guard, environment, authz, ContentAdminAccess.Create, cancellationToken);
            var created = await composer.CreateAsync(body, cancellationToken);
            return Results.Json(created, statusCode: StatusCodes.Status201Created);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex)
        {
            var conflict = ex.Message.Contains("تکراری", StringComparison.Ordinal);
            return Results.Json(
                new { title = conflict ? "Conflict" : "Bad Request", errorCode = conflict ? "content.slug.duplicate" : "content.create.rejected", detail = ex.Message },
                statusCode: conflict ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> AdminUpdateAsync(
        Guid id,
        UpdateArticleBody body,
        ContentPanelComposer composer,
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
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IAuthorizationService authz,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await AdminLifecycleAsync(
            id, composer, request, session, tenant, guard, authz, environment, cancellationToken,
            ContentAdminAccess.Publish, composer.PublishAsync);

    private static async Task<IResult> AdminUnpublishAsync(
        Guid id,
        ContentPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IAuthorizationService authz,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await AdminLifecycleAsync(
            id, composer, request, session, tenant, guard, authz, environment, cancellationToken,
            ContentAdminAccess.Publish, composer.UnpublishAsync);

    private static async Task<IResult> AdminArchiveAsync(
        Guid id,
        ContentPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IAuthorizationService authz,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await AdminLifecycleAsync(
            id, composer, request, session, tenant, guard, authz, environment, cancellationToken,
            ContentAdminAccess.Edit, composer.ArchiveAsync);

    private static async Task<IResult> AdminDeleteAsync(
        Guid id,
        ContentPanelComposer composer,
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
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IAuthorizationService authz,
        IHostEnvironment environment,
        CancellationToken cancellationToken,
        string permissionId,
        Func<Guid, CancellationToken, Task<AdminArticleSnapshot>> action)
    {
        try
        {
            await ContentAdminAccess.RequireAsync(
                request, session, tenant, guard, environment, authz, permissionId, cancellationToken);
            return Results.Json(await action(id, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex)
        {
            var missing = ex.Message.Contains("یافت نشد", StringComparison.Ordinal);
            if (missing)
                return Results.Json(new { title = "Not Found", errorCode = "content.article.missing" }, statusCode: StatusCodes.Status404NotFound);
            if (ex.Message.Contains(ContentArticleErrorCodes.AlreadyArchived, StringComparison.Ordinal)
                || ex.Message.Contains(ContentArticleErrorCodes.ArchiveNotAllowed, StringComparison.Ordinal))
            {
                return Results.Json(
                    new { title = "Bad Request", errorCode = ContentArticleErrorCodes.ArchiveNotAllowed, detail = ex.Message },
                    statusCode: StatusCodes.Status400BadRequest);
            }
            return Results.Json(new { title = "Not Found", errorCode = "content.article.missing" }, statusCode: StatusCodes.Status404NotFound);
        }
    }

    /// <summary>
    /// ارتقای کد دامنه از Message استثناء به errorCode پایدار — نه فقط content.update.rejected.
    /// </summary>
    private static string ResolveArticleUpdateErrorCode(string message, bool missing)
    {
        if (missing) return "content.article.missing";
        if (string.IsNullOrWhiteSpace(message)) return "content.update.rejected";

        string[] known =
        [
            ContentArticleErrorCodes.LocaleLocked,
            ContentArticleErrorCodes.AlreadyArchived,
            ContentArticleErrorCodes.ArchiveNotAllowed,
            ContentArticleErrorCodes.UnsafeBodyMedia,
            ContentArticleErrorCodes.MediaNotFound,
            ContentCategoryErrorCodes.LanguageMismatch,
            ContentCategoryErrorCodes.NotFound,
            ContentCategoryErrorCodes.InvalidLanguage,
            ContentAuthorErrorCodes.Inactive,
            ContentAuthorErrorCodes.NotFound,
            ContentAuthorErrorCodes.RequiredForPublish,
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
