using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Localization.Application;

namespace Tooba.Host.Admin;

/// <summary>
/// مسیرهای عملیات lifecycle سفارش ادمین.
/// </summary>
public static class AdminOrderOperationsEndpoints
{
    /// <summary>مسیرهای operations و return-eligibility را ثبت می‌کند.</summary>
    public static void MapAdminOrderOperationsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/admin/orders");
        group.MapGet("/inventory-recovery/audit", AuditInventoryRecoveryAsync);
        group.MapGet("/{checkoutId:guid}/inventory-recovery", AssessInventoryRecoveryAsync);
        group.MapGet("/{checkoutId:guid}/operations", ListOperationsAsync);
        group.MapPost("/{checkoutId:guid}/operations", ExecuteOperationAsync);
        group.MapGet("/{checkoutId:guid}/return-eligibility", ListReturnEligibilityAsync);
        app.MapGet("/v1/admin/shipping-methods", ListShippingMethodsAsync);
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

    private static async Task<IResult> ListShippingMethodsAsync(
        FulfillmentDbContext db,
        ILanguageDirectory languages,
        ShippingMethodsOptions options,
        string? language,
        CancellationToken cancellationToken)
    {
        var tree = await ShippingServiceEndpoints.ListEnabledMethodsTreeAsync(
            db, languages, options, language, cancellationToken);
        return Results.Json(tree);
    }

    private static async Task<IResult> ListOperationsAsync(
        Guid checkoutId,
        AdminOrderOperationsComposer composer,
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
            return Results.Json(await composer.ListAsync(checkoutId, actor, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> ExecuteOperationAsync(
        Guid checkoutId,
        AdminOrderOperationRequest body,
        AdminOrderOperationsComposer composer,
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
            return Results.Json(await composer.ExecuteAsync(checkoutId, actor, body, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(
                new { title = ex.Message, errorCode = "order.operation.failed", detail = ex.Message },
                statusCode: 400);
        }
    }

    private static async Task<IResult> ListReturnEligibilityAsync(
        Guid checkoutId,
        AdminOrderOperationsComposer composer,
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
            return Results.Json(await composer.ListReturnEligibilityAsync(checkoutId, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(
            new
            {
                title = ex.Title,
                errorCode = ex.ErrorCode ?? "order.operation.failed",
                detail = ex.Title,
            },
            statusCode: ex.StatusCode);
}
