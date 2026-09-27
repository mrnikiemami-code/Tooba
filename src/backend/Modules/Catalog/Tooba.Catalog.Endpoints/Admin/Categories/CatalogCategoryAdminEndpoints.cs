using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application.Categories.Commands;
using Tooba.Catalog.Application.Categories.Models;
using Tooba.Catalog.Application.Categories.Queries;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Endpoints.Admin;

namespace Tooba.Catalog.Endpoints.Admin.Categories;

/// <summary>Admin Catalog Category HTTP — Catalog-owned via MediatR + ApiResponseFactory.</summary>
public static class CatalogCategoryAdminEndpoints
{
    /// <summary>Maps nine Admin Category routes.</summary>
    public static void MapCatalogCategoryAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/v1/admin/catalog/categories");
        admin.MapGet("/tree", GetTreeAsync);
        admin.MapGet("/{id:guid}", GetWorkspaceAsync);
        admin.MapPost("/", CreateAsync);
        admin.MapPatch("/{id:guid}", UpdateCoreAsync);
        admin.MapPut("/{id:guid}/translations/{locale}", UpsertTranslationAsync);
        admin.MapPost("/{id:guid}/move", MoveAsync);
        admin.MapPost("/reorder", ReorderAsync);
        admin.MapPost("/{id:guid}/publish", PublishAsync);
        admin.MapPost("/{id:guid}/archive", ArchiveAsync);
    }

    private static async Task<IResult> GetTreeAsync(
        string locale,
        string? search,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new GetCategoryTreeQuery(locale, search), cancellationToken));
    }

    private static async Task<IResult> GetWorkspaceAsync(
        Guid id,
        string? locale,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new GetCategoryWorkspaceQuery(id, locale), cancellationToken));
    }

    private static async Task<IResult> CreateAsync(
        CreateCategoryWriteModel body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        var result = await sender.Send(new CreateCategoryCommand(body), cancellationToken);
        if (result.IsFailure)
        {
            return api.From(result);
        }

        return api.Created($"/v1/admin/catalog/categories/{result.Value.CategoryId}", result);
    }

    private static async Task<IResult> UpdateCoreAsync(
        Guid id,
        UpdateCategoryCoreBody body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new UpdateCategoryCoreCommand(
                id,
                body.Status,
                body.SortOrder,
                body.IsVisible,
                body.ImageMediaAssetId,
                body.IconMediaAssetId,
                body.BannerMediaAssetId,
                body.ClearImage,
                body.ClearIcon,
                body.ClearBanner,
                body.ExpectedUpdatedAt),
            cancellationToken));
    }

    private static async Task<IResult> UpsertTranslationAsync(
        Guid id,
        string locale,
        UpsertCategoryTranslationBody body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new UpsertCategoryTranslationCommand(
                id,
                locale,
                body.Name,
                body.Slug,
                body.ShortDescription,
                body.Description,
                body.SeoTitle,
                body.SeoDescription,
                body.MetaKeywords),
            cancellationToken));
    }

    private static async Task<IResult> MoveAsync(
        Guid id,
        MoveCategoryBody body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new MoveCategoryCommand(id, body.NewParentId, body.ExpectedUpdatedAt),
            cancellationToken));
    }

    private static async Task<IResult> ReorderAsync(
        ReorderCategoriesBody body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new ReorderCategoriesCommand(body.ParentId, body.OrderedCategoryIds),
            cancellationToken));
    }

    private static async Task<IResult> PublishAsync(
        Guid id,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new PublishCategoryCommand(id), cancellationToken));
    }

    private static async Task<IResult> ArchiveAsync(
        Guid id,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new ArchiveCategoryCommand(id), cancellationToken));
    }
}

/// <summary>PATCH body for category core update.</summary>
public sealed record UpdateCategoryCoreBody(
    CatalogPublicationStatus? Status,
    int? SortOrder,
    bool? IsVisible,
    Guid? ImageMediaAssetId,
    Guid? IconMediaAssetId,
    Guid? BannerMediaAssetId = null,
    bool ClearImage = false,
    bool ClearIcon = false,
    bool ClearBanner = false,
    DateTimeOffset? ExpectedUpdatedAt = null);

/// <summary>PUT body for category translation (locale from route).</summary>
public sealed record UpsertCategoryTranslationBody(
    string Name,
    string Slug,
    string? ShortDescription = null,
    string? Description = null,
    string? SeoTitle = null,
    string? SeoDescription = null,
    string? MetaKeywords = null);

/// <summary>POST body for category move.</summary>
public sealed record MoveCategoryBody(Guid? NewParentId, DateTimeOffset? ExpectedUpdatedAt = null);

/// <summary>POST body for sibling reorder.</summary>
public sealed record ReorderCategoriesBody(Guid? ParentId, List<Guid>? OrderedCategoryIds);
