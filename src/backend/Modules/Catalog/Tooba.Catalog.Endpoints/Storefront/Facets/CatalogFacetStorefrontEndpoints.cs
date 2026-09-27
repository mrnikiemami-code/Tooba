using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application.Facets.Queries;

namespace Tooba.Catalog.Endpoints.Storefront.Facets;

/// <summary>Storefront Catalog Facet HTTP — Catalog-owned via MediatR + ApiResponseFactory.</summary>
public static class CatalogFacetStorefrontEndpoints
{
    /// <summary>Maps the Storefront category facets read route.</summary>
    public static void MapCatalogFacetStorefrontEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/storefront/categories/{categoryId:guid}/facets", GetStorefrontFacetsAsync);
    }

    private static async Task<IResult> GetStorefrontFacetsAsync(
        Guid categoryId,
        string? locale,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken) =>
        api.From(await sender.Send(new GetStorefrontCategoryFacetsQuery(categoryId, locale), cancellationToken));
}
