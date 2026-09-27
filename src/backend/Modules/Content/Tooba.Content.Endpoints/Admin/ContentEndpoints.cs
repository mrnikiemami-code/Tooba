using MediatR;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Content.Application.Commands.ArchiveArticle;
using Tooba.Content.Application.Commands.CreateArticle;
using Tooba.Content.Application.Commands.DeleteArticle;
using Tooba.Content.Application.Commands.PublishArticle;
using Tooba.Content.Application.Commands.UnpublishArticle;
using Tooba.Content.Application.Commands.UpdateArticle;
using Tooba.Content.Application.Queries.GetAdminArticle;
using Tooba.Content.Application.Queries.GetArticlePreview;
using Tooba.Content.Application.Queries.GetPublishReadiness;
using Tooba.Content.Application.Queries.ListAdminArticles;
using Tooba.Content.Application.Queries.ListArticleHistory;
using Tooba.Content.Application.Queries.QueryAdminArticlesGrid;

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

    private static async Task<IResult> AdminListAsync(
        ISender sender,
        ApiResponseFactory api,
        IContentAdminAuthorizer auth,
        HttpContext http,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.View, cancellationToken);
        return api.From(await sender.Send(new ListAdminArticlesQuery(page, pageSize), cancellationToken));
    }

    private static async Task<IResult> AdminQueryGridAsync(
        GridQueryRequest body,
        ISender sender,
        ApiResponseFactory api,
        IContentAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.View, cancellationToken);
        return api.From(await sender.Send(new QueryAdminArticlesGridQuery(body), cancellationToken));
    }

    private static async Task<IResult> AdminGetAsync(
        Guid id,
        ISender sender,
        ApiResponseFactory api,
        IContentAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.View, cancellationToken);
        return api.From(await sender.Send(new GetAdminArticleQuery(id), cancellationToken));
    }

    private static async Task<IResult> AdminGetPublishReadinessAsync(
        Guid id,
        ISender sender,
        ApiResponseFactory api,
        IContentAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.View, cancellationToken);
        return api.From(await sender.Send(new GetPublishReadinessQuery(id), cancellationToken));
    }

    private static async Task<IResult> AdminGetPreviewAsync(
        Guid id,
        ISender sender,
        ApiResponseFactory api,
        IContentAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.View, cancellationToken);
        return api.From(await sender.Send(new GetArticlePreviewQuery(id), cancellationToken));
    }

    private static async Task<IResult> AdminListHistoryAsync(
        Guid id,
        ISender sender,
        ApiResponseFactory api,
        IContentAdminAuthorizer auth,
        HttpContext http,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.View, cancellationToken);
        return api.From(await sender.Send(new ListArticleHistoryQuery(id, skip, take), cancellationToken));
    }

    private static async Task<IResult> AdminCreateAsync(
        CreateArticleBody body,
        ISender sender,
        ApiResponseFactory api,
        IContentAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.Create, cancellationToken);
        var result = await sender.Send(
            new CreateArticleCommand(
                body.Slug, body.Title, body.Excerpt, body.Body,
                body.CoverMediaAssetId, body.AuthorId, body.Tags, body.IsFeatured,
                body.PublishDate, body.Locale, body.SeoTitle, body.SeoDescription,
                body.Category, body.CategoryId),
            cancellationToken);
        return result.IsSuccess
            ? api.Created($"/v1/admin/content/articles/{result.Value.ArticleId}", result)
            : api.From(result);
    }

    private static async Task<IResult> AdminUpdateAsync(
        Guid id,
        UpdateArticleBody body,
        ISender sender,
        ApiResponseFactory api,
        IContentAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.Edit, cancellationToken);
        return api.From(await sender.Send(
            new UpdateArticleCommand(
                id, body.Title, body.Excerpt, body.Body, body.CoverMediaAssetId, body.AuthorId,
                body.Tags, body.IsFeatured, body.Locale, body.SeoTitle, body.SeoDescription,
                body.Category, body.CategoryId, body.PublishDate),
            cancellationToken));
    }

    private static async Task<IResult> AdminPublishAsync(
        Guid id, ISender sender, ApiResponseFactory api, IContentAdminAuthorizer auth,
        HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.Publish, cancellationToken);
        return api.From(await sender.Send(new PublishArticleCommand(id), cancellationToken));
    }

    private static async Task<IResult> AdminUnpublishAsync(
        Guid id, ISender sender, ApiResponseFactory api, IContentAdminAuthorizer auth,
        HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.Publish, cancellationToken);
        return api.From(await sender.Send(new UnpublishArticleCommand(id), cancellationToken));
    }

    private static async Task<IResult> AdminArchiveAsync(
        Guid id, ISender sender, ApiResponseFactory api, IContentAdminAuthorizer auth,
        HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.Edit, cancellationToken);
        return api.From(await sender.Send(new ArchiveArticleCommand(id), cancellationToken));
    }

    private static async Task<IResult> AdminDeleteAsync(
        Guid id, ISender sender, ApiResponseFactory api, IContentAdminAuthorizer auth,
        HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.Edit, cancellationToken);
        return api.From(await sender.Send(new DeleteArticleCommand(id), cancellationToken));
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
