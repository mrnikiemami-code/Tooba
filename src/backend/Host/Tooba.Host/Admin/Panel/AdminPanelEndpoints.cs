using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.BuildingBlocks.Security;

namespace Tooba.Host.Admin.Panel;

/// <summary>
/// مسیر داشبورد Admin برای ترکیب cross-module.
/// GET/POST sellers به Party.Endpoints؛ dev-context به Admin/Development منتقل شده است.
/// Dashboard: HOST_PRESENTATION_COMPOSITION_CQRS_EXCEPTION (composition without module request handlers).
/// </summary>
public static class AdminPanelEndpoints
{
    /// <summary>
    /// مسیر داشبورد مدیر را ثبت می‌کند.
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
        // W4: GET /v1/admin/dev-context owned by Admin/Development (AdminDevContextEndpoints).
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
}
