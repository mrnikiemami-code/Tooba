using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.Story.Application.Stories.Commands.Seller;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Application.Stories.Queries.Seller;
using Tooba.Story.Application.Stories.Queries.Shared;
using Tooba.Story.Endpoints.Errors;
using Tooba.Story.Endpoints.Permissions;
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
        seller.MapPost("/query", SellerQueryAsync);
        seller.MapGet("/locale-options", SellerLocaleOptionsAsync);
        seller.MapGet("/product-options", SellerProductOptionsAsync);
        seller.MapGet("/{id:guid}", SellerGetAsync);
        seller.MapPost("", SellerCreateAsync);
        seller.MapPut("/{id:guid}", SellerUpdateAsync);
        seller.MapDelete("/{id:guid}", SellerDeleteAsync);
        seller.MapPost("/{id:guid}/submit", SellerSubmitAsync);
        seller.MapPost("/{id:guid}/return-to-draft", SellerReturnToDraftAsync);
        seller.MapPost("/{id:guid}/stop", SellerStopAsync);
        seller.MapPost("/{id:guid}/items", SellerAddItemAsync);
        seller.MapPut("/{id:guid}/items/{itemId:guid}", SellerUpdateItemAsync);
        seller.MapDelete("/{id:guid}/items/{itemId:guid}", SellerRemoveItemAsync);
        seller.MapPut("/{id:guid}/items/reorder", SellerReorderItemsAsync);
    }

    private static async Task<IResult> SellerQueryAsync(
        GridQueryRequest? body,
        ISender sender,
        IStorySellerAuthorizer auth,
        HttpContext http,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var (_, sellerPartyId) = await auth.RequireAuthorizedAsync(http, StoryPermissionIds.View, cancellationToken);
        var tenantResult = StoryHttpErrors.ResolveTenantId(tenant);
        if (tenantResult.IsFailure)
            return api.From(tenantResult);
        var request = body ?? new GridQueryRequest(1, 50, null, [], [], null);
        return api.From(await sender.Send(
            new QuerySellerStoriesQuery(tenantResult.Value, sellerPartyId, request),
            cancellationToken));
    }

    private static async Task<IResult> SellerLocaleOptionsAsync(
        ISender sender,
        IStorySellerAuthorizer auth,
        HttpContext http,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, StoryPermissionIds.View, cancellationToken);
        return api.From(await sender.Send(new ListStoryLocaleOptionsQuery(), cancellationToken));
    }

    private static async Task<IResult> SellerProductOptionsAsync(
        string? search,
        int? page,
        int? pageSize,
        ISender sender,
        IStorySellerAuthorizer auth,
        HttpContext http,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var (_, sellerPartyId) = await auth.RequireAuthorizedAsync(http, StoryPermissionIds.View, cancellationToken);
        return api.From(await sender.Send(
            new ListStoryProductOptionsQuery(
                sellerPartyId,
                search,
                page ?? 1,
                pageSize ?? 20),
            cancellationToken));
    }

    private static async Task<IResult> SellerListAsync(
        ISender sender,
        IStorySellerAuthorizer auth,
        HttpContext http,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var (_, sellerPartyId) = await auth.RequireAuthorizedAsync(http, StoryPermissionIds.View, cancellationToken);
        var tenantResult = StoryHttpErrors.ResolveTenantId(tenant);
        if (tenantResult.IsFailure)
            return api.From(tenantResult);
        return api.From(await sender.Send(
            new ListSellerStoriesQuery(tenantResult.Value, sellerPartyId), cancellationToken));
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
        var (_, sellerPartyId) = await auth.RequireAuthorizedAsync(http, StoryPermissionIds.View, cancellationToken);
        var tenantResult = StoryHttpErrors.ResolveTenantId(tenant);
        if (tenantResult.IsFailure)
            return api.From(tenantResult);
        return api.From(await sender.Send(
            new GetSellerStoryQuery(tenantResult.Value, sellerPartyId, id), cancellationToken));
    }

    private static Task<IResult> SellerCreateAsync(
        CreateStoryBody body, ISender sender, IStorySellerAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, ApiResponseFactory api, CancellationToken cancellationToken) =>
        SellerMutationAsync(auth, http, tenant, api, StoryPermissionIds.Create, cancellationToken,
            (tenantId, actor, sellerPartyId) => sender.Send(
                new CreateSellerStoryDraftCommand(tenantId, sellerPartyId, actor, StoryBodyMapping.ToCreate(body)),
                cancellationToken),
            createdLocation: result => $"/v1/seller/stories/{result.StoryId}");

    private static Task<IResult> SellerUpdateAsync(
        Guid id, UpdateStoryBody body, ISender sender, IStorySellerAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, ApiResponseFactory api, CancellationToken cancellationToken) =>
        SellerMutationAsync(auth, http, tenant, api, StoryPermissionIds.Edit, cancellationToken,
            (tenantId, _, sellerPartyId) => sender.Send(
                new UpdateSellerStoryCommand(tenantId, sellerPartyId, id, StoryBodyMapping.ToUpdate(body)),
                cancellationToken));

    private static Task<IResult> SellerDeleteAsync(
        Guid id,
        [FromBody] ReviewedStoryActionBody? body,
        ISender sender,
        IStorySellerAuthorizer auth,
        HttpContext http,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken) =>
        SellerMutationAsync(auth, http, tenant, api, StoryPermissionIds.Edit, cancellationToken,
            (tenantId, _, sellerPartyId) => sender.Send(
                new DeleteSellerStoryCommand(
                    tenantId, sellerPartyId, id, body?.ExpectedVersion, body?.OccurrenceKey),
                cancellationToken));

    private static Task<IResult> SellerSubmitAsync(
        Guid id, ReviewedStoryActionBody? body, ISender sender, IStorySellerAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, ApiResponseFactory api, CancellationToken cancellationToken) =>
        SellerMutationAsync(auth, http, tenant, api, StoryPermissionIds.Submit, cancellationToken,
            (tenantId, actor, sellerPartyId) => sender.Send(
                new SubmitSellerStoryCommand(
                    tenantId, sellerPartyId, id, actor, body?.ExpectedVersion, body?.OccurrenceKey),
                cancellationToken));

    private static Task<IResult> SellerReturnToDraftAsync(
        Guid id, ReviewedStoryActionBody? body, ISender sender, IStorySellerAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, ApiResponseFactory api, CancellationToken cancellationToken) =>
        SellerMutationAsync(auth, http, tenant, api, StoryPermissionIds.Edit, cancellationToken,
            (tenantId, _, sellerPartyId) => sender.Send(
                new ReturnSellerStoryToDraftCommand(
                    tenantId, sellerPartyId, id, body?.ExpectedVersion, body?.OccurrenceKey),
                cancellationToken));

    private static Task<IResult> SellerStopAsync(
        Guid id, ReviewedStoryActionBody? body, ISender sender, IStorySellerAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, ApiResponseFactory api, CancellationToken cancellationToken) =>
        SellerMutationAsync(auth, http, tenant, api, StoryPermissionIds.Edit, cancellationToken,
            (tenantId, _, sellerPartyId) => sender.Send(
                new StopSellerStoryCommand(
                    tenantId, sellerPartyId, id, body?.ExpectedVersion, body?.OccurrenceKey),
                cancellationToken));

    private static Task<IResult> SellerAddItemAsync(
        Guid id, AddStoryItemBody body, ISender sender, IStorySellerAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, ApiResponseFactory api, CancellationToken cancellationToken) =>
        SellerMutationAsync(auth, http, tenant, api, StoryPermissionIds.Edit, cancellationToken,
            (tenantId, _, sellerPartyId) => sender.Send(
                new AddSellerStoryItemCommand(tenantId, sellerPartyId, id, StoryBodyMapping.ToAddItem(body)),
                cancellationToken),
            createdLocation: result => $"/v1/seller/stories/{result.StoryId}");

    private static Task<IResult> SellerUpdateItemAsync(
        Guid id, Guid itemId, UpdateStoryItemBody body, ISender sender, IStorySellerAuthorizer auth,
        HttpContext http, ICurrentTenant tenant, ApiResponseFactory api, CancellationToken cancellationToken) =>
        SellerMutationAsync(auth, http, tenant, api, StoryPermissionIds.Edit, cancellationToken,
            (tenantId, _, sellerPartyId) => sender.Send(
                new UpdateSellerStoryItemCommand(
                    tenantId, sellerPartyId, id, itemId, StoryBodyMapping.ToUpdateItem(body)),
                cancellationToken));

    private static Task<IResult> SellerRemoveItemAsync(
        Guid id,
        Guid itemId,
        [FromBody] ReviewedStoryActionBody? body,
        ISender sender,
        IStorySellerAuthorizer auth,
        HttpContext http,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken) =>
        SellerMutationAsync(auth, http, tenant, api, StoryPermissionIds.Edit, cancellationToken,
            (tenantId, _, sellerPartyId) => sender.Send(
                new RemoveSellerStoryItemCommand(
                    tenantId, sellerPartyId, id, itemId, body?.ExpectedVersion, body?.OccurrenceKey),
                cancellationToken));

    private static Task<IResult> SellerReorderItemsAsync(
        Guid id, ReorderStoryItemsBody body, ISender sender, IStorySellerAuthorizer auth, HttpContext http,
        ICurrentTenant tenant, ApiResponseFactory api, CancellationToken cancellationToken) =>
        SellerMutationAsync(auth, http, tenant, api, StoryPermissionIds.Edit, cancellationToken,
            (tenantId, _, sellerPartyId) => sender.Send(
                new ReorderSellerStoryItemsCommand(
                    tenantId, sellerPartyId, id, body.ItemIds, body.ExpectedVersion, body.OccurrenceKey),
                cancellationToken));

    private static async Task<IResult> SellerMutationAsync(
        IStorySellerAuthorizer auth,
        HttpContext http,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        string permissionId,
        CancellationToken cancellationToken,
        Func<Guid, Guid, Guid, Task<Result<AdminStorySnapshot>>> action,
        Func<AdminStorySnapshot, string>? createdLocation = null)
    {
        var (actorUserId, sellerPartyId) = await auth.RequireAuthorizedAsync(http, permissionId, cancellationToken);
        var tenantResult = StoryHttpErrors.ResolveTenantId(tenant);
        if (tenantResult.IsFailure)
            return api.From(tenantResult);
        var result = await action(tenantResult.Value, actorUserId, sellerPartyId);
        if (createdLocation is not null && result.IsSuccess)
            return api.Created(createdLocation(result.Value), result);
        return api.From(result);
    }
}
