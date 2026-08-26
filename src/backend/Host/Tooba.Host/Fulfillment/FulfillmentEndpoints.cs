using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Host.Admin;
using Tooba.Host.Seller;

namespace Tooba.Host.Fulfillment;

/// <summary>
/// HTTP fulfillment برای seller/admin/customer با فیلتر مجوز در سرور.
/// </summary>
public static class FulfillmentEndpoints
{
    /// <summary>
    /// مسیرهای fulfillment را ثبت می‌کند.
    /// </summary>
    public static void MapFulfillmentEndpoints(this WebApplication app)
    {
        var seller = app.MapGroup("/v1/seller");
        seller.MapGet("/fulfillments", SellerListAsync);
        seller.MapGet("/fulfillments/{fulfillmentId:guid}", SellerGetAsync);
        seller.MapPost("/fulfillments/{fulfillmentId:guid}/processing", SellerProcessingAsync);
        seller.MapPost("/fulfillments/{fulfillmentId:guid}/packed", SellerPackedAsync);
        seller.MapPost("/fulfillments/{fulfillmentId:guid}/shipments", SellerCreateShipmentAsync);
        seller.MapPost("/fulfillments/{fulfillmentId:guid}/shipments/{shipmentId:guid}/tracking", SellerTrackingAsync);
        seller.MapPost("/fulfillments/{fulfillmentId:guid}/shipments/{shipmentId:guid}/dispatch", SellerDispatchAsync);
        seller.MapPost("/fulfillments/{fulfillmentId:guid}/shipments/{shipmentId:guid}/deliver", SellerDeliverAsync);

        var admin = app.MapGroup("/v1/admin");
        admin.MapGet("/fulfillments", AdminListAsync);
        admin.MapGet("/fulfillments/{fulfillmentId:guid}", AdminGetAsync);

        var customer = app.MapGroup("/v1/customer");
        customer.MapGet("/orders/{checkoutId:guid}/fulfillments", CustomerListAsync);
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);

    private static async Task<IResult> SellerListAsync(
        FulfillmentPanelComposer composer,
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
            return Results.Json(await composer.ListForSellerAsync(sellerPartyId, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return Results.Json(new { title = "Bad Request", errorCode = "fulfillment.rejected", detail = ex.Message }, statusCode: 400); }
    }

    private static async Task<IResult> SellerGetAsync(
        Guid fulfillmentId,
        FulfillmentPanelComposer composer,
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
            var page = await composer.GetForSellerAsync(sellerPartyId, fulfillmentId, cancellationToken);
            return page is null
                ? Results.Json(new { title = "Not Found", errorCode = "fulfillment.missing" }, statusCode: 404)
                : Results.Json(page);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static async Task<IResult> SellerProcessingAsync(
        Guid fulfillmentId,
        FulfillmentPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
        => await SellerMutateAsync(
            request, session, guard, environment, fulfillmentId, composer,
            (actor, id, c) => composer.MarkProcessingAsync(id, actor, c), cancellationToken);

    private static async Task<IResult> SellerPackedAsync(
        Guid fulfillmentId,
        FulfillmentPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
        => await SellerMutateAsync(
            request, session, guard, environment, fulfillmentId, composer,
            (actor, id, c) => composer.MarkPackedAsync(id, actor, c), cancellationToken);

    private static async Task<IResult> SellerCreateShipmentAsync(
        Guid fulfillmentId,
        FulfillmentCreateShipmentRequest body,
        FulfillmentPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
        => await SellerMutateAsync(
            request, session, guard, environment, fulfillmentId, composer,
            (actor, id, c) => composer.CreateShipmentAsync(id, actor, body, c), cancellationToken);

    private static async Task<IResult> SellerTrackingAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        FulfillmentAssignTrackingRequest body,
        FulfillmentPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
        => await SellerMutateAsync(
            request, session, guard, environment, fulfillmentId, composer,
            (actor, id, c) => composer.AssignTrackingAsync(id, shipmentId, actor, body.TrackingReference, c), cancellationToken);

    private static async Task<IResult> SellerDispatchAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        FulfillmentPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
        => await SellerMutateAsync(
            request, session, guard, environment, fulfillmentId, composer,
            (actor, id, c) => composer.DispatchShipmentAsync(id, shipmentId, actor, c), cancellationToken);

    private static async Task<IResult> SellerDeliverAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        FulfillmentPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
        => await SellerMutateAsync(
            request, session, guard, environment, fulfillmentId, composer,
            (actor, id, c) => composer.DeliverShipmentAsync(id, shipmentId, actor, c), cancellationToken);

    private static async Task<IResult> SellerMutateAsync(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        Guid fulfillmentId,
        FulfillmentPanelComposer composer,
        Func<Guid, Guid, CancellationToken, Task<Tooba.Fulfillment.Application.FulfillmentSnapshot>> action,
        CancellationToken cancellationToken)
    {
        try
        {
            var (actorUserId, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            var existing = await composer.GetForSellerAsync(sellerPartyId, fulfillmentId, cancellationToken);
            if (existing is null)
            {
                return Results.Json(new { title = "Not Found", errorCode = "fulfillment.missing" }, statusCode: 404);
            }

            return Results.Json(await action(actorUserId, fulfillmentId, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return Results.Json(new { title = "Bad Request", errorCode = "fulfillment.rejected", detail = ex.Message }, statusCode: 400); }
    }

    private static async Task<IResult> AdminListAsync(
        FulfillmentPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            return Results.Json(await composer.ListAllAsync(cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static async Task<IResult> AdminGetAsync(
        Guid fulfillmentId,
        FulfillmentPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            var page = await composer.GetAsync(fulfillmentId, cancellationToken);
            return page is null
                ? Results.Json(new { title = "Not Found", errorCode = "fulfillment.missing" }, statusCode: 404)
                : Results.Json(page);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static async Task<IResult> CustomerListAsync(
        Guid checkoutId,
        FulfillmentPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        Order.Infrastructure.Persistence.OrderDbContext orders,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = ResolveCustomerActor(request, session, environment);
            if (actor is null)
            {
                return Results.Json(new { title = "Unauthorized", errorCode = "customer.actor.missing" }, statusCode: 401);
            }

            var owned = await orders.Checkouts.AsNoTracking()
                .AnyAsync(x => x.CheckoutId == checkoutId && x.PlacedByUserId == actor.Value, cancellationToken);
            if (!owned)
            {
                return Results.Json(new { title = "Not Found", errorCode = "customer.order.missing" }, statusCode: 404);
            }

            return Results.Json(await composer.ListForCheckoutAsync(checkoutId, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static Guid? ResolveCustomerActor(HttpRequest request, CurrentAuthenticatedSession session, IHostEnvironment environment)
    {
        if (session.IsAuthenticated && session.UserId is { } authenticated)
        {
            return authenticated;
        }

        if ((environment.IsDevelopment() || environment.IsEnvironment("Testing"))
            && request.Headers.TryGetValue("X-Tooba-Dev-Actor-User-Id", out var raw)
            && Guid.TryParse(raw.ToString(), out var devActor)
            && devActor != Guid.Empty)
        {
            return devActor;
        }

        return null;
    }
}
