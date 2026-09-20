using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Application.Ports;
using Tooba.Offer.Application;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;

namespace Tooba.Offer.Endpoints.Seller;

/// <summary>
/// Maps seller Offer HTTP routes without business or persistence decisions.
/// </summary>
public static class OfferSellerEndpoints
{
    /// <summary>
    /// Maps Offer routes on the seller route group.
    /// </summary>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/offers", ListOffersAsync);
        group.MapPost("/offers", CreateOfferAsync);
        group.MapGet("/offers/{offerId:guid}", GetOfferAsync);
        group.MapPatch("/offers/{offerId:guid}", PatchOfferAsync);
        group.MapPost("/offers/{offerId:guid}/price", WriteOfferPriceAsync);
        group.MapPut("/offers/{offerId:guid}/price", WriteOfferPriceAsync);
        group.MapPost("/offers/{offerId:guid}/inventory", WriteOfferInventoryAsync);
        group.MapPut("/offers/{offerId:guid}/inventory", WriteOfferInventoryAsync);
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);

    private static IResult ToSemanticError(SemanticException ex, HttpContext httpContext)
    {
        var culture = httpContext.Request.Headers.AcceptLanguage.ToString();
        var title = OfferEndpointLocalizer.Title(ex.Error, culture);
        return Results.Json(new { title, errorCode = ex.Error.Code }, statusCode: StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> ListOffersAsync(
        ISender sender,
        IOfferSellerPanel panel,
        IOfferSellerAuthorizer authorizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await authorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
            await sender.Send(new ListSellerOffersQuery(sellerPartyId), cancellationToken);
            var items = await panel.ListOffersAsync(sellerPartyId, cancellationToken);
            return Results.Json(items);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (SemanticException ex)
        {
            return ToSemanticError(ex, httpContext);
        }
    }

    private static async Task<IResult> CreateOfferAsync(
        SellerOfferCreateRequest body,
        ISender sender,
        IOfferSellerPanel panel,
        IOfferSellerAuthorizer authorizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await authorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
            var created = await sender.Send(
                new CreateOfferCommand(body.CatalogVariantId, sellerPartyId, SalesChannel.Marketplace, body.SellerSku),
                cancellationToken);
            if (string.Equals(body.Status, nameof(OfferStatus.Active), StringComparison.OrdinalIgnoreCase))
                await sender.Send(new ActivateOfferCommand(created.OfferId), cancellationToken);
            if (body.ReturnPolicyChoice is not null || body.CustomReturnWindowDays is not null)
                await sender.Send(new SetReturnPolicyCommand(created.OfferId, body.ReturnPolicyChoice ?? "Default", body.CustomReturnWindowDays), cancellationToken);
            var page = await panel.GetOfferAsync(sellerPartyId, created.OfferId, cancellationToken);
            return Results.Json(page, statusCode: StatusCodes.Status201Created);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (SemanticException ex)
        {
            return ToSemanticError(ex, httpContext);
        }
    }

    private static async Task<IResult> GetOfferAsync(
        Guid offerId,
        ISender sender,
        IOfferSellerPanel panel,
        IOfferSellerAuthorizer authorizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await authorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
            var offer = await sender.Send(new GetOfferQuery(offerId), cancellationToken);
            if (offer is null || offer.SellerPartyId != sellerPartyId)
                return Results.Json(new { title = "Offer not found.", errorCode = "seller.offer.missing" }, statusCode: StatusCodes.Status404NotFound);
            var page = await panel.GetOfferAsync(sellerPartyId, offerId, cancellationToken);
            return page is null
                ? Results.Json(new { title = "Offer not found.", errorCode = "seller.offer.missing" }, statusCode: StatusCodes.Status404NotFound)
                : Results.Json(page);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (SemanticException ex)
        {
            return ToSemanticError(ex, httpContext);
        }
    }

    private static async Task<IResult> PatchOfferAsync(
        Guid offerId,
        SellerOfferPatchRequest body,
        ISender sender,
        IOfferSellerPanel panel,
        IOfferSellerAuthorizer authorizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await authorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
            await sender.Send(new UpdateOfferCommand(offerId, sellerPartyId, body.SellerSku, body.Status), cancellationToken);
            if (body.ReturnPolicyChoice is not null || body.CustomReturnWindowDays is not null)
                await sender.Send(new SetReturnPolicyCommand(offerId, body.ReturnPolicyChoice ?? "Default", body.CustomReturnWindowDays), cancellationToken);
            if (body.MinimumOrderQuantity is not null || body.MaximumOrderQuantity is not null)
                await sender.Send(new SetOrderQuantityLimitsCommand(offerId, body.MinimumOrderQuantity, body.MaximumOrderQuantity), cancellationToken);
            var page = await panel.GetOfferAsync(sellerPartyId, offerId, cancellationToken);
            return Results.Json(page);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (SemanticException ex)
        {
            return ToSemanticError(ex, httpContext);
        }
    }

    private static async Task<IResult> WriteOfferPriceAsync(
        Guid offerId,
        SellerOfferPriceWriteRequest body,
        IOfferSellerPanel panel,
        IOfferSellerAuthorizer authorizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await authorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
            var page = await panel.SetOfferPriceAsync(sellerPartyId, offerId, body, cancellationToken);
            return Results.Json(page);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (SemanticException ex)
        {
            return ToSemanticError(ex, httpContext);
        }
    }

    private static async Task<IResult> WriteOfferInventoryAsync(
        Guid offerId,
        SellerOfferInventoryWriteRequest body,
        IOfferSellerPanel panel,
        IOfferSellerAuthorizer authorizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await authorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
            var page = await panel.SetOfferInventoryAsync(sellerPartyId, offerId, body, cancellationToken);
            return Results.Json(page);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (SemanticException ex)
        {
            return ToSemanticError(ex, httpContext);
        }
    }
}
