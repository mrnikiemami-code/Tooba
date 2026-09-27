using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application.Categories.Queries;

namespace Tooba.Catalog.Endpoints.Storefront.Categories;

/// <summary>Storefront Catalog Category route resolve — Catalog-owned via MediatR.</summary>
public static class CatalogCategoryStorefrontEndpoints
{
    /// <summary>Maps the storefront category-route resolve endpoint.</summary>
    public static void MapCatalogCategoryStorefrontEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/storefront/category-routes/resolve", ResolveRouteAsync);
    }

    private static async Task<IResult> ResolveRouteAsync(
        string locale,
        string slug,
        bool forStorefront,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken) =>
        api.From(await sender.Send(
            new ResolveCategoryRouteQuery(locale, slug, forStorefront),
            cancellationToken));
}
