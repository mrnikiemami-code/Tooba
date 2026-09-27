using MediatR;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Catalog.Application.StoreMenus.Commands;
using Tooba.Catalog.Application.StoreMenus.Models;
using Tooba.Catalog.Application.StoreMenus.Queries;
using Tooba.Catalog.Endpoints.Admin;

namespace Tooba.Catalog.Endpoints.Admin.StoreMenus;

/// <summary>Admin Store Menu HTTP — Catalog-owned via MediatR + ApiResponseFactory.</summary>
public static class CatalogStoreMenuAdminEndpoints
{
    /// <summary>Maps /v1/admin/menus* routes.</summary>
    public static void MapCatalogStoreMenuAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/v1/admin/menus");
        admin.MapGet("/", ListAsync);
        admin.MapPost("/", CreateAsync);
        admin.MapGet("/header", GetHeaderAdminAsync);
        admin.MapPut("/header", SetHeaderAsync);
        admin.MapGet("/{menuId:guid}", GetAsync);
        admin.MapPut("/{menuId:guid}", UpdateAsync);
        admin.MapPut("/{menuId:guid}/enabled", SetEnabledAsync);
        admin.MapGet("/{menuId:guid}/usage", UsageAsync);
        admin.MapDelete("/{menuId:guid}", DeleteAsync);
        admin.MapPost("/{menuId:guid}/items", AddItemAsync);
        admin.MapPut("/{menuId:guid}/items/reorder", ReorderAsync);
        admin.MapPut("/{menuId:guid}/items/{itemId:guid}", UpdateItemAsync);
        admin.MapPut("/{menuId:guid}/items/{itemId:guid}/enabled", SetItemEnabledAsync);
        admin.MapDelete("/{menuId:guid}/items/{itemId:guid}", DeleteItemAsync);
    }

    private static async Task<IResult> ListAsync(
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new ListStoreMenusQuery(), cancellationToken));
    }

    private static async Task<IResult> GetAsync(
        Guid menuId,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new GetStoreMenuQuery(menuId), cancellationToken));
    }

    private static async Task<IResult> CreateAsync(
        StoreMenuWriteRequest body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new CreateStoreMenuAdminCommand(body), cancellationToken));
    }

    private static async Task<IResult> UpdateAsync(
        Guid menuId,
        StoreMenuWriteRequest body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new UpdateStoreMenuAdminCommand(menuId, body), cancellationToken));
    }

    private static async Task<IResult> SetEnabledAsync(
        Guid menuId,
        StoreMenuEnabledRequest body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new SetStoreMenuEnabledAdminCommand(menuId, body.IsEnabled),
            cancellationToken));
    }

    private static async Task<IResult> UsageAsync(
        Guid menuId,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new GetStoreMenuUsageQuery(menuId), cancellationToken));
    }

    private static async Task<IResult> DeleteAsync(
        Guid menuId,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new DeleteStoreMenuAdminCommand(menuId), cancellationToken));
    }

    private static async Task<IResult> AddItemAsync(
        Guid menuId,
        StoreMenuItemWriteRequest body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new AddStoreMenuItemAdminCommand(menuId, body), cancellationToken));
    }

    private static async Task<IResult> UpdateItemAsync(
        Guid menuId,
        Guid itemId,
        StoreMenuItemWriteRequest body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new UpdateStoreMenuItemAdminCommand(menuId, itemId, body),
            cancellationToken));
    }

    private static async Task<IResult> SetItemEnabledAsync(
        Guid menuId,
        Guid itemId,
        StoreMenuEnabledRequest body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new SetStoreMenuItemEnabledAdminCommand(menuId, itemId, body.IsEnabled),
            cancellationToken));
    }

    private static async Task<IResult> DeleteItemAsync(
        Guid menuId,
        Guid itemId,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new DeleteStoreMenuItemAdminCommand(menuId, itemId),
            cancellationToken));
    }

    private static async Task<IResult> ReorderAsync(
        Guid menuId,
        StoreMenuItemReorderRequest body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new ReorderStoreMenuItemsAdminCommand(menuId, body.ItemIds),
            cancellationToken));
    }

    private static async Task<IResult> GetHeaderAdminAsync(
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(new GetStoreHeaderMenuSelectionQuery(), cancellationToken));
    }

    private static async Task<IResult> SetHeaderAsync(
        StoreHeaderMenuWriteRequest body,
        ISender sender,
        ApiResponseFactory api,
        ICatalogAdminAuthorizer auth,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        await auth.RequireAuthorizedAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new SetStoreHeaderMenuAdminCommand(body.HeaderMenuId),
            cancellationToken));
    }
}
