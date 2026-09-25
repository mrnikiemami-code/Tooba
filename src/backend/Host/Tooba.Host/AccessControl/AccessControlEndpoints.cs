using Tooba.AccessControl.Application;
using Tooba.AccessControl.Application.Authorization;
using Tooba.AccessControl.Domain;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Host.Admin;
using Tooba.Host.Seller;

namespace Tooba.Host.AccessControl;

/// <summary>
/// مرز HTTP مرکز کنترل دسترسی Admin و Seller.
/// </summary>
public static class AccessControlEndpoints
{
    /// <summary>مسیرهای access-control را ثبت می‌کند.</summary>
    public static void MapAccessControlEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/v1/admin/access-control");
        admin.MapGet("/demo-preview", AdminDemoPreviewAsync);
        admin.MapGet("/scope-resources/categories", AdminListCategoriesAsync);
        admin.MapGet("/scope-resources/brands", AdminListBrandsAsync);
        admin.MapGet("/scope-resources/products", AdminListProductsAsync);
        admin.MapGet("/scope-resources/warehouses", AdminDeferredScopeAsync);
        admin.MapGet("/scope-resources/stores", AdminDeferredScopeAsync);
        admin.MapGet("/scope-resources/order-segments", AdminDeferredScopeAsync);

        var seller = app.MapGroup("/v1/seller/access-control");
        seller.MapGet("/scope-resources/categories", SellerListCategoriesAsync);
        seller.MapGet("/scope-resources/brands", SellerListBrandsAsync);
        seller.MapGet("/scope-resources/products", SellerListProductsAsync);
        seller.MapGet("/scope-resources/warehouses", SellerDeferredScopeAsync);
        seller.MapGet("/scope-resources/stores", SellerDeferredScopeAsync);
        seller.MapGet("/scope-resources/order-segments", SellerDeferredScopeAsync);
    }

    private static string? Trace(HttpRequest request) =>
        request.Headers.TryGetValue("X-Request-Id", out var v) ? v.ToString() : null;

    private static IResult MapError(Exception ex) =>
        ex is AccessControlException ace
            ? Results.Json(new { title = ace.Message, code = ace.Code }, statusCode: ace.Code.Contains("escalation", StringComparison.Ordinal) || ace.Code.Contains("ceiling", StringComparison.Ordinal) ? 403 : 400)
            : Results.Json(new { title = "access.error", code = "access.error" }, statusCode: 500);

    #region Admin platform

    private static IResult AdminDemoPreviewAsync(IHostEnvironment env)
    {
        if (!env.IsDevelopment())
        {
            return Results.NotFound();
        }

        var demo = AccessControlDemoSnapshot.Current;
        return demo is null
            ? Results.Json(new { title = "ACC demo seed not ready", code = "access.demo.not_ready" }, statusCode: StatusCodes.Status503ServiceUnavailable)
            : Results.Json(demo);
    }

    #endregion

    #region Seller

    private static async Task<(Guid Actor, Guid SellerId)> RequireSellerAsync(
        HttpRequest request, CurrentAuthenticatedSession session, IAuthorizationGuard guard, IHostEnvironment env, CancellationToken ct)
    {
        var ctx = await SellerPanelAccess.RequireAuthorizedAsync(request, session, guard, env, ct);
        return (ctx.ActorUserId, ctx.SellerPartyId);
    }

    private static async Task<IResult> AdminListCategoriesAsync(
        HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, ICatalogLookupGateway catalog, CancellationToken ct, string? q = null)
    {
        var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
        await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, ct);
        var items = await catalog.ListCategoriesForAccessControlAsync(q, ct);
        return Results.Json(new { deferred = false, items });
    }

    private static async Task<IResult> SellerListCategoriesAsync(
        HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, ICatalogLookupGateway catalog, CancellationToken ct, string? q = null)
    {
        var (actor, _) = await RequireSellerAsync(request, session, guard, env, ct);
        await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, ct);
        var items = await catalog.ListCategoriesForAccessControlAsync(q, ct);
        return Results.Json(new { deferred = false, items });
    }

    private static async Task<IResult> AdminListBrandsAsync(
        HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, ICatalogLookupGateway catalog, CancellationToken ct, string? q = null)
    {
        var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
        await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, ct);
        var items = await catalog.ListBrandsForAccessControlAsync(q, ct);
        return Results.Json(new { deferred = false, items });
    }

    private static async Task<IResult> SellerListBrandsAsync(
        HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, ICatalogLookupGateway catalog, CancellationToken ct, string? q = null)
    {
        var (actor, _) = await RequireSellerAsync(request, session, guard, env, ct);
        await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, ct);
        var items = await catalog.ListBrandsForAccessControlAsync(q, ct);
        return Results.Json(new { deferred = false, items });
    }

    private static async Task<IResult> AdminListProductsAsync(
        HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, ICatalogLookupGateway catalog, CancellationToken ct, string? q = null)
    {
        var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
        await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, ct);
        var items = await catalog.ListProductsForAccessControlAsync(q, ct);
        return Results.Json(new { deferred = false, items });
    }

    private static async Task<IResult> SellerListProductsAsync(
        HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, ICatalogLookupGateway catalog, CancellationToken ct, string? q = null)
    {
        var (actor, _) = await RequireSellerAsync(request, session, guard, env, ct);
        await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, ct);
        var items = await catalog.ListProductsForAccessControlAsync(q, ct);
        return Results.Json(new { deferred = false, items });
    }

    private static async Task<IResult> AdminDeferredScopeAsync(
        HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, CancellationToken ct)
    {
        var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
        await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, ct);
        return Results.Json(new { deferred = true, items = Array.Empty<object>() });
    }

    private static async Task<IResult> SellerDeferredScopeAsync(
        HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, CancellationToken ct)
    {
        var (actor, _) = await RequireSellerAsync(request, session, guard, env, ct);
        await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, ct);
        return Results.Json(new { deferred = true, items = Array.Empty<object>() });
    }

    #endregion
}
