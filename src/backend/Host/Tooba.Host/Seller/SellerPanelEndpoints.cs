using Tooba.BuildingBlocks;

namespace Tooba.Host.Seller;

/// <summary>
/// مسیرهای HTTP پنل فروشنده. مجوز از Actor احرازشده و SpiceDB/موتور مجوز می‌آید؛ هدر Seller فقط زمینه است.
/// </summary>
public static class SellerPanelEndpoints
{
    /// <summary>
    /// هدر زمینهٔ Party فروشنده (مرجع مجوز نیست).
    /// </summary>
    public const string SellerPartyHeader = SellerPanelAccess.SellerPartyHeader;

    /// <summary>
    /// هدر Actor محدود Development.
    /// </summary>
    public const string DevActorHeader = SellerPanelAccess.DevActorHeader;

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
        group.MapGet("/dev-contexts", GetDevContexts);
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);

    private static async Task<IResult> GetDashboardAsync(
        SellerPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
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
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
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
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
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
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
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
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
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
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
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

    private static IResult GetDevContexts(IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            return Results.Json(new { title = "Not Found", errorCode = "seller.dev.unavailable" }, statusCode: StatusCodes.Status404NotFound);
        }

        var snapshot = SellerDevActorBootstrap.Snapshot;
        if (snapshot is null)
        {
            return Results.Json(new { title = "در دسترس نیست", errorCode = "seller.dev.not-ready" }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Json(new
        {
            actors = new[]
            {
                new
                {
                    actorUserId = snapshot.ActorA.ActorUserId,
                    actorLabel = snapshot.ActorA.ActorLabel,
                    sellerPartyId = snapshot.ActorA.SellerPartyId,
                    sellerLabel = snapshot.ActorA.SellerLabel,
                },
                new
                {
                    actorUserId = snapshot.ActorB.ActorUserId,
                    actorLabel = snapshot.ActorB.ActorLabel,
                    sellerPartyId = snapshot.ActorB.SellerPartyId,
                    sellerLabel = snapshot.ActorB.SellerLabel,
                },
            },
        });
    }
}
