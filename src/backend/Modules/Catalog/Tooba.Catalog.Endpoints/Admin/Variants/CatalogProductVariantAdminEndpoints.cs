using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application.Variants.Commands;
using Tooba.Catalog.Application.Variants.Models;
using Tooba.Catalog.Application.Variants.Queries;
using Tooba.Catalog.Endpoints.Admin;

namespace Tooba.Catalog.Endpoints.Admin.Variants;

/// <summary>Admin Catalog Product Variant Axes + Matrix HTTP — Catalog-owned via MediatR + ApiResponseFactory.</summary>
public static class CatalogProductVariantAdminEndpoints
{
    /// <summary>Maps five Admin product-variant routes.</summary>
    public static void MapCatalogProductVariantAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/v1/admin/catalog/products/{productId:guid}");
        products.MapPut("/variant-axes", SetAxesAsync);
        products.MapGet("/variants/editor", GetEditorStateAsync);
        products.MapPost("/variants/preview", PreviewAsync);
        products.MapPut("/variants/apply", ApplyAsync);
        products.MapGet("/variants/readiness", GetReadinessAsync);
    }

    private static async Task<IResult> SetAxesAsync(
        Guid productId,
        SetProductVariantAxesWriteModel body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var actorUserId = await auth.RequireAuthorizedAsync(http, cancellationToken);
        await CatalogActorRequestBinding.BindAsync(http, actorUserId, cancellationToken);
        return api.From(await sender.Send(new SetProductVariantAxesCommand(productId, body), cancellationToken));
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
        return api.From(await sender.Send(new GetProductVariantEditorStateQuery(productId, locale), cancellationToken));
    }

    private static async Task<IResult> PreviewAsync(
        Guid productId,
        ProductVariantPreviewWriteModel body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var actorUserId = await auth.RequireAuthorizedAsync(http, cancellationToken);
        await CatalogActorRequestBinding.BindAsync(http, actorUserId, cancellationToken);
        return api.From(await sender.Send(new PreviewProductVariantsQuery(productId, body), cancellationToken));
    }

    private static async Task<IResult> ApplyAsync(
        Guid productId,
        ProductVariantApplyWriteModel body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var actorUserId = await auth.RequireAuthorizedAsync(http, cancellationToken);
        await CatalogActorRequestBinding.BindAsync(http, actorUserId, cancellationToken);
        return api.From(await sender.Send(new ApplyProductVariantMatrixCommand(productId, body), cancellationToken));
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
        return api.From(await sender.Send(new GetProductVariantReadinessQuery(productId), cancellationToken));
    }
}
