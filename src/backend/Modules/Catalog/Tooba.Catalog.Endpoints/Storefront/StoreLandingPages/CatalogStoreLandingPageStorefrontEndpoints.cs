using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application.StoreLandingPages.Queries;

namespace Tooba.Catalog.Endpoints.Storefront.StoreLandingPages;

/// <summary>Storefront Landing HTTP — Catalog-owned via MediatR + ApiResponseFactory.</summary>
public static class CatalogStoreLandingPageStorefrontEndpoints
{
    /// <summary>Maps /v1/storefront/pages* and home-selection.</summary>
    public static void MapCatalogStoreLandingPageStorefrontEndpoints(this IEndpointRouteBuilder app)
    {
        var storefront = app.MapGroup("/v1/storefront");
        storefront.MapGet("/pages", ListPublicSitemapAsync);
        storefront.MapGet("/pages/{slug}", GetPublicAsync);
        storefront.MapGet("/home-selection", GetHomePublicAsync);
    }

    private static async Task<IResult> GetPublicAsync(
        string slug,
        string? locale,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken) =>
        api.From(await sender.Send(new ResolvePublicStoreLandingPageQuery(locale, slug), cancellationToken));

    private static async Task<IResult> ListPublicSitemapAsync(
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken) =>
        api.From(await sender.Send(new ListStoreLandingSitemapQuery(), cancellationToken));

    private static async Task<IResult> GetHomePublicAsync(
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken) =>
        api.From(await sender.Send(new GetStoreHomeSelectionQuery(), cancellationToken));
}
