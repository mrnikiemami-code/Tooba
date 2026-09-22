using Tooba.BuildingBlocks;

namespace Tooba.Host.Admin;

/// <summary>
/// Thin Host endpoints for inventory-recovery audit/assess and supply-status.
/// Ops business routes live in Order.Endpoints.
/// </summary>
public static class AdminOrderInventoryRecoverySupplyEndpoints
{
    /// <summary>مسیرهای inventory-recovery و supply-status را ثبت می‌کند (بدون Ops).</summary>
    public static void MapAdminOrderInventoryRecoverySupplyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/admin/orders");
        group.MapGet("/inventory-recovery/audit", AuditInventoryRecoveryAsync);
        group.MapGet("/{checkoutId:guid}/inventory-recovery", AssessInventoryRecoveryAsync);
        group.MapGet("/{checkoutId:guid}/supply-status", GetOrderSupplyStatusAsync);
    }

    private static async Task<IResult> AuditInventoryRecoveryAsync(
        OrderInventoryRecoveryComposer recovery,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        int? take,
        CancellationToken cancellationToken)
    {
        _ = session;
        _ = tenant;
        return Results.Json(await recovery.AuditAsync(take ?? 50, cancellationToken));
    }

    private static async Task<IResult> AssessInventoryRecoveryAsync(
        Guid checkoutId,
        OrderInventoryRecoveryComposer recovery,
        CancellationToken cancellationToken) =>
        Results.Json(await recovery.AssessCheckoutAsync(checkoutId, cancellationToken));

    private static async Task<IResult> GetOrderSupplyStatusAsync(
        Guid checkoutId,
        OrderSupplyComposer supply,
        CancellationToken cancellationToken) =>
        Results.Json(await supply.GetStatusAsync(checkoutId, cancellationToken));
}
