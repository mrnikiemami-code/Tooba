using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application;

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
        group.MapGet("/{checkoutId:guid}/operations", ListOperationsAsync);
        group.MapPost("/{checkoutId:guid}/operations", ExecuteOperationAsync);
        group.MapGet("/{checkoutId:guid}/return-eligibility", ListReturnEligibilityAsync);
        app.MapGet("/v1/admin/shipping-methods", ListShippingMethodsAsync);
    }

    private static IResult ListShippingMethodsAsync(ShippingMethodsOptions options)
    {
        var enabled = ShippingMethodRegistry.Enabled(options);
        return Results.Json(enabled.Select(x => new { code = x.Code, labelFa = x.LabelFa, providerKind = x.ProviderKind }));
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
