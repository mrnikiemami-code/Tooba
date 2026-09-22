using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;

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
        // R3: POST /v1/admin/orders/query is owned by Order.Endpoints (MapOrderEndpoints).
        group.MapGet("/orders/{checkoutId:guid}", GetOrderAsync);
        // R2_REMAINDER: payment detail/actions owned by Payment.Endpoints (MapPaymentEndpoints).
        group.MapGet("/sellers", ListSellersAsync);
        group.MapPost("/sellers/query", QuerySellersGridAsync);
        group.MapGet("/customers", ListCustomersAsync);
        group.MapPost("/customers/query", QueryCustomersGridAsync);
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
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            var page = await composer.GetOrderAsync(checkoutId, actor, cancellationToken);
            return page is null
                ? Results.Json(new { title = "سفارش پیدا نشد.", errorCode = "admin.order.missing" }, statusCode: 404)
                : Results.Json(page);
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
