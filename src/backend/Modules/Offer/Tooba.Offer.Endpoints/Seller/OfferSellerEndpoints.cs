using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
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

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);

    private static IResult ToSemanticError(SemanticException ex, HttpContext context)
    {
        var status = ex.Error.Code switch
        {
            OfferErrorCodes.NotFound => StatusCodes.Status404NotFound,
            OfferErrorCodes.DuplicateActiveListing or OfferErrorCodes.DuplicateSellerSku
                or OfferErrorCodes.ArchivedCannotActivate => StatusCodes.Status409Conflict,
            PricingErrorCodes.AmountInvalid or InventoryErrorCodes.QuantityInvalid => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest,
        };
        return Results.Json(
            new { title = OfferEndpointLocalizer.Title(ex.Error, context.Request.Headers.AcceptLanguage), errorCode = ex.Error.Code },
            statusCode: status);
    }

    private static async Task<IResult> ExecuteAsync(Func<Task<IResult>> action, HttpContext context)
    {
        try { return await action(); }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (SemanticException ex) { return ToSemanticError(ex, context); }
    }

    private static Task<IResult> ListOffersAsync(
        ISender sender, IOfferSellerAuthorizer authorizer, HttpContext context, CancellationToken token) =>
        ExecuteAsync(async () =>
        {
            var (_, sellerId) = await authorizer.RequireAuthorizedAsync(context, token);
            return Results.Json(await sender.Send(new ListSellerOffersQuery(sellerId), token));
        }, context);

    private static Task<IResult> CreateOfferAsync(
        SellerOfferCreateRequest body, ISender sender, IOfferSellerAuthorizer authorizer,
        HttpContext context, CancellationToken token) =>
        ExecuteAsync(async () =>
        {
            var (_, sellerId) = await authorizer.RequireAuthorizedAsync(context, token);
            var result = await sender.Send(new CreateOfferCommand(
                body.CatalogVariantId, sellerId, SalesChannel.Marketplace, body.SellerSku,
                body.Status, body.ReturnPolicyChoice, body.CustomReturnWindowDays), token);
            return Results.Json(result, statusCode: StatusCodes.Status201Created);
        }, context);

    private static Task<IResult> GetOfferAsync(
        Guid offerId, ISender sender, IOfferSellerAuthorizer authorizer,
        HttpContext context, CancellationToken token) =>
        ExecuteAsync(async () =>
        {
            var (_, sellerId) = await authorizer.RequireAuthorizedAsync(context, token);
            return Results.Json(await sender.Send(new GetOfferQuery(offerId, sellerId), token));
        }, context);

    private static Task<IResult> PatchOfferAsync(
        Guid offerId, SellerOfferPatchRequest body, ISender sender, IOfferSellerAuthorizer authorizer,
        HttpContext context, CancellationToken token) =>
        ExecuteAsync(async () =>
        {
            var (_, sellerId) = await authorizer.RequireAuthorizedAsync(context, token);
            return Results.Json(await sender.Send(new UpdateOfferCommand(
                offerId, sellerId, body.SellerSku, body.Status, body.ReturnPolicyChoice,
                body.CustomReturnWindowDays, body.MinimumOrderQuantity, body.MaximumOrderQuantity), token));
        }, context);

    private static Task<IResult> WriteOfferPriceAsync(
        Guid offerId, SellerOfferPriceWriteRequest body, ISellerOfferPricingGateway pricing,
        ISender sender, IOfferSellerAuthorizer authorizer, HttpContext context, CancellationToken token) =>
        ExecuteAsync(async () =>
        {
            var (_, sellerId) = await authorizer.RequireAuthorizedAsync(context, token);
            await pricing.SetPriceAsync(new SetSellerOfferPrice(
                offerId, sellerId, body.Amount, body.Currency, body.Market), token);
            return Results.Json(await sender.Send(new GetOfferQuery(offerId, sellerId), token));
        }, context);

    private static Task<IResult> WriteOfferInventoryAsync(
        Guid offerId, SellerOfferInventoryWriteRequest body, ISellerOfferInventoryGateway inventory,
        ISender sender, IOfferSellerAuthorizer authorizer, HttpContext context, CancellationToken token) =>
        ExecuteAsync(async () =>
        {
            var (_, sellerId) = await authorizer.RequireAuthorizedAsync(context, token);
            await inventory.SetInventoryAsync(new SetSellerOfferInventory(
                offerId, sellerId, body.OnHand, body.Reason), token);
            return Results.Json(await sender.Send(new GetOfferQuery(offerId, sellerId), token));
        }, context);
}
