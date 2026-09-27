using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Facets.Commands;
using Tooba.Catalog.Application.Facets.Models;
using Tooba.Catalog.Application.Facets.Queries;
using Tooba.Catalog.Endpoints.Admin;

namespace Tooba.Catalog.Endpoints.Admin.Facets;

/// <summary>Admin Catalog Facet HTTP — Catalog-owned via MediatR + ApiResponseFactory.</summary>
public static class CatalogFacetAdminEndpoints
{
    /// <summary>Maps five Admin Facet routes.</summary>
    public static void MapCatalogFacetAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var categories = app.MapGroup("/v1/admin/catalog/categories/{categoryId:guid}/facets");
        categories.MapGet("/effective", GetEffectiveFacetsAsync);
        categories.MapGet("/local", ListLocalFacetsAsync);
        categories.MapPut("/{definitionId:guid}", UpsertFacetAsync);
        categories.MapDelete("/{definitionId:guid}", RemoveFacetOverrideAsync);
        categories.MapPut("/order", ReorderFacetsAsync);
    }

    private static async Task<IResult> GetEffectiveFacetsAsync(
        Guid categoryId,
        string? locale,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new GetEffectiveCategoryFacetsQuery(categoryId, locale), cancellationToken));
    }

    private static async Task<IResult> ListLocalFacetsAsync(
        Guid categoryId,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new ListLocalCategoryFacetsQuery(categoryId), cancellationToken));
    }

    private static async Task<IResult> UpsertFacetAsync(
        Guid categoryId,
        Guid definitionId,
        CategoryFacetConfigurationInput body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new UpsertCategoryFacetCommand(categoryId, definitionId, body),
            cancellationToken));
    }

    private static async Task<IResult> RemoveFacetOverrideAsync(
        Guid categoryId,
        Guid definitionId,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new RemoveCategoryFacetOverrideCommand(categoryId, definitionId),
            cancellationToken));
    }

    private static async Task<IResult> ReorderFacetsAsync(
        Guid categoryId,
        ReorderCategoryFacetsWriteModel body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new ReorderCategoryFacetsCommand(categoryId, body.OrderedDefinitionIds),
            cancellationToken));
    }
}
