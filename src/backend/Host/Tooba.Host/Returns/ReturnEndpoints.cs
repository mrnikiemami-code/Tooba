using Tooba.BuildingBlocks;
using Tooba.Host.Admin;
using Tooba.Host.Seller;

namespace Tooba.Host.Returns;

/// <summary>
/// HTTP مرجوعی برای customer/seller/admin با فیلتر مجوز در سرور.
/// </summary>
public static class ReturnEndpoints
{
    /// <summary>
    /// مسیرهای مرجوعی را ثبت می‌کند.
    /// </summary>
    public static void MapReturnEndpoints(this WebApplication app)
    {
        var customer = app.MapGroup("/v1/customer");
        customer.MapGet("/returns", CustomerListAsync);
        customer.MapGet("/returns/{returnRequestId:guid}", CustomerGetAsync);
        customer.MapPost("/returns", CustomerCreateAsync);

        var seller = app.MapGroup("/v1/seller");
        seller.MapGet("/returns", SellerListAsync);
        seller.MapGet("/returns/{returnRequestId:guid}", SellerGetAsync);
        seller.MapPost("/returns/{returnRequestId:guid}/approve", SellerApproveAsync);
        seller.MapPost("/returns/{returnRequestId:guid}/reject", SellerRejectAsync);

        var admin = app.MapGroup("/v1/admin");
        admin.MapGet("/returns", AdminListAsync);
        admin.MapGet("/returns/{returnRequestId:guid}", AdminGetAsync);
        admin.MapPost("/returns/{returnRequestId:guid}/retry-refund", AdminRetryRefundAsync);
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);

    private static async Task<IResult> CustomerListAsync(
        ReturnPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = ResolveCustomerActor(request, session, environment);
            if (actor is null)
            {
                return Results.Json(new { title = "Unauthorized", errorCode = "customer.actor.missing" }, statusCode: 401);
            }

            return Results.Json(await composer.ListForCustomerAsync(actor.Value, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static async Task<IResult> CustomerGetAsync(
        Guid returnRequestId,
        ReturnPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = ResolveCustomerActor(request, session, environment);
            if (actor is null)
            {
                return Results.Json(new { title = "Unauthorized", errorCode = "customer.actor.missing" }, statusCode: 401);
            }

            var page = await composer.GetAsync(returnRequestId, cancellationToken);
            if (page is null || page.RequestedByUserId != actor.Value)
            {
                return Results.Json(new { title = "Not Found", errorCode = "return.missing" }, statusCode: 404);
            }

            return Results.Json(page);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static async Task<IResult> CustomerCreateAsync(
        CreateReturnRequest body,
        ReturnPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = ResolveCustomerActor(request, session, environment);
            if (actor is null)
            {
                return Results.Json(new { title = "Unauthorized", errorCode = "customer.actor.missing" }, statusCode: 401);
            }

            return Results.Json(await composer.CreateAsync(actor.Value, body, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return Results.Json(new { title = "Bad Request", errorCode = "return.rejected", detail = ex.Message }, statusCode: 400); }
    }

    private static async Task<IResult> SellerListAsync(
        ReturnPanelComposer composer,
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
    }

    private static async Task<IResult> SellerGetAsync(
        Guid returnRequestId,
        ReturnPanelComposer composer,
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
            var page = await composer.GetForSellerAsync(sellerPartyId, returnRequestId, cancellationToken);
            return page is null
                ? Results.Json(new { title = "Not Found", errorCode = "return.missing" }, statusCode: 404)
                : Results.Json(page);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static async Task<IResult> SellerApproveAsync(
        Guid returnRequestId,
        ReturnPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
        => await SellerMutateAsync(
            request, session, guard, environment, returnRequestId, composer,
            (actor, id, c) => composer.ApproveAsync(id, actor, c), cancellationToken);

    private static async Task<IResult> SellerRejectAsync(
        Guid returnRequestId,
        RejectReturnRequest body,
        ReturnPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
        => await SellerMutateAsync(
            request, session, guard, environment, returnRequestId, composer,
            (actor, id, c) => composer.RejectAsync(id, actor, body.Reason, c), cancellationToken);

    private static async Task<IResult> SellerMutateAsync(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        Guid returnRequestId,
        ReturnPanelComposer composer,
        Func<Guid, Guid, CancellationToken, Task<Tooba.Returns.Application.ReturnSnapshot>> action,
        CancellationToken cancellationToken)
    {
        try
        {
            var (actorUserId, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            var existing = await composer.GetForSellerAsync(sellerPartyId, returnRequestId, cancellationToken);
            if (existing is null)
            {
                return Results.Json(new { title = "Not Found", errorCode = "return.missing" }, statusCode: 404);
            }

            return Results.Json(await action(actorUserId, returnRequestId, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return Results.Json(new { title = "Bad Request", errorCode = "return.rejected", detail = ex.Message }, statusCode: 400); }
    }

    private static async Task<IResult> AdminListAsync(
        ReturnPanelComposer composer,
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
        Guid returnRequestId,
        ReturnPanelComposer composer,
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
            var page = await composer.GetAsync(returnRequestId, cancellationToken);
            return page is null
                ? Results.Json(new { title = "Not Found", errorCode = "return.missing" }, statusCode: 404)
                : Results.Json(page);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static async Task<IResult> AdminRetryRefundAsync(
        Guid returnRequestId,
        ReturnPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorUserId = await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            return Results.Json(await composer.RetryRefundAsync(returnRequestId, actorUserId, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex) { return Results.Json(new { title = "Bad Request", errorCode = "return.rejected", detail = ex.Message }, statusCode: 400); }
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
