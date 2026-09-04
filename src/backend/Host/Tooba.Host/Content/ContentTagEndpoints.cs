using Tooba.BuildingBlocks;
using Tooba.Content.Application;
using Tooba.Content.Domain;

namespace Tooba.Host.Content;

/// <summary>مسیرهای Admin برچسب محتوا و انتساب به مقاله.</summary>
public static class ContentTagEndpoints
{
    /// <summary>مسیرهای Admin برچسب محتوا را ثبت می‌کند.</summary>
    public static void MapContentTagEndpoints(this WebApplication app)
    {
        var tags = app.MapGroup("/v1/admin/content/tags");
        tags.MapGet("/", SearchAsync);
        tags.MapPost("/", CreateAsync);

        var articleTags = app.MapGroup("/v1/admin/content/articles/{articleId:guid}/tags");
        articleTags.MapGet("/", ListArticleTagsAsync);
        articleTags.MapPost("/{tagId:guid}", AssignAsync);
        articleTags.MapDelete("/{tagId:guid}", RemoveAsync);
    }

    private static async Task<IResult> SearchAsync(
        string languageCode,
        string? search,
        int? limit,
        bool? activeOnly,
        IContentTagDirectory directory,
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
            return Results.Json(await directory.SearchAsync(
                languageCode,
                search,
                limit ?? 30,
                activeOnly ?? true,
                cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }

    private static async Task<IResult> CreateAsync(
        CreateContentTagHttpRequest body,
        IContentTagDirectory directory,
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
                new CreateContentTagCommand(body.LanguageCode ?? "", body.Name ?? "", body.Slug),
                cancellationToken);
            return Results.Json(created);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }

    private static async Task<IResult> ListArticleTagsAsync(
        Guid articleId,
        IContentTagDirectory directory,
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
            return Results.Json(await directory.ListArticleTagsAsync(articleId, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }

    private static async Task<IResult> AssignAsync(
        Guid articleId,
        Guid tagId,
        IContentTagDirectory directory,
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
            return Results.Json(await directory.AssignToArticleAsync(articleId, tagId, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }

    private static async Task<IResult> RemoveAsync(
        Guid articleId,
        Guid tagId,
        IContentTagDirectory directory,
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
            return Results.Json(await directory.RemoveFromArticleAsync(articleId, tagId, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return MapInvalid(ex); }
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);

    private static IResult MapInvalid(InvalidOperationException ex) =>
        Results.Json(new { title = ex.Message, errorCode = ex.Message }, statusCode: StatusCodes.Status400BadRequest);
}

/// <summary>بدنهٔ ایجاد برچسب محتوا.</summary>
public sealed record CreateContentTagHttpRequest(string? LanguageCode, string? Name, string? Slug);
