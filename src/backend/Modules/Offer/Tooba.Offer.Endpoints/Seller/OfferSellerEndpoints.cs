using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Localization;
using Tooba.StoreContext.Contracts.Current;
using Tooba.Offer.Application.Offers.Commands.CreateOffer;
using Tooba.Offer.Application.Offers.Commands.SetOfferInventory;
using Tooba.Offer.Application.Offers.Commands.SetOfferPrice;
using Tooba.Offer.Application.Offers.Commands.UpdateOffer;
using Tooba.Offer.Application.Offers.Queries.FindActiveOffer;
using Tooba.Offer.Application.Offers.Queries.GetAllowedOfferChannels;
using Tooba.Offer.Application.Offers.Queries.GetOffer;
using Tooba.Offer.Application.Offers.Queries.GetOfferHistory;
using Tooba.Offer.Application.Offers.Queries.ListSellerOffers;
using Tooba.Offer.Application.Offers.Queries.QueryOffers;
using Tooba.BuildingBlocks.Grid;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Endpoints.Errors;

namespace Tooba.Offer.Endpoints.Seller;

/// <summary>
/// Thin seller Offer HTTP routes — success/failure mapped through <see cref="ApiResponseFactory"/>.
/// </summary>
public static class OfferSellerEndpoints
{
    /// <summary>Maps Offer routes on the seller route group.</summary>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/offers", ListOffersAsync);
        group.MapPost("/offers/query", QueryOffersAsync);
        group.MapGet("/offers/allowed-channels", AllowedChannelsAsync);
        group.MapGet("/offers/active-listing", FindActiveAsync);
        group.MapPost("/offers", CreateOfferAsync);
        group.MapGet("/offers/{offerId:guid}", GetOfferAsync);
        group.MapGet("/offers/{offerId:guid}/history", HistoryAsync);
        group.MapPatch("/offers/{offerId:guid}", PatchOfferAsync);
        group.MapMethods("/offers/{offerId:guid}/price", ["POST", "PUT"], WriteOfferPriceAsync);
        group.MapMethods("/offers/{offerId:guid}/inventory", ["POST", "PUT"], WriteOfferInventoryAsync);
    }

    private static async Task<IResult> ListOffersAsync(
        ISender sender,
        IOfferSellerAuthorizer authorizer,
        ApiResponseFactory api,
        IRequestLocaleResolver localeResolver,
        HttpContext context,
        CancellationToken token)
    {
        var (_, sellerId) = await authorizer.RequireAuthorizedAsync(context, token);
        var result = await sender.Send(new ListSellerOffersQuery(sellerId, localeResolver.ResolveRequestLocale(context).Name), token);
        return api.From(result);
    }

    private static async Task<IResult> QueryOffersAsync(
        GridQueryRequest body,
        ISender sender,
        IOfferSellerAuthorizer authorizer,
        ApiResponseFactory api,
        IRequestLocaleResolver localeResolver,
        HttpContext context,
        CancellationToken token)
    {
        var (_, sellerId) = await authorizer.RequireAuthorizedAsync(context, token);
        var result = await sender.Send(new QuerySellerOffersQuery(sellerId, body, localeResolver.ResolveRequestLocale(context).Name), token);
        return api.From(result);
    }

    private static async Task<IResult> AllowedChannelsAsync(
        ISender sender,
        IOfferSellerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken token)
    {
        await authorizer.RequireAuthorizedAsync(context, token);
        return api.From(await sender.Send(new GetAllowedOfferChannelsQuery(), token));
    }

    private static async Task<IResult> FindActiveAsync(
        Guid catalogVariantId,
        string? channel,
        ISender sender,
        IOfferSellerAuthorizer authorizer,
        ICurrentStoreCommerceContext storeCommerce,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken token)
    {
        var (_, sellerId) = await authorizer.RequireAuthorizedAsync(context, token);
        var resolved = OfferChannelRequestResolver.ResolveOptional(channel, storeCommerce.Current?.SalesChannel);
        return api.From(await sender.Send(
            new FindActiveOfferQuery(sellerId, catalogVariantId, resolved), token));
    }

    private static async Task<IResult> CreateOfferAsync(
        SellerOfferCreateRequest body,
        ISender sender,
        IOfferSellerAuthorizer authorizer,
        ICurrentStoreCommerceContext storeCommerce,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken token)
    {
        var (_, sellerId) = await authorizer.RequireCreateAsync(context, token);
        var channel = OfferChannelRequestResolver.ResolveRequired(body.Channel, storeCommerce.Current?.SalesChannel);
        var result = await sender.Send(new CreateOfferCommand(
            body.CatalogVariantId, sellerId, channel, body.SellerSku,
            body.Status, body.ReturnPolicyChoice, body.CustomReturnWindowDays, RequestId: body.RequestId), token);
        if (result.IsFailure)
            return api.From(result);
        return api.Created($"/v1/seller/offers/{result.Value.OfferId}", result);
    }


    private static async Task<IResult> GetOfferAsync(
        Guid offerId,
        ISender sender,
        IOfferSellerAuthorizer authorizer,
        ApiResponseFactory api,
        IRequestLocaleResolver localeResolver,
        HttpContext context,
        CancellationToken token)
    {
        var (_, sellerId) = await authorizer.RequireAuthorizedAsync(context, token);
        var result = await sender.Send(new GetOfferQuery(offerId, sellerId, localeResolver.ResolveRequestLocale(context).Name), token);
        return api.From(result);
    }

    private static async Task<IResult> HistoryAsync(
        Guid offerId,
        int? page,
        int? pageSize,
        ISender sender,
        IOfferSellerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken token)
    {
        var (_, sellerId) = await authorizer.RequireAuthorizedAsync(context, token);
        var result = await sender.Send(
            new GetOfferHistoryQuery(offerId, sellerId, page ?? 1, pageSize ?? 20), token);
        return api.From(result);
    }

    private static async Task<IResult> PatchOfferAsync(
        Guid offerId,
        SellerOfferPatchRequest body,
        ISender sender,
        IOfferSellerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken token)
    {
        var (_, sellerId) = await authorizer.RequireEditAsync(context, token);
        var result = await sender.Send(new UpdateOfferCommand(
            offerId, sellerId, body.SellerSku, body.Status, body.ReturnPolicyChoice,
            body.CustomReturnWindowDays, body.MinimumOrderQuantity, body.MaximumOrderQuantity,
            body.ClearOrderQuantityLimits), token);
        return api.From(result);
    }

    private static async Task<IResult> WriteOfferPriceAsync(
        Guid offerId,
        SellerOfferPriceWriteRequest body,
        ISender sender,
        IOfferSellerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken token)
    {
        var (_, sellerId) = await authorizer.RequireEditAsync(context, token);
        var result = await sender.Send(new SetOfferPriceCommand(
            offerId, sellerId, body.Amount, body.Currency, body.Market), token);
        return api.From(result);
    }

    private static async Task<IResult> WriteOfferInventoryAsync(
        Guid offerId,
        SellerOfferInventoryWriteRequest body,
        ISender sender,
        IOfferSellerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken token)
    {
        var (_, sellerId) = await authorizer.RequireEditAsync(context, token);
        var result = await sender.Send(new SetOfferInventoryCommand(
            offerId, sellerId, body.OnHand, body.Reason, body.StockItemId), token);
        return api.From(result);
    }
}



