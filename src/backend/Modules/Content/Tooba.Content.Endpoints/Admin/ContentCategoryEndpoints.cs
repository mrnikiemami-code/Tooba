using MediatR;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Content.Application.Commands.ArchiveCategory;
using Tooba.Content.Application.Commands.CreateCategory;
using Tooba.Content.Application.Commands.MoveCategory;
using Tooba.Content.Application.Commands.ReorderCategories;
using Tooba.Content.Application.Commands.UpdateCategory;
using Tooba.Content.Application.Commands.UpdateCategoryMedia;
using Tooba.Content.Application.Commands.UpdateCategorySeo;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Queries.GetCategoryTree;
using Tooba.Content.Application.Queries.GetCategoryWorkspace;

namespace Tooba.Content.Endpoints.Admin;

/// <summary>مسیرهای Admin دسته‌بندی مقاله.</summary>
public static class ContentCategoryEndpoints
{
    /// <summary>مسیرهای Admin دسته‌بندی مقاله را ثبت می‌کند.</summary>
    public static void MapContentCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/v1/admin/content/categories");
        admin.MapGet("/tree", GetTreeAsync);
        admin.MapGet("/{id:guid}", GetWorkspaceAsync);
        admin.MapPost("/", CreateAsync);
        admin.MapPatch("/{id:guid}", UpdateAsync);
        admin.MapPut("/{id:guid}/seo", UpdateSeoAsync);
        admin.MapPut("/{id:guid}/media", UpdateMediaAsync);
        admin.MapPost("/{id:guid}/move", MoveAsync);
        admin.MapPost("/reorder", ReorderAsync);
        admin.MapPost("/{id:guid}/archive", ArchiveAsync);
    }

    private static async Task<IResult> GetTreeAsync(
        string languageCode, string? search, ISender sender, ApiResponseFactory api,
        IContentAdminAuthorizer auth, HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.View, cancellationToken);
        return api.From(await sender.Send(new GetCategoryTreeQuery(languageCode, search), cancellationToken));
    }

    private static async Task<IResult> GetWorkspaceAsync(
        Guid id, ISender sender, ApiResponseFactory api,
        IContentAdminAuthorizer auth, HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.View, cancellationToken);
        return api.From(await sender.Send(new GetCategoryWorkspaceQuery(id), cancellationToken));
    }

    private static async Task<IResult> CreateAsync(
        CreateContentCategoryHttpRequest body, ISender sender, ApiResponseFactory api,
        IContentAdminAuthorizer auth, HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.Create, cancellationToken);
        return api.From(await sender.Send(new CreateCategoryCommand(
            body.LanguageCode ?? "", body.ParentCategoryId, body.Name ?? "", body.Slug ?? "",
            body.ShortDescription, body.Description, body.SortOrder ?? 0), cancellationToken));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id, UpdateContentCategoryHttpRequest body, ISender sender, ApiResponseFactory api,
        IContentAdminAuthorizer auth, HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.Edit, cancellationToken);
        return api.From(await sender.Send(new UpdateCategoryCommand(
            id, body.Name ?? "", body.Slug ?? "", body.ShortDescription, body.Description,
            body.SortOrder ?? 0, body.Status ?? "Active"), cancellationToken));
    }

    private static async Task<IResult> UpdateSeoAsync(
        Guid id, UpdateContentCategorySeoHttpRequest body, ISender sender, ApiResponseFactory api,
        IContentAdminAuthorizer auth, HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.Edit, cancellationToken);
        return api.From(await sender.Send(new UpdateCategorySeoCommand(id, body.SeoTitle, body.SeoDescription), cancellationToken));
    }

    private static async Task<IResult> UpdateMediaAsync(
        Guid id, UpdateContentCategoryMediaHttpRequest body, ISender sender, ApiResponseFactory api,
        IContentAdminAuthorizer auth, HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.Edit, cancellationToken);
        return api.From(await sender.Send(new UpdateCategoryMediaCommand(id, body.ImageMediaAssetId), cancellationToken));
    }

    private static async Task<IResult> MoveAsync(
        Guid id, MoveContentCategoryHttpRequest body, ISender sender, ApiResponseFactory api,
        IContentAdminAuthorizer auth, HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.Edit, cancellationToken);
        return api.From(await sender.Send(new MoveCategoryCommand(id, body.NewParentId), cancellationToken));
    }

    private static async Task<IResult> ReorderAsync(
        ReorderContentCategoriesHttpRequest body, ISender sender, ApiResponseFactory api,
        IContentAdminAuthorizer auth, HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.Edit, cancellationToken);
        var items = (body.Items ?? [])
            .Select(x => new ReorderContentCategoryItem(x.CategoryId, x.SortOrder))
            .ToList();
        return api.From(await sender.Send(new ReorderCategoriesCommand(items), cancellationToken));
    }

    private static async Task<IResult> ArchiveAsync(
        Guid id, ISender sender, ApiResponseFactory api,
        IContentAdminAuthorizer auth, HttpContext http, CancellationToken cancellationToken)
    {
        await auth.RequireAsync(http, ContentAdminPermissions.Edit, cancellationToken);
        var result = await sender.Send(new ArchiveCategoryCommand(id), cancellationToken);
        return result.IsSuccess ? Results.Ok() : api.From(result);
    }
}

public sealed record CreateContentCategoryHttpRequest(
    string? LanguageCode, Guid? ParentCategoryId, string? Name, string? Slug,
    string? ShortDescription, string? Description, int? SortOrder);

public sealed record UpdateContentCategoryHttpRequest(
    string? Name, string? Slug, string? ShortDescription, string? Description, int? SortOrder, string? Status);

public sealed record UpdateContentCategorySeoHttpRequest(string? SeoTitle, string? SeoDescription);

public sealed record UpdateContentCategoryMediaHttpRequest(Guid? ImageMediaAssetId);

public sealed record MoveContentCategoryHttpRequest(Guid? NewParentId);

public sealed record ReorderContentCategoriesHttpRequest(IReadOnlyList<ReorderContentCategoryHttpItem>? Items);

public sealed record ReorderContentCategoryHttpItem(Guid CategoryId, int SortOrder);

