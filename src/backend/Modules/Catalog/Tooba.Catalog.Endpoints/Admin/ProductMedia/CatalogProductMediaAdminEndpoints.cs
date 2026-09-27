using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductMedia.Commands;
using Tooba.Catalog.Application.ProductMedia.Models;
using Tooba.Catalog.Application.ProductMedia.Queries;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Endpoints.Admin;

namespace Tooba.Catalog.Endpoints.Admin.ProductMedia;

/// <summary>Admin Product Media HTTP — Catalog-owned via MediatR + ApiResponseFactory.</summary>
public static class CatalogProductMediaAdminEndpoints
{
    /// <summary>Maps eight Admin product media routes under /v1/admin/products.</summary>
    public static void MapCatalogProductMediaAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/v1/admin/products/{productId:guid}");
        products.MapGet("/media", ListAsync);
        products.MapGet("/media/readiness", GetReadinessAsync);
        products.MapPost("/media", AttachAsync);
        products.MapPost("/media/placeholder", AttachPlaceholderAsync);
        products.MapPut("/media/order", ReorderAsync);
        products.MapPut("/media/{assetId:guid}/primary", SetPrimaryAsync);
        products.MapPatch("/media/{assetId:guid}", PatchAsync);
        products.MapDelete("/media/{assetId:guid}", DetachAsync);
    }

    private static async Task<IResult> ListAsync(
        Guid productId,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new GetProductMediaQuery(productId), cancellationToken));
    }

    private static async Task<IResult> GetReadinessAsync(
        Guid productId,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new GetProductMediaReadinessQuery(productId), cancellationToken));
    }

    private static async Task<IResult> AttachAsync(
        Guid productId,
        AttachProductMediaWriteModel body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var actorUserId = await auth.RequireAuthorizedAsync(http, cancellationToken);
        if (!CatalogWorkspaceMediaScope.AllowsCatalogEdit(http.Request))
        {
            return api.FromFailure(new SemanticError(CatalogErrorCodes.WorkspacePermissionDenied));
        }

        await CatalogActorRequestBinding.BindAsync(http, actorUserId, cancellationToken);
        var result = await sender.Send(new AttachProductMediaCommand(productId, body), cancellationToken);
        return ToCreatedList(api, result);
    }

    private static async Task<IResult> AttachPlaceholderAsync(
        Guid productId,
        AttachPlaceholderProductMediaWriteModel? body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var actorUserId = await auth.RequireAuthorizedAsync(http, cancellationToken);
        if (!CatalogWorkspaceMediaScope.AllowsCatalogEdit(http.Request))
        {
            return api.FromFailure(new SemanticError(CatalogErrorCodes.WorkspacePermissionDenied));
        }

        await CatalogActorRequestBinding.BindAsync(http, actorUserId, cancellationToken);
        var result = await sender.Send(
            new AttachPlaceholderProductMediaCommand(productId, body),
            cancellationToken);
        return ToCreatedList(api, result);
    }

    private static async Task<IResult> ReorderAsync(
        Guid productId,
        ReorderProductMediaWriteModel body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var actorUserId = await auth.RequireAuthorizedAsync(http, cancellationToken);
        if (!CatalogWorkspaceMediaScope.AllowsCatalogEdit(http.Request))
        {
            return api.FromFailure(new SemanticError(CatalogErrorCodes.WorkspacePermissionDenied));
        }

        await CatalogActorRequestBinding.BindAsync(http, actorUserId, cancellationToken);
        return api.From(await sender.Send(
            new ReorderProductMediaCommand(
                productId,
                new ReorderProductMediaWriteModel(body.OrderedMediaAssetIds ?? [])),
            cancellationToken));
    }

    private static async Task<IResult> SetPrimaryAsync(
        Guid productId,
        Guid assetId,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var actorUserId = await auth.RequireAuthorizedAsync(http, cancellationToken);
        if (!CatalogWorkspaceMediaScope.AllowsCatalogEdit(http.Request))
        {
            return api.FromFailure(new SemanticError(CatalogErrorCodes.WorkspacePermissionDenied));
        }

        await CatalogActorRequestBinding.BindAsync(http, actorUserId, cancellationToken);
        return api.From(await sender.Send(
            new SetPrimaryProductMediaCommand(productId, assetId),
            cancellationToken));
    }

    private static async Task<IResult> PatchAsync(
        Guid productId,
        Guid assetId,
        PatchProductMediaWriteModel body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var actorUserId = await auth.RequireAuthorizedAsync(http, cancellationToken);
        if (!CatalogWorkspaceMediaScope.AllowsCatalogEdit(http.Request))
        {
            return api.FromFailure(new SemanticError(CatalogErrorCodes.WorkspacePermissionDenied));
        }

        await CatalogActorRequestBinding.BindAsync(http, actorUserId, cancellationToken);
        return api.From(await sender.Send(
            new PatchProductMediaCommand(productId, assetId, body),
            cancellationToken));
    }

    private static async Task<IResult> DetachAsync(
        Guid productId,
        Guid assetId,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var actorUserId = await auth.RequireAuthorizedAsync(http, cancellationToken);
        if (!CatalogWorkspaceMediaScope.AllowsCatalogEdit(http.Request))
        {
            return api.FromFailure(new SemanticError(CatalogErrorCodes.WorkspacePermissionDenied));
        }

        await CatalogActorRequestBinding.BindAsync(http, actorUserId, cancellationToken);
        return api.From(await sender.Send(
            new DetachProductMediaCommand(productId, assetId),
            cancellationToken));
    }

    /// <summary>
    /// Host returned 201 JSON without Location header for attach/placeholder — preserve that contract.
    /// </summary>
    private static IResult ToCreatedList(
        ApiResponseFactory api,
        Result<IReadOnlyList<ProductMediaItemView>> result)
    {
        if (result.IsFailure)
        {
            return api.From(result);
        }

        return Results.Json(result.Value, statusCode: StatusCodes.Status201Created);
    }
}

/// <summary>
/// Smallest module-owned policy for live <c>X-Tooba-Workspace-Scope</c> transport used by Product Workspace media writes.
/// </summary>
public static class CatalogWorkspaceMediaScope
{
    /// <summary>
    /// Returns false when header is <c>view</c> (Host ReadPermissions.CanEditCatalog=false); otherwise true.
    /// </summary>
    public static bool AllowsCatalogEdit(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var scope = request.Headers["X-Tooba-Workspace-Scope"].ToString();
        return !string.Equals(scope, "view", StringComparison.OrdinalIgnoreCase);
    }
}
