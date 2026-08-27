using Tooba.BuildingBlocks;
using Tooba.Host.Admin;
using Tooba.Host.Seller;

namespace Tooba.Host.Settlement;

/// <summary>
/// HTTP تسویه برای seller/admin با فیلتر مجوز در سرور.
/// </summary>
public static class SettlementEndpoints
{
    /// <summary>
    /// مسیرهای تسویه را ثبت می‌کند.
    /// </summary>
    public static void MapSettlementEndpoints(this WebApplication app)
    {
        var seller = app.MapGroup("/v1/seller");
        seller.MapGet("/settlement/balance", SellerBalanceAsync);
        seller.MapGet("/settlement/entries", SellerEntriesAsync);
        seller.MapGet("/settlement/statements", SellerStatementsAsync);
        seller.MapGet("/settlement/payout-requests", SellerPayoutListAsync);
        seller.MapPost("/settlement/payout-requests", SellerRequestPayoutAsync);

        var admin = app.MapGroup("/v1/admin");
        admin.MapGet("/settlement/balances", AdminBalancesAsync);
        admin.MapGet("/settlement/payout-queue", AdminPayoutQueueAsync);
        admin.MapPost("/settlement/payout-requests/{payoutRequestId:guid}/process", AdminProcessPayoutAsync);
        admin.MapPost("/settlement/payout-requests/{payoutRequestId:guid}/retry", AdminRetryPayoutAsync);
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);

    private static async Task<IResult> SellerBalanceAsync(
        SettlementPanelComposer composer,
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
            var balance = await composer.GetBalanceAsync(sellerPartyId, cancellationToken);
            return balance is null
                ? Results.Json(new { title = "Not Found", errorCode = "settlement.account.missing" }, statusCode: 404)
                : Results.Json(balance);
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static async Task<IResult> SellerEntriesAsync(
        SettlementPanelComposer composer,
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
            return Results.Json(await composer.ListEntriesAsync(sellerPartyId, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static async Task<IResult> SellerStatementsAsync(
        SettlementPanelComposer composer,
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
            return Results.Json(await composer.ListStatementsAsync(sellerPartyId, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static async Task<IResult> SellerPayoutListAsync(
        SettlementPanelComposer composer,
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
            return Results.Json(await composer.ListPayoutRequestsAsync(sellerPartyId, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static async Task<IResult> SellerRequestPayoutAsync(
        RequestPayoutBody body,
        SettlementPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var (actorUserId, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            return Results.Json(await composer.RequestPayoutAsync(sellerPartyId, actorUserId, body, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex)
        {
            return Results.Json(new { title = "Bad Request", errorCode = "settlement.payout.rejected", detail = ex.Message }, statusCode: 400);
        }
    }

    private static async Task<IResult> AdminBalancesAsync(
        SettlementPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        ControlPlaneRegistry registry,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await SettlementAdminAccess.RequireAuthorizedAsync(
                request, session, tenant, registry, guard, environment, cancellationToken);
            return Results.Json(await composer.ListAllBalancesAsync(cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static async Task<IResult> AdminPayoutQueueAsync(
        SettlementPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        ControlPlaneRegistry registry,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await SettlementAdminAccess.RequireAuthorizedAsync(
                request, session, tenant, registry, guard, environment, cancellationToken);
            return Results.Json(await composer.ListPayoutQueueAsync(cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
    }

    private static async Task<IResult> AdminProcessPayoutAsync(
        Guid payoutRequestId,
        SettlementPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        ControlPlaneRegistry registry,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorUserId = await SettlementAdminAccess.RequireAuthorizedAsync(
                request, session, tenant, registry, guard, environment, cancellationToken);
            return Results.Json(await composer.ProcessPayoutAsync(payoutRequestId, actorUserId, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex)
        {
            return Results.Json(new { title = "Bad Request", errorCode = "settlement.payout.rejected", detail = ex.Message }, statusCode: 400);
        }
    }

    private static async Task<IResult> AdminRetryPayoutAsync(
        Guid payoutRequestId,
        SettlementPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        ControlPlaneRegistry registry,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorUserId = await SettlementAdminAccess.RequireAuthorizedAsync(
                request, session, tenant, registry, guard, environment, cancellationToken);
            return Results.Json(await composer.RetryPayoutAsync(payoutRequestId, actorUserId, cancellationToken));
        }
        catch (PlatformHttpException ex) { return ToError(ex); }
        catch (InvalidOperationException ex)
        {
            return Results.Json(new { title = "Bad Request", errorCode = "settlement.payout.rejected", detail = ex.Message }, statusCode: 400);
        }
    }
}
