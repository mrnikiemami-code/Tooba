using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Story.Application.Commands.Seller;
using Tooba.Story.Application.Queries.Seller;
using Tooba.Story.Endpoints.Models;

namespace Tooba.Story.Endpoints.Seller;

/// <summary>مرز HTTP فروشنده Story.</summary>
public static class StorySellerEndpoints
{
    /// <summary>مسیرهای Seller را ثبت می‌کند.</summary>
    public static void Map(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var seller = app.MapGroup("/v1/seller/stories");
        seller.MapGet("", SellerListAsync);
        seller.MapGet("/{id:guid}", SellerGetAsync);
        seller.MapPost("", SellerCreateAsync);
        seller.MapPut("/{id:guid}", SellerUpdateAsync);
        seller.MapPost("/{id:guid}/submit", SellerSubmitAsync);
        seller.MapPost("/{id:guid}/items", SellerAddItemAsync);
        seller.MapPut("/{id:guid}/items/{itemId:guid}", SellerUpdateItemAsync);
        seller.MapDelete("/{id:guid}/items/{itemId:guid}", SellerRemoveItemAsync);
        seller.MapPut("/{id:guid}/items/reorder", SellerReorderItemsAsync);
    }

    private static async Task<IResult> SellerListAsync(
        ISender sender,
        IStorySellerAuthorizer auth,
        HttpContext http,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await auth.RequireAuthorizedAsync(http, cancellationToken);
            var tenantId = StoryHttpErrors.RequireTenantId(tenant);
            return Results.Json(await sender.Send(
                new ListSellerStoriesQuery(tenantId, sellerPartyId), cancellationToken));
        }
        catch (Exception ex) when (ex is SemanticException or PlatformHttpException)
        {
            return StoryHttpErrors.From(ex, api);
        }
    }

    private static async Task<IResult> SellerGetAsync(
        Guid id,
        ISender sender,
        IStorySellerAuthorizer auth,
        HttpContext http,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await auth.RequireAuthorizedAsync(http, cancellationToken);
            var tenantId = StoryHttpErrors.RequireTenantId(tenant);
            var story = await sender.Send(new GetSellerStoryQuery(tenantId, sellerPartyId, id), cancellationToken);
            return story is null ? Results.NotFound() : Results.Json(story);
        }
        catch (Exception ex) when (ex is SemanticException or PlatformHttpException)
        {
            return StoryHttpErrors.From(ex, api);
        }
    }

    private static Task<IResult> SellerCreateAsync(
        CreateStoryBody body, ISender sender, IStorySellerAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, ApiResponseFactory api, CancellationToken cancellationToken) =>
        SellerMutationAsync(auth, http, tenant, api, cancellationToken,
            (tenantId, actor, sellerPartyId) => sender.Send(
                new CreateSellerStoryDraftCommand(tenantId, sellerPartyId, actor, StoryBodyMapping.ToCreate(body)),
                cancellationToken),
            StatusCodes.Status201Created);

    private static Task<IResult> SellerUpdateAsync(
        Guid id, UpdateStoryBody body, ISender sender, IStorySellerAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, ApiResponseFactory api, CancellationToken cancellationToken) =>
        SellerMutationAsync(auth, http, tenant, api, cancellationToken,
            (tenantId, _, sellerPartyId) => sender.Send(
                new UpdateSellerStoryCommand(tenantId, sellerPartyId, id, StoryBodyMapping.ToUpdate(body)),
                cancellationToken));

    private static Task<IResult> SellerSubmitAsync(
        Guid id, ISender sender, IStorySellerAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, ApiResponseFactory api, CancellationToken cancellationToken) =>
        SellerMutationAsync(auth, http, tenant, api, cancellationToken,
            (tenantId, actor, sellerPartyId) => sender.Send(
                new SubmitSellerStoryCommand(tenantId, sellerPartyId, id, actor), cancellationToken));

    private static Task<IResult> SellerAddItemAsync(
        Guid id, AddStoryItemBody body, ISender sender, IStorySellerAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, ApiResponseFactory api, CancellationToken cancellationToken) =>
        SellerMutationAsync(auth, http, tenant, api, cancellationToken,
            (tenantId, _, sellerPartyId) => sender.Send(
                new AddSellerStoryItemCommand(tenantId, sellerPartyId, id, StoryBodyMapping.ToAddItem(body)),
                cancellationToken),
            StatusCodes.Status201Created);

    private static Task<IResult> SellerUpdateItemAsync(
        Guid id, Guid itemId, UpdateStoryItemBody body, ISender sender, IStorySellerAuthorizer auth,
        HttpContext http, ICurrentTenant tenant, ApiResponseFactory api, CancellationToken cancellationToken) =>
        SellerMutationAsync(auth, http, tenant, api, cancellationToken,
            (tenantId, _, sellerPartyId) => sender.Send(
                new UpdateSellerStoryItemCommand(
                    tenantId, sellerPartyId, id, itemId, StoryBodyMapping.ToUpdateItem(body)),
                cancellationToken));

    private static Task<IResult> SellerRemoveItemAsync(
        Guid id, Guid itemId, ISender sender, IStorySellerAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, ApiResponseFactory api, CancellationToken cancellationToken) =>
        SellerMutationAsync(auth, http, tenant, api, cancellationToken,
            (tenantId, _, sellerPartyId) => sender.Send(
                new RemoveSellerStoryItemCommand(tenantId, sellerPartyId, id, itemId), cancellationToken));

    private static Task<IResult> SellerReorderItemsAsync(
        Guid id, ReorderStoryItemsBody body, ISender sender, IStorySellerAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, ApiResponseFactory api, CancellationToken cancellationToken) =>
        SellerMutationAsync(auth, http, tenant, api, cancellationToken,
            (tenantId, _, sellerPartyId) => sender.Send(
                new ReorderSellerStoryItemsCommand(tenantId, sellerPartyId, id, body.ItemIds), cancellationToken));

    private static async Task<IResult> SellerMutationAsync<T>(
        IStorySellerAuthorizer auth,
        HttpContext http,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken,
        Func<Guid, Guid, Guid, Task<T>> action,
        int successStatusCode = StatusCodes.Status200OK)
    {
        try
        {
            var (actorUserId, sellerPartyId) = await auth.RequireAuthorizedAsync(http, cancellationToken);
            var tenantId = StoryHttpErrors.RequireTenantId(tenant);
            var result = await action(tenantId, actorUserId, sellerPartyId);
            return Results.Json(result, statusCode: successStatusCode);
        }
        catch (Exception ex) when (ex is SemanticException or PlatformHttpException)
        {
            return StoryHttpErrors.From(ex, api);
        }
    }
}
