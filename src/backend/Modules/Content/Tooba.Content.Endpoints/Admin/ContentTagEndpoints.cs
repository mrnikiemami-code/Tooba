using MediatR;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Content.Application.Commands.AssignArticleTag;
using Tooba.Content.Application.Commands.CreateTag;
using Tooba.Content.Application.Commands.RemoveArticleTag;
using Tooba.Content.Application.Queries.ListArticleTags;
using Tooba.Content.Application.Queries.SearchTags;

namespace Tooba.Content.Endpoints.Admin;

/// <summary>مسیرهای Admin برچسب محتوا و انتساب به مقاله.</summary>
public static class ContentTagEndpoints
{
    /// <summary>مسیرهای Admin برچسب محتوا را ثبت می‌کند.</summary>
    public static void MapContentTagEndpoints(this IEndpointRouteBuilder app)
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
        string languageCode, string? search, int? limit, bool? activeOnly,
        ISender sender, ApiResponseFactory api, IContentAdminAuthorizer auth, HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.View, cancellationToken);
        return api.From(await sender.Send(
            new SearchTagsQuery(languageCode, search, limit ?? 30, activeOnly ?? true), cancellationToken));
    }

    private static async Task<IResult> CreateAsync(
        CreateContentTagHttpRequest body, ISender sender, ApiResponseFactory api,
        IContentAdminAuthorizer auth, HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.Edit, cancellationToken);
        return api.From(await sender.Send(
            new CreateTagCommand(body.LanguageCode ?? "", body.Name ?? "", body.Slug), cancellationToken));
    }

    private static async Task<IResult> ListArticleTagsAsync(
        Guid articleId, ISender sender, ApiResponseFactory api,
        IContentAdminAuthorizer auth, HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.View, cancellationToken);
        return api.From(await sender.Send(new ListArticleTagsQuery(articleId), cancellationToken));
    }

    private static async Task<IResult> AssignAsync(
        Guid articleId, Guid tagId, ISender sender, ApiResponseFactory api,
        IContentAdminAuthorizer auth, HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.Edit, cancellationToken);
        return api.From(await sender.Send(new AssignArticleTagCommand(articleId, tagId), cancellationToken));
    }

    private static async Task<IResult> RemoveAsync(
        Guid articleId, Guid tagId, ISender sender, ApiResponseFactory api,
        IContentAdminAuthorizer auth, HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.Edit, cancellationToken);
        return api.From(await sender.Send(new RemoveArticleTagCommand(articleId, tagId), cancellationToken));
    }
}

public sealed record CreateContentTagHttpRequest(string? LanguageCode, string? Name, string? Slug);
