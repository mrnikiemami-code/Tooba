using Tooba.BuildingBlocks;
using Tooba.Content.Application;
using Tooba.Content.Domain;

namespace Tooba.Host.Content;

/// <summary>مسیرهای Admin تعدیل نظرات مقاله.</summary>
public static class ContentArticleCommentEndpoints
{
    /// <summary>مسیرهای نظرات مقاله را ثبت می‌کند.</summary>
    public static void MapContentArticleCommentEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/v1/admin/content/articles/{articleId:guid}/comments");
        admin.MapGet("/", ListAsync);
        admin.MapPost("/", CreateAsync);
        admin.MapPost("/{commentId:guid}/approve", ApproveAsync);
        admin.MapPost("/{commentId:guid}/reject", RejectAsync);
        admin.MapPost("/{commentId:guid}/hide", HideAsync);
        admin.MapPost("/{commentId:guid}/pending", MarkPendingAsync);
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);

    private static IResult MapInvalid(InvalidOperationException ex)
    {
        var message = ex.Message ?? "";
        string[] known =
        [
            ArticleCommentCodes.NotFound,
            ArticleCommentCodes.ArticleNotFound,
            ArticleCommentCodes.InvalidTransition,
            ArticleCommentCodes.InvalidPayload,
            ArticleCommentCodes.Forbidden,
        ];

        var errorCode = "content.comment.rejected";
        foreach (var code in known)
        {
            if (message.Contains(code, StringComparison.Ordinal))
            {
                errorCode = code;
                break;
            }
        }

        var status = errorCode is ArticleCommentCodes.NotFound or ArticleCommentCodes.ArticleNotFound
            ? StatusCodes.Status404NotFound
            : errorCode is ArticleCommentCodes.Forbidden
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status400BadRequest;

        return Results.Json(new { title = "Request rejected", errorCode }, statusCode: status);
    }

    private static async Task<IResult> ListAsync(
        Guid articleId,
        IArticleCommentDirectory directory,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IAuthorizationService authz,
        IHostEnvironment environment,
        string? status = null,
        string? search = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await ContentAdminAccess.RequireAsync(
                request, session, tenant, guard, environment, authz, ContentAdminAccess.View, cancellationToken);
            ArticleCommentStatus? parsed = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<ArticleCommentStatus>(status, ignoreCase: true, out var value))
                    return Results.Json(new { title = "Request rejected", errorCode = ArticleCommentCodes.InvalidPayload }, statusCode: 400);
                parsed = value;
            }

            return Results.Json(await directory.ListForArticleAsync(articleId, parsed, search, skip, take, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }

    private static async Task<IResult> CreateAsync(
        Guid articleId,
        CreateArticleCommentBody body,
        IArticleCommentDirectory directory,
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
            var created = await directory.CreateAsync(
                articleId,
                new CreateArticleCommentCommand(body.DisplayName, body.Body, body.AuthorPartyId),
                cancellationToken);
            return Results.Json(created);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }

    private static Task<IResult> ApproveAsync(
        Guid articleId,
        Guid commentId,
        ModerateArticleCommentBody? body,
        IArticleCommentDirectory directory,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IAuthorizationService authz,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        ModerateAsync(articleId, commentId, body, directory, request, session, tenant, guard, authz, environment, cancellationToken,
            (dir, aid, cid, actor, cmd, ct) => dir.ApproveAsync(aid, cid, actor, cmd, ct));

    private static Task<IResult> RejectAsync(
        Guid articleId,
        Guid commentId,
        ModerateArticleCommentBody? body,
        IArticleCommentDirectory directory,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IAuthorizationService authz,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        ModerateAsync(articleId, commentId, body, directory, request, session, tenant, guard, authz, environment, cancellationToken,
            (dir, aid, cid, actor, cmd, ct) => dir.RejectAsync(aid, cid, actor, cmd, ct));

    private static Task<IResult> HideAsync(
        Guid articleId,
        Guid commentId,
        ModerateArticleCommentBody? body,
        IArticleCommentDirectory directory,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IAuthorizationService authz,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        ModerateAsync(articleId, commentId, body, directory, request, session, tenant, guard, authz, environment, cancellationToken,
            (dir, aid, cid, actor, cmd, ct) => dir.HideAsync(aid, cid, actor, cmd, ct));

    private static Task<IResult> MarkPendingAsync(
        Guid articleId,
        Guid commentId,
        ModerateArticleCommentBody? body,
        IArticleCommentDirectory directory,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IAuthorizationService authz,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        ModerateAsync(articleId, commentId, body, directory, request, session, tenant, guard, authz, environment, cancellationToken,
            (dir, aid, cid, actor, cmd, ct) => dir.MarkPendingAsync(aid, cid, actor, cmd, ct));

    private static async Task<IResult> ModerateAsync(
        Guid articleId,
        Guid commentId,
        ModerateArticleCommentBody? body,
        IArticleCommentDirectory directory,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IAuthorizationService authz,
        IHostEnvironment environment,
        CancellationToken cancellationToken,
        Func<IArticleCommentDirectory, Guid, Guid, Guid, ModerateArticleCommentCommand, CancellationToken, Task<ArticleCommentAdminDto>> action)
    {
        try
        {
            var actor = await ContentAdminAccess.RequireAsync(
                request, session, tenant, guard, environment, authz, ContentAdminAccess.Edit, cancellationToken);
            var cmd = new ModerateArticleCommentCommand(body?.Note);
            return Results.Json(await action(directory, articleId, commentId, actor, cmd, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }
}

/// <summary>بدنهٔ ایجاد نظر Admin.</summary>
public sealed record CreateArticleCommentBody(string DisplayName, string Body, Guid? AuthorPartyId = null);

/// <summary>بدنهٔ تعدیل با یادداشت اختیاری.</summary>
public sealed record ModerateArticleCommentBody(string? Note = null);
