using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.Host.Grid;
using Tooba.ProductWorkspace.Application.Composition.Models;

namespace Tooba.Host.Admin;

/// <summary>
/// مسیرهای HTTP ترکیب Workspace محصول. SQL بین‌ماژولی اینجا نوشته نمی‌شود.
/// W19: aggregate GET evacuated to ProductWorkspace.Endpoints.
/// W26: lifecycle POSTs evacuated to ProductWorkspace.Endpoints.
/// W27: variant create/patch evacuated to ProductWorkspace.Endpoints.
/// W28: product DELETE evacuated to Catalog.Endpoints.
/// W29: create / catalog-title / core / quantity-policy evacuated to ProductWorkspace.Endpoints.
/// W30: category / additional categories / brand evacuated to ProductWorkspace.Endpoints.
/// Host ProductWorkspace surface after brand options Catalog ownership: list + grid query only.
/// </summary>
public static class ProductWorkspaceEndpoints
{
    /// <summary>
    /// مسیرهای Admin Product Workspace را ثبت می‌کند.
    /// </summary>
    public static void MapProductWorkspaceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/admin/products");
        group.AddEndpointFilter(CatalogActorHttpBinding.BindAsync);
        group.MapGet("/", ListAsync);
        group.MapPost("/query", QueryGridAsync);
    }

    private static async Task<IResult> QueryGridAsync(
        GridQueryRequest body,
        ProductWorkspaceComposer composer,
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
            var normalized = AdminProductGridQueryPolicy.Normalize(body);
            return Results.Json(await composer.QueryGridAsync(normalized, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> ListAsync(
        ProductWorkspaceComposer composer,
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
            return Results.Json(await composer.ListAsync(cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
}
