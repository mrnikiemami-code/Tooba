using Tooba.BuildingBlocks;

namespace Tooba.Host.Seller;

/// <summary>
/// مسیرهای HTTP پنل فروشنده. هویت Seller از هدر اجباری خوانده می‌شود؛ فیلتر UI مرجع نیست.
/// </summary>
public static class SellerPanelEndpoints
{
    /// <summary>
    /// هدر هویت Party فروشنده برای مرز API.
    /// </summary>
    public const string SellerPartyHeader = "X-Tooba-Seller-Party-Id";

    /// <summary>
    /// مسیرهای Seller Panel را ثبت می‌کند.
    /// </summary>
    public static void MapSellerPanelEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/seller");
        group.MapGet("/dashboard", GetDashboardAsync);
        group.MapGet("/offers", ListOffersAsync);
        group.MapGet("/offers/{offerId:guid}", GetOfferAsync);
        group.MapPatch("/offers/{offerId:guid}", PatchOfferAsync);
        group.MapGet("/orders", ListOrdersAsync);
        group.MapGet("/orders/{sellerOrderId:guid}", GetOrderAsync);
    }

    private static Guid RequireSellerPartyId(HttpRequest request)
    {
        var raw = request.Headers[SellerPartyHeader].ToString();
        if (!Guid.TryParse(raw, out var sellerPartyId) || sellerPartyId == Guid.Empty)
        {
            throw new PlatformHttpException(400, "شناسهٔ فروشنده نامعتبر است.", "seller.identity.missing");
        }

        return sellerPartyId;
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);

    private static async Task<IResult> GetDashboardAsync(
        SellerPanelComposer composer,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var sellerPartyId = RequireSellerPartyId(request);
            var summary = await composer.GetDashboardAsync(sellerPartyId, cancellationToken);
            return Results.Json(summary);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> ListOffersAsync(
        SellerPanelComposer composer,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var sellerPartyId = RequireSellerPartyId(request);
            var items = await composer.ListOffersAsync(sellerPartyId, cancellationToken);
            return Results.Json(items);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> GetOfferAsync(
        Guid offerId,
        SellerPanelComposer composer,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var sellerPartyId = RequireSellerPartyId(request);
            var page = await composer.GetOfferAsync(sellerPartyId, offerId, cancellationToken);
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
        SellerPanelComposer composer,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var sellerPartyId = RequireSellerPartyId(request);
            var page = await composer.PatchOfferAsync(sellerPartyId, offerId, body, cancellationToken);
            return Results.Json(page);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> ListOrdersAsync(
        SellerPanelComposer composer,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var sellerPartyId = RequireSellerPartyId(request);
            var items = await composer.ListOrdersAsync(sellerPartyId, cancellationToken);
            return Results.Json(items);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> GetOrderAsync(
        Guid sellerOrderId,
        SellerPanelComposer composer,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var sellerPartyId = RequireSellerPartyId(request);
            var page = await composer.GetOrderAsync(sellerPartyId, sellerOrderId, cancellationToken);
            return page is null
                ? Results.Json(new { title = "سفارش فروشنده پیدا نشد.", errorCode = "seller.order.missing" }, statusCode: StatusCodes.Status404NotFound)
                : Results.Json(page);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }
}
