using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application.ProductPublishing.Queries;
using Tooba.Catalog.Endpoints.Admin;

namespace Tooba.Catalog.Endpoints.Admin.ProductPublishing;

/// <summary>Admin Product Publish Readiness HTTP — Catalog-owned via MediatR + ApiResponseFactory.</summary>
public static class CatalogProductPublishReadinessAdminEndpoints
{
    /// <summary>Maps Admin product publish-readiness GET under /v1/admin/products.</summary>
    public static void MapCatalogProductPublishReadinessAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/v1/admin/products/{productId:guid}");
        products.MapGet("/publish/readiness", GetAsync);
    }

    private static async Task<IResult> GetAsync(
        Guid productId,
        string? locale,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new GetProductPublishReadinessQuery(productId, locale),
            cancellationToken));
    }
}
