using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;

namespace Tooba.Host.Admin;

/// <summary>
/// مسیرهای فقط‌خواندنی عملیات مدیر برای سطوح cross-module.
/// مسیرهای /orders و /customers به Order.Endpoints منتقل شده‌اند.
/// </summary>
public static class AdminPanelEndpoints
{
    /// <summary>
    /// مسیرهای داشبورد و فروشندگان مدیر را ثبت می‌کند.
    /// </summary>
    public static void MapAdminPanelEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/admin");
        group.MapGet("/dashboard", GetDashboardAsync);
        // R11: GET /v1/admin/orders owned by Order.Endpoints (AdminOrdersGridEndpoints).
        // R3: POST /v1/admin/orders/query is owned by Order.Endpoints (MapOrderEndpoints).
        // R6: GET /v1/admin/orders/{checkoutId} is owned by Order.Endpoints (AdminOrderDetailEndpoints).
        // R2_REMAINDER: payment detail/actions owned by Payment.Endpoints (MapPaymentEndpoints).
        // R11: GET/POST /v1/admin/customers* owned by Order.Endpoints (AdminCustomersEndpoints).
        group.MapGet("/sellers", ListSellersAsync);
        group.MapPost("/sellers/query", QuerySellersGridAsync);
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
