using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application.Tags.Commands;
using Tooba.Catalog.Application.Tags.Models;
using Tooba.Catalog.Application.Tags.Queries;
using Tooba.Catalog.Endpoints.Admin;

namespace Tooba.Catalog.Endpoints.Admin.Tags;

/// <summary>Admin Catalog taxonomy tag HTTP — Catalog-owned via MediatR + ApiResponseFactory.</summary>
public static class CatalogTagEndpoints
{
    /// <summary>Maps nine Admin tag routes.</summary>
    public static void MapCatalogTagEndpoints(this IEndpointRouteBuilder app)
    {
        var tags = app.MapGroup("/v1/admin/catalog/tags");
        tags.MapGet("/", ListTagsAsync);
        tags.MapPost("/", CreateTagAsync);
        tags.MapGet("/{tagId:guid}", GetTagAsync);

        var products = app.MapGroup("/v1/admin/catalog/products/{productId:guid}/tags");
        products.MapGet("/", ListProductTagsAsync);
        products.MapPost("/{tagId:guid}", AssignProductTagAsync);
        products.MapDelete("/{tagId:guid}", RemoveProductTagAsync);

        var categories = app.MapGroup("/v1/admin/catalog/categories/{categoryId:guid}/tags");
        categories.MapGet("/", ListCategoryTagsAsync);
        categories.MapPost("/{tagId:guid}", AssignCategoryTagAsync);
        categories.MapDelete("/{tagId:guid}", RemoveCategoryTagAsync);
    }

    private static async Task<IResult> ListTagsAsync(
        string? locale,
        string? search,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new ListTagsQuery(locale, search), cancellationToken));
    }

    private static async Task<IResult> CreateTagAsync(
        CreateTagBody body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        var names = body.LocalizedNames ?? new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(body.NameFa))
        {
            names = new Dictionary<string, string>(names) { ["fa-IR"] = body.NameFa.Trim() };
        }

        if (!string.IsNullOrWhiteSpace(body.NameEn))
        {
            names = new Dictionary<string, string>(names) { ["en"] = body.NameEn.Trim() };
        }

        var model = new CreateTagWriteModel(body.Code, body.Slug, body.Locale ?? "fa-IR", names);
        return api.From(await sender.Send(new CreateTagCommand(model), cancellationToken));
    }

    private static async Task<IResult> GetTagAsync(
        Guid tagId,
        string? locale,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new GetTagQuery(tagId, locale), cancellationToken));
    }

    private static async Task<IResult> ListProductTagsAsync(
        Guid productId,
        string? locale,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new ListProductTagsQuery(productId, locale), cancellationToken));
    }

    private static async Task<IResult> AssignProductTagAsync(
        Guid productId,
        Guid tagId,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new AssignProductTagCommand(productId, tagId), cancellationToken));
    }

    private static async Task<IResult> RemoveProductTagAsync(
        Guid productId,
        Guid tagId,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new RemoveProductTagCommand(productId, tagId), cancellationToken));
    }

    private static async Task<IResult> ListCategoryTagsAsync(
        Guid categoryId,
        string? locale,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new ListCategoryTagsQuery(categoryId, locale), cancellationToken));
    }

    private static async Task<IResult> AssignCategoryTagAsync(
        Guid categoryId,
        Guid tagId,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new AssignCategoryTagCommand(categoryId, tagId), cancellationToken));
    }

    private static async Task<IResult> RemoveCategoryTagAsync(
        Guid categoryId,
        Guid tagId,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new RemoveCategoryTagCommand(categoryId, tagId), cancellationToken));
    }
}

/// <summary>Create-tag transport body.</summary>
public sealed record CreateTagBody(
    string? Code,
    string? Slug,
    string? NameFa,
    string? NameEn,
    string? Locale,
    Dictionary<string, string>? LocalizedNames);
