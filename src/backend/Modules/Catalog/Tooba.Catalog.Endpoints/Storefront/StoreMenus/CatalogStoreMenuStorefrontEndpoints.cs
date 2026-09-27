using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application.StoreMenus.Queries;

namespace Tooba.Catalog.Endpoints.Storefront.StoreMenus;

/// <summary>Storefront Store Menu HTTP — Catalog-owned via MediatR + ApiResponseFactory.</summary>
public static class CatalogStoreMenuStorefrontEndpoints
{
    /// <summary>Maps /v1/storefront/header-menu and /v1/storefront/menus/{menuId}.</summary>
    public static void MapCatalogStoreMenuStorefrontEndpoints(this IEndpointRouteBuilder app)
    {
        var storefront = app.MapGroup("/v1/storefront");
        storefront.MapGet("/header-menu", GetHeaderPublicAsync);
        storefront.MapGet("/menus/{menuId:guid}", GetPublicMenuAsync);
    }

    private static async Task<IResult> GetHeaderPublicAsync(
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken) =>
        api.From(await sender.Send(new GetStoreHeaderMenuPublicQuery(), cancellationToken));

    private static async Task<IResult> GetPublicMenuAsync(
        Guid menuId,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken) =>
        api.From(await sender.Send(new ProjectPublicStoreMenuQuery(menuId), cancellationToken));
}
