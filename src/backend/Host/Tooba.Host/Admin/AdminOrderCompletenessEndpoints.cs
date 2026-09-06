using Tooba.BuildingBlocks;

namespace Tooba.Host.Admin;

/// <summary>
/// مسیرهای تکمیل عملیاتی جزئیات سفارش: یادداشت، تاریخچه، فاکتور، رسید.
/// </summary>
public static class AdminOrderCompletenessEndpoints
{
    /// <summary>مسیرهای notes / operational-history / invoice / receipt را ثبت می‌کند.</summary>
    public static void MapAdminOrderCompletenessEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/admin/orders");
        group.MapGet("/{checkoutId:guid}/notes", ListNotesAsync);
        group.MapPost("/{checkoutId:guid}/notes", AddNoteAsync);
        group.MapGet("/{checkoutId:guid}/operational-history", ListHistoryAsync);
        group.MapGet("/{checkoutId:guid}/invoice.html", GetInvoiceAsync);
        group.MapGet("/{checkoutId:guid}/receipt.html", GetReceiptAsync);
    }

    private static async Task<IResult> ListNotesAsync(
        Guid checkoutId,
        AdminOrderCompletenessComposer composer,
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
            return Results.Json(await composer.ListNotesAsync(checkoutId, actor, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> AddNoteAsync(
        Guid checkoutId,
        AdminOrderNoteRequest? body,
        AdminOrderCompletenessComposer composer,
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
            return Results.Json(await composer.AddNoteAsync(checkoutId, actor, body?.Body, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> ListHistoryAsync(
        Guid checkoutId,
        int? page,
        int? pageSize,
        AdminOrderCompletenessComposer composer,
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
            return Results.Json(await composer.ListOperationalHistoryAsync(
                checkoutId,
                actor,
                page ?? 1,
                pageSize ?? 20,
                cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> GetInvoiceAsync(
        Guid checkoutId,
        AdminOrderCompletenessComposer composer,
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
            var html = await composer.BuildInvoiceHtmlAsync(checkoutId, actor, cancellationToken);
            return Results.Content(html, "text/html; charset=utf-8");
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> GetReceiptAsync(
        Guid checkoutId,
        AdminOrderCompletenessComposer composer,
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
            var html = await composer.BuildReceiptHtmlAsync(checkoutId, actor, cancellationToken);
            return Results.Content(html, "text/html; charset=utf-8");
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
