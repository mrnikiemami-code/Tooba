using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductSeo.Commands;
using Tooba.Catalog.Application.ProductSeo.Models;
using Tooba.Catalog.Application.ProductSeo.Queries;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Endpoints.Admin;

namespace Tooba.Catalog.Endpoints.Admin.ProductSeo;

/// <summary>Admin Product SEO HTTP — Catalog-owned via MediatR + ApiResponseFactory.</summary>
public static class CatalogProductSeoAdminEndpoints
{
    /// <summary>Maps three Admin product SEO routes under /v1/admin/products.</summary>
    public static void MapCatalogProductSeoAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/v1/admin/products/{productId:guid}");
        products.MapGet("/seo", GetAsync);
        products.MapPut("/seo", PutAsync);
        products.MapGet("/seo/readiness", GetReadinessAsync);
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
        return api.From(await sender.Send(new GetProductSeoQuery(productId, locale), cancellationToken));
    }

    private static async Task<IResult> PutAsync(
        Guid productId,
        UpdateProductSeoWriteModel body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var actorUserId = await auth.RequireAuthorizedAsync(http, cancellationToken);
        if (!CatalogWorkspaceScope.AllowsCatalogEdit(http.Request))
        {
            return api.FromFailure(new SemanticError(CatalogErrorCodes.WorkspacePermissionDenied));
        }

        await CatalogActorRequestBinding.BindAsync(http, actorUserId, cancellationToken);
        return api.From(await sender.Send(new UpdateProductSeoCommand(productId, body), cancellationToken));
    }

    private static async Task<IResult> GetReadinessAsync(
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
            new GetProductSeoReadinessQuery(productId, locale),
            cancellationToken));
    }
}
