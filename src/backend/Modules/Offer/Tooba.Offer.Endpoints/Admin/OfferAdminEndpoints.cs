using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Offer.Application.Offers.Commands.CreateOffer;
using Tooba.Offer.Application.Offers.Commands.SetOfferInventory;
using Tooba.Offer.Application.Offers.Commands.SetOfferPrice;
using Tooba.Offer.Application.Offers.Commands.UpdateOffer;
using Tooba.Offer.Application.Offers.Queries.FindActiveOffer;
using Tooba.Offer.Application.Offers.Queries.GetOffer;
using Tooba.Offer.Application.Offers.Queries.QueryOffers;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Errors;

#pragma warning disable CS1591
namespace Tooba.Offer.Endpoints.Admin;

/// <summary>Admin-managed Offer create/read/edit on the existing admin prefix.</summary>
public static class OfferAdminEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapPost("/offers/query", QueryAsync);
        group.MapGet("/offers/active-listing", FindActiveAsync);
        group.MapPost("/offers", CreateAsync);
        group.MapGet("/offers/{offerId:guid}", GetAsync);
        group.MapPatch("/offers/{offerId:guid}", PatchAsync);
        group.MapMethods("/offers/{offerId:guid}/price", ["POST", "PUT"], PriceAsync);
        group.MapMethods("/offers/{offerId:guid}/inventory", ["POST", "PUT"], InventoryAsync);
    }

    private static async Task<IResult> QueryAsync(
        GridQueryRequest body,
        ISender sender,
        IOfferAdminAuthorizer authorizer,
        ApiResponseFactory api,
        IRequestLocaleResolver localeResolver,
        HttpContext context,
        CancellationToken token)
    {
        await authorizer.RequireViewAsync(context, token);
        return api.From(await sender.Send(new QueryAdminOffersQuery(body, localeResolver.ResolveRequestLocale(context).Name), token));
    }

    private static async Task<IResult> FindActiveAsync(
        Guid sellerPartyId,
        Guid catalogVariantId,
        ISender sender,
        IOfferAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken token)
    {
        await authorizer.RequireViewAsync(context, token);
        return api.From(await sender.Send(
            new FindActiveOfferQuery(sellerPartyId, catalogVariantId, SalesChannel.Marketplace), token));
    }

    private static async Task<IResult> CreateAsync(
        AdminOfferCreateRequest body,
        ISender sender,
        IOfferAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken token)
    {
        await authorizer.RequireCreateAsync(context, token);
        var result = await sender.Send(new CreateOfferCommand(
            body.CatalogVariantId,
            body.SellerPartyId,
            SalesChannel.Marketplace,
            body.SellerSku,
            body.Status,
            RequestId: body.RequestId), token);
        if (result.IsFailure) return api.From(result);
        return api.Created($"/v1/admin/offers/{result.Value.OfferId}", result);
    }

    private static async Task<IResult> GetAsync(
        Guid offerId,
        ISender sender,
        IOfferAdminAuthorizer authorizer,
        ApiResponseFactory api,
        IRequestLocaleResolver localeResolver,
        HttpContext context,
        CancellationToken token)
    {
        await authorizer.RequireViewAsync(context, token);
        return api.From(await sender.Send(new GetOfferForAdminQuery(offerId, localeResolver.ResolveRequestLocale(context).Name), token));
    }

    private static async Task<IResult> PatchAsync(
        Guid offerId,
        SellerOfferPatchRequest body,
        ISender sender,
        IOfferAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken token)
    {
        await authorizer.RequireEditAsync(context, token);
        var owner = await SellerOfAsync(sender, offerId, token);
        if (owner.IsFailure) return api.From(owner);
        var result = await sender.Send(new UpdateOfferCommand(
            offerId, owner.Value, body.SellerSku, body.Status, body.ReturnPolicyChoice,
            body.CustomReturnWindowDays, body.MinimumOrderQuantity, body.MaximumOrderQuantity,
            body.ClearOrderQuantityLimits), token);
        return api.From(result);
    }

    private static async Task<IResult> PriceAsync(
        Guid offerId,
        SellerOfferPriceWriteRequest body,
        ISender sender,
        IOfferAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken token)
    {
        await authorizer.RequireEditAsync(context, token);
        var owner = await SellerOfAsync(sender, offerId, token);
        if (owner.IsFailure) return api.From(owner);
        var result = await sender.Send(new SetOfferPriceCommand(
            offerId, owner.Value, body.Amount, body.Currency, body.Market), token);
        return api.From(result);
    }

    private static async Task<IResult> InventoryAsync(
        Guid offerId,
        SellerOfferInventoryWriteRequest body,
        ISender sender,
        IOfferAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken token)
    {
        await authorizer.RequireEditAsync(context, token);
        var owner = await SellerOfAsync(sender, offerId, token);
        if (owner.IsFailure) return api.From(owner);
        var result = await sender.Send(new SetOfferInventoryCommand(
            offerId, owner.Value, body.OnHand, body.Reason, body.StockItemId), token);
        return api.From(result);
    }

    private static async Task<Tooba.BuildingBlocks.Results.Result<Guid>> SellerOfAsync(
        ISender sender,
        Guid offerId,
        CancellationToken token)
    {
        var detail = await sender.Send(new GetOfferForAdminQuery(offerId), token);
        if (detail.IsFailure) return Tooba.BuildingBlocks.Results.Result<Guid>.Failure(detail.Errors);
        return Tooba.BuildingBlocks.Results.Result<Guid>.Success(detail.Value.SellerPartyId);
    }
}

