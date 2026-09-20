using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.Offer.Application;
using Tooba.Offer.Contracts;

namespace Tooba.Offer.Endpoints;

/// <summary>
/// نگاشت HTTP مسیرهای Offer پنل فروشنده؛ تصمیم تجاری و persistence ندارد.
/// </summary>
public static class OfferSellerEndpoints
{
    /// <summary>
    /// مسیرهای Offer را روی گروه seller ثبت می‌کند.
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

    private static async Task<IResult> ListOffersAsync(
        IOfferSellerPanel panel,
        IOfferSellerAuthorizer authorizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await authorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
            var items = await panel.ListOffersAsync(sellerPartyId, cancellationToken);
            return Results.Json(items);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> CreateOfferAsync(
        SellerOfferCreateRequest body,
        IOfferSellerPanel panel,
        IOfferSellerAuthorizer authorizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await authorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
            var page = await panel.CreateOfferAsync(sellerPartyId, body, cancellationToken);
            return Results.Json(page, statusCode: StatusCodes.Status201Created);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> GetOfferAsync(
        Guid offerId,
        IOfferSellerPanel panel,
        IOfferSellerAuthorizer authorizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await authorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
            var page = await panel.GetOfferAsync(sellerPartyId, offerId, cancellationToken);
            return page is null
                ? Results.Json(new { title = "پیشنهاد پیدا نشد.", errorCode = "seller.offer.missing" }, statusCode: StatusCodes.Status404NotFound)
                : Results.Json(page);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> PatchOfferAsync(
        Guid offerId,
        SellerOfferPatchRequest body,
        IOfferSellerPanel panel,
        IOfferSellerAuthorizer authorizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await authorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
            var page = await panel.PatchOfferAsync(sellerPartyId, offerId, body, cancellationToken);
            return Results.Json(page);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
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
    }
}
