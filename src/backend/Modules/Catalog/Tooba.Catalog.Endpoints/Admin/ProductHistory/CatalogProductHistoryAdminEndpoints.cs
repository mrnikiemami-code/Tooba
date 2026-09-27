using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application.ProductHistory.Queries;
using Tooba.Catalog.Endpoints.Admin;

namespace Tooba.Catalog.Endpoints.Admin.ProductHistory;

/// <summary>Admin Product History HTTP — Catalog-owned via MediatR + ApiResponseFactory.</summary>
public static class CatalogProductHistoryAdminEndpoints
{
    /// <summary>Maps Admin product history GET under /v1/admin/products.</summary>
    public static void MapCatalogProductHistoryAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/v1/admin/products/{productId:guid}");
        products.MapGet("/history", GetAsync);
    }

    private static async Task<IResult> GetAsync(
        Guid productId,
        string? section,
        int? skip,
        int? take,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new GetProductHistoryQuery(productId, section, skip ?? 0, take ?? 50),
            cancellationToken));
    }
}
