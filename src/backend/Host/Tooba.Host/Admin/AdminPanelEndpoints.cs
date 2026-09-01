using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.Payment.Application;

namespace Tooba.Host.Admin;

/// <summary>
/// مسیرهای فقط‌خواندنی عملیات مدیر؛ هر handler پیش از خواندن داده مجوز Tenant را بررسی می‌کند.
/// </summary>
public static class AdminPanelEndpoints
{
    /// <summary>
    /// مسیرهای داشبورد، سفارش‌ها، فروشندگان و مشتریان مدیر را ثبت می‌کند.
    /// </summary>
    public static void MapAdminPanelEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/admin");
        group.MapGet("/dashboard", GetDashboardAsync);
        group.MapGet("/orders", ListOrdersAsync);
        group.MapPost("/orders/query", QueryOrdersGridAsync);
        group.MapGet("/orders/{checkoutId:guid}", GetOrderAsync);
        group.MapGet("/payments/{paymentId:guid}", GetPaymentAsync);
        group.MapPost("/payments/{paymentId:guid}/reconcile", ReconcilePaymentAsync);
        group.MapGet("/sellers", ListSellersAsync);
        group.MapPost("/sellers/query", QuerySellersGridAsync);
        group.MapGet("/customers", ListCustomersAsync);
        group.MapPost("/customers/query", QueryCustomersGridAsync);
        group.MapPost("/payments/query", QueryPaymentsGridAsync);
        group.MapGet("/dev-context", GetDevContext);
    }

    private static async Task<IResult> GetDashboardAsync(
        AdminPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(request, session, tenant, guard, environment, cancellationToken,
            () => composer.GetDashboardAsync(cancellationToken));

    private static async Task<IResult> ListOrdersAsync(
        AdminPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(request, session, tenant, guard, environment, cancellationToken,
            () => composer.ListOrdersAsync(cancellationToken));

    private static Task<IResult> QueryOrdersGridAsync(
        GridQueryRequest body,
        AdminPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        AdminGridQueryEndpoint.ExecuteAsync(
            body,
            request,
            session,
            tenant,
            guard,
            environment,
            composer.QueryOrdersGridAsync,
            cancellationToken);

    private static async Task<IResult> GetOrderAsync(
        Guid checkoutId,
        AdminPanelComposer composer,
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
            var page = await composer.GetOrderAsync(checkoutId, cancellationToken);
            return page is null
                ? Results.Json(new { title = "سفارش پیدا نشد.", errorCode = "admin.order.missing" }, statusCode: 404)
                : Results.Json(page);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> GetPaymentAsync(
        Guid paymentId,
        IPaymentAdminDirectory payments,
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
            var page = await payments.GetOperationalAsync(paymentId, cancellationToken);
            return page is null
                ? Results.Json(new { title = "پرداخت پیدا نشد.", errorCode = "admin.payment.missing" }, statusCode: 404)
                : Results.Json(page);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> ReconcilePaymentAsync(
        Guid paymentId,
        IPaymentAdminDirectory payments,
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
            var result = await payments.ReconcileAsync(paymentId, cancellationToken);
            return Results.Json(result);
        }
        catch (InvalidOperationException ex) when (ex.Message is "payment.missing" or "payment.attempt.missing")
        {
            return Results.Json(new { title = "پرداخت پیدا نشد.", errorCode = ex.Message }, statusCode: 404);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> ListSellersAsync(
        AdminPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(request, session, tenant, guard, environment, cancellationToken,
            () => composer.ListSellersAsync(cancellationToken));

    private static Task<IResult> QuerySellersGridAsync(
        GridQueryRequest body,
        AdminPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        AdminGridQueryEndpoint.ExecuteAsync(
            body,
            request,
            session,
            tenant,
            guard,
            environment,
            composer.QuerySellersGridAsync,
            cancellationToken);

    private static async Task<IResult> ListCustomersAsync(
        AdminPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(request, session, tenant, guard, environment, cancellationToken,
            () => composer.ListCustomersAsync(cancellationToken));

    private static Task<IResult> QueryCustomersGridAsync(
        GridQueryRequest body,
        AdminPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        AdminGridQueryEndpoint.ExecuteAsync(
            body,
            request,
            session,
            tenant,
            guard,
            environment,
            composer.QueryCustomersGridAsync,
            cancellationToken);

    private static Task<IResult> QueryPaymentsGridAsync(
        GridQueryRequest body,
        AdminPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        AdminGridQueryEndpoint.ExecuteAsync(
            body,
            request,
            session,
            tenant,
            guard,
            environment,
            composer.QueryPaymentsGridAsync,
            cancellationToken);

    private static async Task<IResult> ExecuteAsync<T>(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken,
        Func<Task<T>> action)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            return Results.Json(await action());
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static IResult GetDevContext(IHostEnvironment environment)
    {
        if (!environment.IsDevelopment() || AdminDevActorBootstrap.Snapshot is not { } snapshot)
        {
            return Results.Json(new { title = "Not Found", errorCode = "admin.dev.unavailable" }, statusCode: 404);
        }

        return Results.Json(new
        {
            actorUserId = snapshot.ActorUserId,
            actorLabel = snapshot.ActorLabel,
            tenantId = snapshot.TenantId,
        });
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
}
