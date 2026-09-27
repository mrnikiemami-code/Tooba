using MediatR;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Content.Application.Commands.CreateAuthor;
using Tooba.Content.Application.Commands.DeactivateAuthor;
using Tooba.Content.Application.Commands.UpdateAuthor;
using Tooba.Content.Application.Queries.GetAuthorPickerList;
using Tooba.Content.Application.Queries.GetAuthorWorkspace;
using Tooba.Content.Application.Queries.QueryAdminAuthorsGrid;

namespace Tooba.Content.Endpoints.Admin;

/// <summary>مسیرهای Admin نویسندهٔ مقاله.</summary>
public static class ContentAuthorEndpoints
{
    /// <summary>مسیرهای Admin نویسندهٔ مقاله را ثبت می‌کند.</summary>
    public static void MapContentAuthorEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/v1/admin/content/authors");
        admin.MapPost("/query", QueryGridAsync);
        admin.MapGet("/picker", GetPickerListAsync);
        admin.MapGet("/{id:guid}", GetWorkspaceAsync);
        admin.MapPost("/", CreateAsync);
        admin.MapPatch("/{id:guid}", UpdateAsync);
        admin.MapPost("/{id:guid}/deactivate", DeactivateAsync);
    }

    private static async Task<IResult> QueryGridAsync(
        GridQueryRequest body, ISender sender, ApiResponseFactory api,
        IContentAdminAuthorizer auth, HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.View, cancellationToken);
        return api.From(await sender.Send(new QueryAdminAuthorsGridQuery(body), cancellationToken));
    }

    private static async Task<IResult> GetPickerListAsync(
        string? search, bool activeOnly, ISender sender, ApiResponseFactory api,
        IContentAdminAuthorizer auth, HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.View, cancellationToken);
        return api.From(await sender.Send(new GetAuthorPickerListQuery(search, activeOnly), cancellationToken));
    }

    private static async Task<IResult> GetWorkspaceAsync(
        Guid id, ISender sender, ApiResponseFactory api,
        IContentAdminAuthorizer auth, HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.View, cancellationToken);
        return api.From(await sender.Send(new GetAuthorWorkspaceQuery(id), cancellationToken));
    }

    private static async Task<IResult> CreateAsync(
        CreateContentAuthorHttpRequest body, ISender sender, ApiResponseFactory api,
        IContentAdminAuthorizer auth, HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.Create, cancellationToken);
        var result = await sender.Send(new CreateAuthorCommand(
            body.DisplayName ?? "", body.Slug ?? "", body.ShortBio, body.FullBio,
            body.ProfileImageMediaAssetId, body.CoverImageMediaAssetId,
            body.WebsiteUrl, body.InstagramUrl, body.TwitterUrl, body.LinkedInUrl), cancellationToken);
        return result.IsSuccess
            ? Results.Json(result.Value, statusCode: StatusCodes.Status201Created)
            : api.From(result);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id, UpdateContentAuthorHttpRequest body, ISender sender, ApiResponseFactory api,
        IContentAdminAuthorizer auth, HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.Edit, cancellationToken);
        return api.From(await sender.Send(new UpdateAuthorCommand(
            id, body.DisplayName ?? "", body.Slug ?? "", body.ShortBio, body.FullBio,
            body.ProfileImageMediaAssetId, body.CoverImageMediaAssetId,
            body.WebsiteUrl, body.InstagramUrl, body.TwitterUrl, body.LinkedInUrl), cancellationToken));
    }

    private static async Task<IResult> DeactivateAsync(
        Guid id, ISender sender, ApiResponseFactory api,
        IContentAdminAuthorizer auth, HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.Edit, cancellationToken);
        var result = await sender.Send(new DeactivateAuthorCommand(id), cancellationToken);
        return result.IsSuccess ? Results.Ok() : api.From(result);
    }
}

public sealed record CreateContentAuthorHttpRequest(
    string? DisplayName, string? Slug, string? ShortBio, string? FullBio,
    Guid? ProfileImageMediaAssetId, Guid? CoverImageMediaAssetId,
    string? WebsiteUrl, string? InstagramUrl, string? TwitterUrl, string? LinkedInUrl);

public sealed record UpdateContentAuthorHttpRequest(
    string? DisplayName, string? Slug, string? ShortBio, string? FullBio,
    Guid? ProfileImageMediaAssetId, Guid? CoverImageMediaAssetId,
    string? WebsiteUrl, string? InstagramUrl, string? TwitterUrl, string? LinkedInUrl);
