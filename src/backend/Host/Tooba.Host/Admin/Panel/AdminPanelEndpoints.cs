using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.BuildingBlocks.Security;
using Tooba.Host.Admin.Development;

namespace Tooba.Host.Admin.Panel;

/// <summary>
/// مسیرهای فقط‌خواندنی عملیات مدیر برای سطوح cross-module.
/// GET/POST sellers به Party.Endpoints منتقل شده‌اند.
/// Dashboard: HOST_PRESENTATION_COMPOSITION_CQRS_EXCEPTION (composition without module request handlers).
/// </summary>
public static class AdminPanelEndpoints
{
    /// <summary>
    /// مسیرهای داشبورد و dev-context مدیر را ثبت می‌کند.
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
        // W1: GET /v1/admin/sellers owned by Party.Endpoints (PartyAdminSellersEndpoints).
        // W2: POST /v1/admin/sellers/query owned by Party.Endpoints (PartyAdminSellersEndpoints).
        // W3: dashboard auth/presentation canonicalized; ownership remains Host Panel.
        group.MapGet("/dev-context", GetDevContext);
    }

    private static async Task<IResult> GetDashboardAsync(
        AdminPanelComposer composer,
        HttpRequest request,
        IAdminPanelAccess adminAccess,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        await adminAccess.RequireAuthorizedAsync(request, cancellationToken).ConfigureAwait(false);
        var summary = await composer.GetDashboardAsync(cancellationToken).ConfigureAwait(false);
        return api.From(Result.Success(summary));
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
}
