using MediatR;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Content.Application.Commands.AddGalleryMedia;
using Tooba.Content.Application.Commands.AssignFeaturedMedia;
using Tooba.Content.Application.Commands.AssignSeoImage;
using Tooba.Content.Application.Commands.PatchGalleryMedia;
using Tooba.Content.Application.Commands.RemoveGalleryMedia;
using Tooba.Content.Application.Commands.ReorderGallery;
using Tooba.Content.Application.Queries.GetArticleMediaWorkspace;

namespace Tooba.Content.Endpoints.Admin;

/// <summary>مسیرهای Admin رسانهٔ مقاله.</summary>
public static class ContentArticleMediaEndpoints
{
    /// <summary>مسیرهای رسانهٔ مقاله را ثبت می‌کند.</summary>
    public static void MapContentArticleMediaEndpoints(this IEndpointRouteBuilder app)
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

    private static async Task<IResult> GetWorkspaceAsync(
        Guid articleId, ISender sender, ApiResponseFactory api,
        IContentAdminAuthorizer auth, HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.View, cancellationToken);
        return api.From(await sender.Send(new GetArticleMediaWorkspaceQuery(articleId), cancellationToken));
    }

    private static async Task<IResult> AssignFeaturedAsync(
        Guid articleId, AssignMediaBody body, ISender sender, ApiResponseFactory api,
        IContentAdminAuthorizer auth, HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.Edit, cancellationToken);
        return api.From(await sender.Send(new AssignFeaturedMediaCommand(articleId, body.MediaAssetId), cancellationToken));
    }

    private static async Task<IResult> AssignSeoImageAsync(
        Guid articleId, AssignMediaBody body, ISender sender, ApiResponseFactory api,
        IContentAdminAuthorizer auth, HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.Edit, cancellationToken);
        return api.From(await sender.Send(new AssignSeoImageCommand(articleId, body.MediaAssetId), cancellationToken));
    }

    private static async Task<IResult> AddGalleryAsync(
        Guid articleId, AddGalleryBody body, ISender sender, ApiResponseFactory api,
        IContentAdminAuthorizer auth, HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.Edit, cancellationToken);
        return api.From(await sender.Send(
            new AddGalleryMediaCommand(articleId, body.MediaAssetIds ?? []), cancellationToken));
    }

    private static async Task<IResult> RemoveGalleryAsync(
        Guid articleId, Guid mediaAssetId, ISender sender, ApiResponseFactory api,
        IContentAdminAuthorizer auth, HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.Edit, cancellationToken);
        return api.From(await sender.Send(new RemoveGalleryMediaCommand(articleId, mediaAssetId), cancellationToken));
    }

    private static async Task<IResult> ReorderGalleryAsync(
        Guid articleId, ReorderGalleryBody body, ISender sender, ApiResponseFactory api,
        IContentAdminAuthorizer auth, HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.Edit, cancellationToken);
        return api.From(await sender.Send(
            new ReorderGalleryCommand(articleId, body.OrderedMediaAssetIds ?? []), cancellationToken));
    }

    private static async Task<IResult> PatchGalleryAsync(
        Guid articleId, Guid mediaAssetId, PatchGalleryBody body, ISender sender, ApiResponseFactory api,
        IContentAdminAuthorizer auth, HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.Edit, cancellationToken);
        return api.From(await sender.Send(
            new PatchGalleryMediaCommand(articleId, mediaAssetId, body.AltText, body.Caption), cancellationToken));
    }
}

public sealed record AssignMediaBody(Guid? MediaAssetId);
public sealed record AddGalleryBody(IReadOnlyList<Guid>? MediaAssetIds);
public sealed record ReorderGalleryBody(IReadOnlyList<Guid>? OrderedMediaAssetIds);
public sealed record PatchGalleryBody(string? AltText, string? Caption);
