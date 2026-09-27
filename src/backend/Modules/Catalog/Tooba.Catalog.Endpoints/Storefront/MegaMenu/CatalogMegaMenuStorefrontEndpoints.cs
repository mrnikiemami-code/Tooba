using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application.MegaMenu.Queries;

namespace Tooba.Catalog.Endpoints.Storefront.MegaMenu;

/// <summary>Storefront MegaMenu HTTP — Catalog-owned via MediatR + ApiResponseFactory.</summary>
public static class CatalogMegaMenuStorefrontEndpoints
{
    /// <summary>Maps the Storefront MegaMenu read route.</summary>
    public static void MapCatalogMegaMenuStorefrontEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/storefront/mega-menu", GetStorefrontMegaMenuAsync);
    }

    private static async Task<IResult> GetStorefrontMegaMenuAsync(
        string? locale,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken) =>
        api.From(await sender.Send(new GetStorefrontMegaMenuQuery(locale), cancellationToken));
}
