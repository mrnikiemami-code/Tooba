using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application.Brands.Queries;
using Tooba.Catalog.Endpoints.Admin;

namespace Tooba.Catalog.Endpoints.Admin.Brands;

/// <summary>Admin brand-options HTTP — Catalog-owned via MediatR + ApiResponseFactory.</summary>
public static class CatalogBrandOptionsAdminEndpoints
{
    /// <summary>Maps GET /v1/admin/products/brand-options.</summary>
    public static void MapCatalogBrandOptionsAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/v1/admin/products");
        products.MapGet("/brand-options", ListAsync);
    }

    private static async Task<IResult> ListAsync(
        string? q,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new ListBrandOptionsQuery(q), cancellationToken));
    }
}
