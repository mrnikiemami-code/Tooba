using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Offer.Application.Commands.CreateOffer;
using Tooba.Offer.Application.Commands.SetOfferInventory;
using Tooba.Offer.Application.Commands.SetOfferPrice;
using Tooba.Offer.Application.Commands.UpdateOffer;
using Tooba.Offer.Application.Queries.GetOffer;
using Tooba.Offer.Application.Queries.ListSellerOffers;
using Tooba.Offer.Contracts.Dtos;

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
        group.MapPost("/offers", CreateOfferAsync);
        group.MapGet("/offers/{offerId:guid}", GetOfferAsync);
        group.MapPatch("/offers/{offerId:guid}", PatchOfferAsync);
        group.MapMethods("/offers/{offerId:guid}/price", ["POST", "PUT"], WriteOfferPriceAsync);
        group.MapMethods("/offers/{offerId:guid}/inventory", ["POST", "PUT"], WriteOfferInventoryAsync);
    }

    private static async Task<IResult> ListOffersAsync(
        ISender sender,
        IOfferSellerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken token)
    {
        var (_, sellerId) = await authorizer.RequireAuthorizedAsync(context, token);
        var result = await sender.Send(new ListSellerOffersQuery(sellerId), token);
        return api.From(result);
    }

    private static async Task<IResult> CreateOfferAsync(
        SellerOfferCreateRequest body,
        ISender sender,
        IOfferSellerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken token)
    {
        var (_, sellerId) = await authorizer.RequireAuthorizedAsync(context, token);
        var result = await sender.Send(new CreateOfferCommand(
            body.CatalogVariantId, sellerId, SalesChannel.Marketplace, body.SellerSku,
            body.Status, body.ReturnPolicyChoice, body.CustomReturnWindowDays), token);
        if (result.IsFailure)
            return api.From(result);
        return api.Created($"/v1/seller/offers/{result.Value.OfferId}", result);
    }

    private static async Task<IResult> GetOfferAsync(
        Guid offerId,
        ISender sender,
        IOfferSellerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken token)
    {
        var (_, sellerId) = await authorizer.RequireAuthorizedAsync(context, token);
        var result = await sender.Send(new GetOfferQuery(offerId, sellerId), token);
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
        var (_, sellerId) = await authorizer.RequireAuthorizedAsync(context, token);
        var result = await sender.Send(new UpdateOfferCommand(
            offerId, sellerId, body.SellerSku, body.Status, body.ReturnPolicyChoice,
            body.CustomReturnWindowDays, body.MinimumOrderQuantity, body.MaximumOrderQuantity), token);
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
        var (_, sellerId) = await authorizer.RequireAuthorizedAsync(context, token);
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
        var (_, sellerId) = await authorizer.RequireAuthorizedAsync(context, token);
        var result = await sender.Send(new SetOfferInventoryCommand(
            offerId, sellerId, body.OnHand, body.Reason), token);
        return api.From(result);
    }
}
