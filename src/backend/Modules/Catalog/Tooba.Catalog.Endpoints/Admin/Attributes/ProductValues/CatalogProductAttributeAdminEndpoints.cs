using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application.Attributes.ProductValues.Commands;
using Tooba.Catalog.Application.Attributes.ProductValues.Models;
using Tooba.Catalog.Application.Attributes.ProductValues.Queries;
using Tooba.Catalog.Endpoints.Admin;

namespace Tooba.Catalog.Endpoints.Admin.Attributes.ProductValues;

/// <summary>Admin Catalog Product Attribute Editor/Readiness HTTP — Catalog-owned via MediatR + ApiResponseFactory.</summary>
public static class CatalogProductAttributeAdminEndpoints
{
    /// <summary>Maps four Admin product-attribute routes.</summary>
    public static void MapCatalogProductAttributeAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/v1/admin/catalog/products/{productId:guid}");
        products.MapGet("/attributes", GetEditorStateAsync);
        products.MapPut("/attributes", SetAttributesAsync);
        products.MapGet("/attributes/readiness", GetReadinessAsync);
        products.MapPut("/attributes/{definitionId:guid}", SetAttributeAsync);
    }

    private static async Task<IResult> GetEditorStateAsync(
        Guid productId,
        string? locale,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var actorUserId = await auth.RequireAuthorizedAsync(http, cancellationToken);
        await CatalogActorRequestBinding.BindAsync(http, actorUserId, cancellationToken);
        return api.From(await sender.Send(new GetProductAttributeEditorStateQuery(productId, locale), cancellationToken));
    }

    private static async Task<IResult> SetAttributesAsync(
        Guid productId,
        SetProductAttributesWriteModel body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var actorUserId = await auth.RequireAuthorizedAsync(http, cancellationToken);
        await CatalogActorRequestBinding.BindAsync(http, actorUserId, cancellationToken);
        return api.From(await sender.Send(new SetProductAttributesCommand(productId, body), cancellationToken));
    }

    private static async Task<IResult> GetReadinessAsync(
        Guid productId,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var actorUserId = await auth.RequireAuthorizedAsync(http, cancellationToken);
        await CatalogActorRequestBinding.BindAsync(http, actorUserId, cancellationToken);
        return api.From(await sender.Send(new GetProductAttributeReadinessQuery(productId), cancellationToken));
    }

    private static async Task<IResult> SetAttributeAsync(
        Guid productId,
        Guid definitionId,
        SetProductAttributeWriteModel body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var actorUserId = await auth.RequireAuthorizedAsync(http, cancellationToken);
        await CatalogActorRequestBinding.BindAsync(http, actorUserId, cancellationToken);
        return api.From(await sender.Send(
            new SetProductAttributeCommand(productId, definitionId, body),
            cancellationToken));
    }
}
