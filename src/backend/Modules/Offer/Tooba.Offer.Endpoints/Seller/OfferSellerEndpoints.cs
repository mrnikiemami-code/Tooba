using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Inventory.Contracts;
using Tooba.Offer.Application.Commands.CreateOffer;
using Tooba.Offer.Application.Commands.UpdateOffer;
using Tooba.Offer.Application.Queries.GetOffer;
using Tooba.Offer.Application.Queries.ListSellerOffers;
using Tooba.Offer.Contracts;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Pricing.Contracts;

namespace Tooba.Offer.Endpoints.Seller;

/// <summary>Maps thin seller Offer HTTP routes.</summary>
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

    private static async Task<IResult> ExecuteAsync(
        Func<Task<IResult>> action,
        HttpContext context,
        ApiResponseFactory responses)
    {
        try
        {
            return await action();
        }
        catch (Exception ex) when (ex is PlatformHttpException or SemanticException)
        {
            return responses.FromException(ex, context.Request.Headers.AcceptLanguage);
        }
    }

    private static Task<IResult> ListOffersAsync(
        ISender sender,
        IOfferSellerAuthorizer authorizer,
        ApiResponseFactory responses,
        HttpContext context,
        CancellationToken token) =>
        ExecuteAsync(async () =>
        {
            var (_, sellerId) = await authorizer.RequireAuthorizedAsync(context, token);
            return Results.Json(await sender.Send(new ListSellerOffersQuery(sellerId), token));
        }, context, responses);

    private static Task<IResult> CreateOfferAsync(
        SellerOfferCreateRequest body,
        ISender sender,
        IOfferSellerAuthorizer authorizer,
        ApiResponseFactory responses,
        HttpContext context,
        CancellationToken token) =>
        ExecuteAsync(async () =>
        {
            var (_, sellerId) = await authorizer.RequireAuthorizedAsync(context, token);
            var result = await sender.Send(new CreateOfferCommand(
                body.CatalogVariantId, sellerId, SalesChannel.Marketplace, body.SellerSku,
                body.Status, body.ReturnPolicyChoice, body.CustomReturnWindowDays), token);
            return Results.Json(result, statusCode: StatusCodes.Status201Created);
        }, context, responses);

    private static Task<IResult> GetOfferAsync(
        Guid offerId,
        ISender sender,
        IOfferSellerAuthorizer authorizer,
        ApiResponseFactory responses,
        HttpContext context,
        CancellationToken token) =>
        ExecuteAsync(async () =>
        {
            var (_, sellerId) = await authorizer.RequireAuthorizedAsync(context, token);
            return Results.Json(await sender.Send(new GetOfferQuery(offerId, sellerId), token));
        }, context, responses);

    private static Task<IResult> PatchOfferAsync(
        Guid offerId,
        SellerOfferPatchRequest body,
        ISender sender,
        IOfferSellerAuthorizer authorizer,
        ApiResponseFactory responses,
        HttpContext context,
        CancellationToken token) =>
        ExecuteAsync(async () =>
        {
            var (_, sellerId) = await authorizer.RequireAuthorizedAsync(context, token);
            return Results.Json(await sender.Send(new UpdateOfferCommand(
                offerId, sellerId, body.SellerSku, body.Status, body.ReturnPolicyChoice,
                body.CustomReturnWindowDays, body.MinimumOrderQuantity, body.MaximumOrderQuantity), token));
        }, context, responses);

    private static Task<IResult> WriteOfferPriceAsync(
        Guid offerId,
        SellerOfferPriceWriteRequest body,
        ISellerOfferPricingGateway pricing,
        ISender sender,
        IOfferSellerAuthorizer authorizer,
        ApiResponseFactory responses,
        HttpContext context,
        CancellationToken token) =>
        ExecuteAsync(async () =>
        {
            var (_, sellerId) = await authorizer.RequireAuthorizedAsync(context, token);
            await pricing.SetPriceAsync(new SetSellerOfferPrice(
                offerId, sellerId, body.Amount, body.Currency, body.Market), token);
            return Results.Json(await sender.Send(new GetOfferQuery(offerId, sellerId), token));
        }, context, responses);

    private static Task<IResult> WriteOfferInventoryAsync(
        Guid offerId,
        SellerOfferInventoryWriteRequest body,
        ISellerOfferInventoryGateway inventory,
        ISender sender,
        IOfferSellerAuthorizer authorizer,
        ApiResponseFactory responses,
        HttpContext context,
        CancellationToken token) =>
        ExecuteAsync(async () =>
        {
            var (_, sellerId) = await authorizer.RequireAuthorizedAsync(context, token);
            await inventory.SetInventoryAsync(new SetSellerOfferInventory(
                offerId, sellerId, body.OnHand, body.Reason), token);
            return Results.Json(await sender.Send(new GetOfferQuery(offerId, sellerId), token));
        }, context, responses);
}
