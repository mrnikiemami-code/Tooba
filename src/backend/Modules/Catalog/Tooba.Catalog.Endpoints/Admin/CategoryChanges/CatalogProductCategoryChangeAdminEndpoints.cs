using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application.CategoryChanges.Commands;
using Tooba.Catalog.Application.CategoryChanges.Models;
using Tooba.Catalog.Application.CategoryChanges.Queries;
using Tooba.Catalog.Endpoints.Admin;

namespace Tooba.Catalog.Endpoints.Admin.CategoryChanges;

/// <summary>Admin Catalog category-change HTTP — Catalog-owned via MediatR + ApiResponseFactory.</summary>
public static class CatalogProductCategoryChangeAdminEndpoints
{
    /// <summary>Maps two Admin category-change routes.</summary>
    public static void MapCatalogProductCategoryChangeAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/v1/admin/catalog/products/{productId:guid}");
        products.MapPost("/category-change-preview", PreviewAsync);
        products.MapPut("/primary-category", ReplacePrimaryAsync);
    }

    private static async Task<IResult> PreviewAsync(
        Guid productId,
        CategoryChangePreviewWriteModel body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var actorUserId = await auth.RequireAuthorizedAsync(http, cancellationToken);
        await CatalogActorRequestBinding.BindAsync(http, actorUserId, cancellationToken);
        return api.From(await sender.Send(new PreviewCategoryChangeQuery(productId, body), cancellationToken));
    }

    private static async Task<IResult> ReplacePrimaryAsync(
        Guid productId,
        CategoryChangeWriteModel body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var actorUserId = await auth.RequireAuthorizedAsync(http, cancellationToken);
        await CatalogActorRequestBinding.BindAsync(http, actorUserId, cancellationToken);
        return api.From(await sender.Send(new ReplacePrimaryCategoryCommand(productId, body), cancellationToken));
    }
}
