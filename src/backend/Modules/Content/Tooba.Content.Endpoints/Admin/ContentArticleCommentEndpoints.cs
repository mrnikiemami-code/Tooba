using MediatR;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Commands.ApproveArticleComment;
using Tooba.Content.Application.Commands.CreateArticleComment;
using Tooba.Content.Application.Commands.HideArticleComment;
using Tooba.Content.Application.Commands.MarkArticleCommentPending;
using Tooba.Content.Application.Commands.RejectArticleComment;
using Tooba.Content.Application.Queries.ListArticleComments;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Domain.Aggregates;
using Tooba.Content.Domain.Rules;

namespace Tooba.Content.Endpoints.Admin;

/// <summary>مسیرهای Admin تعدیل نظرات مقاله.</summary>
public static class ContentArticleCommentEndpoints
{
    /// <summary>مسیرهای نظرات مقاله را ثبت می‌کند.</summary>
    public static void MapContentArticleCommentEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/v1/admin/content/articles/{articleId:guid}/comments");
        admin.MapGet("/", ListAsync);
        admin.MapPost("/", CreateAsync);
        admin.MapPost("/{commentId:guid}/approve", ApproveAsync);
        admin.MapPost("/{commentId:guid}/reject", RejectAsync);
        admin.MapPost("/{commentId:guid}/hide", HideAsync);
        admin.MapPost("/{commentId:guid}/pending", MarkPendingAsync);
    }

    private static async Task<IResult> ListAsync(
        Guid articleId, ISender sender, ApiResponseFactory api,
        IContentAdminAuthorizer auth, HttpContext http,
        string? status = null, string? search = null, int skip = 0, int take = 20,
        CancellationToken cancellationToken = default)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.View, cancellationToken);
        ArticleCommentStatus? parsed = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ArticleCommentStatus>(status, ignoreCase: true, out var value))
                return api.FromFailure(new SemanticError(ContentErrorCodes.CommentInvalidPayload));
            parsed = value;
        }

        return api.From(await sender.Send(
            new ListArticleCommentsQuery(articleId, parsed, search, skip, take), cancellationToken));
    }

    private static async Task<IResult> CreateAsync(
        Guid articleId, CreateArticleCommentBody body, ISender sender, ApiResponseFactory api,
        IContentAdminAuthorizer auth, HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.Edit, cancellationToken);
        return api.From(await sender.Send(
            new CreateArticleCommentCommand(articleId, body.DisplayName, body.Body, body.AuthorPartyId),
            cancellationToken));
    }

    private static async Task<IResult> ApproveAsync(
        Guid articleId, Guid commentId, ModerateArticleCommentBody? body,
        ISender sender, ApiResponseFactory api, IContentAdminAuthorizer auth, HttpContext http,
        CancellationToken cancellationToken)
    {
        var actor = await auth.RequireAsync(http, ContentAdminPermissions.Edit, cancellationToken);
        return api.From(await sender.Send(
            new ApproveArticleCommentCommand(articleId, commentId, actor, body?.Note), cancellationToken));
    }

    private static async Task<IResult> RejectAsync(
        Guid articleId, Guid commentId, ModerateArticleCommentBody? body,
        ISender sender, ApiResponseFactory api, IContentAdminAuthorizer auth, HttpContext http,
        CancellationToken cancellationToken)
    {
        var actor = await auth.RequireAsync(http, ContentAdminPermissions.Edit, cancellationToken);
        return api.From(await sender.Send(
            new RejectArticleCommentCommand(articleId, commentId, actor, body?.Note), cancellationToken));
    }

    private static async Task<IResult> HideAsync(
        Guid articleId, Guid commentId, ModerateArticleCommentBody? body,
        ISender sender, ApiResponseFactory api, IContentAdminAuthorizer auth, HttpContext http,
        CancellationToken cancellationToken)
    {
        var actor = await auth.RequireAsync(http, ContentAdminPermissions.Edit, cancellationToken);
        return api.From(await sender.Send(
            new HideArticleCommentCommand(articleId, commentId, actor, body?.Note), cancellationToken));
    }

    private static async Task<IResult> MarkPendingAsync(
        Guid articleId, Guid commentId, ModerateArticleCommentBody? body,
        ISender sender, ApiResponseFactory api, IContentAdminAuthorizer auth, HttpContext http,
        CancellationToken cancellationToken)
    {
        var actor = await auth.RequireAsync(http, ContentAdminPermissions.Edit, cancellationToken);
        return api.From(await sender.Send(
            new MarkArticleCommentPendingCommand(articleId, commentId, actor, body?.Note), cancellationToken));
    }
}

public sealed record CreateArticleCommentBody(string DisplayName, string Body, Guid? AuthorPartyId = null);
public sealed record ModerateArticleCommentBody(string? Note = null);
