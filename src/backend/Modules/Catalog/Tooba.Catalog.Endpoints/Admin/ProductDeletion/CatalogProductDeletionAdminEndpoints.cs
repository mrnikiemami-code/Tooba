using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductDeletion.Commands;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Endpoints.Admin;

namespace Tooba.Catalog.Endpoints.Admin.ProductDeletion;

/// <summary>Admin product DELETE — Catalog-owned 204 / 409 via MediatR + ApiResponseFactory.</summary>
public static class CatalogProductDeletionAdminEndpoints
{
    /// <summary>Maps DELETE /v1/admin/products/{productId}.</summary>
    public static void MapCatalogProductDeletionAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/v1/admin/products");
        products.MapDelete("/{productId:guid}", DeleteAsync);
    }

    private static async Task<IResult> DeleteAsync(
        Guid productId,
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
        return api.From(await sender.Send(new DeleteProductCommand(productId), cancellationToken));
    }
}
