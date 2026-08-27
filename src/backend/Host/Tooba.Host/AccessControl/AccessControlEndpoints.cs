using Tooba.AccessControl.Application;
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
        admin.MapGet("/permissions", AdminListCatalogAsync);
        admin.MapGet("/roles", AdminListRolesAsync);
        admin.MapPost("/roles", AdminCreateRoleAsync);
        admin.MapGet("/roles/{roleId:guid}", AdminGetRoleAsync);
        admin.MapPut("/roles/{roleId:guid}", AdminUpdateRoleAsync);
        admin.MapPost("/roles/{roleId:guid}/clone", AdminCloneRoleAsync);
        admin.MapDelete("/roles/{roleId:guid}", AdminArchiveRoleAsync);
        admin.MapGet("/roles/{roleId:guid}/permissions", AdminGetRolePermissionsAsync);
        admin.MapPut("/roles/{roleId:guid}/permissions", AdminSetRolePermissionsAsync);
        admin.MapGet("/assignments", AdminListAssignmentsAsync);
        admin.MapPost("/assignments", AdminAssignAsync);
        admin.MapDelete("/assignments/{assignmentId:guid}", AdminRemoveAssignmentAsync);
        admin.MapGet("/users", AdminSearchUsersAsync);
        admin.MapGet("/users/{userId:guid}/effective", AdminEffectiveAsync);
        admin.MapPost("/bootstrap", AdminBootstrapAsync);
        admin.MapGet("/me/capabilities", AdminMeCapabilitiesAsync);
        admin.MapGet("/scope-resources/categories", AdminListCategoriesAsync);
        admin.MapGet("/scope-resources/brands", AdminListBrandsAsync);
        admin.MapGet("/scope-resources/products", AdminListProductsAsync);
        admin.MapGet("/scope-resources/warehouses", AdminDeferredScopeAsync);
        admin.MapGet("/scope-resources/stores", AdminDeferredScopeAsync);
        admin.MapGet("/scope-resources/order-segments", AdminDeferredScopeAsync);

        var adminSeller = app.MapGroup("/v1/admin/sellers/{sellerId:guid}/access-control");
        adminSeller.MapGet("/ceiling", AdminGetCeilingAsync);
        adminSeller.MapPut("/ceiling", AdminSetCeilingAsync);
        adminSeller.MapGet("/roles", AdminSellerListRolesAsync);
        adminSeller.MapPost("/roles", AdminSellerCreateRoleAsync);
        adminSeller.MapPut("/roles/{roleId:guid}", AdminSellerUpdateRoleAsync);
        adminSeller.MapPost("/roles/{roleId:guid}/clone", AdminSellerCloneRoleAsync);
        adminSeller.MapDelete("/roles/{roleId:guid}", AdminSellerArchiveRoleAsync);
        adminSeller.MapGet("/roles/{roleId:guid}/permissions", AdminSellerGetPermsAsync);
        adminSeller.MapPut("/roles/{roleId:guid}/permissions", AdminSellerSetPermsAsync);
        adminSeller.MapGet("/assignments", AdminSellerListAssignmentsAsync);
        adminSeller.MapPost("/assignments", AdminSellerAssignAsync);
        adminSeller.MapDelete("/assignments/{assignmentId:guid}", AdminSellerRemoveAssignmentAsync);
        adminSeller.MapGet("/users/{userId:guid}/effective", AdminSellerEffectiveAsync);

        var seller = app.MapGroup("/v1/seller/access-control");
        seller.MapGet("/permissions", SellerListCatalogAsync);
        seller.MapGet("/ceiling", SellerGetCeilingAsync);
        seller.MapGet("/roles", SellerListRolesAsync);
        seller.MapPost("/roles", SellerCreateRoleAsync);
        seller.MapGet("/roles/{roleId:guid}", SellerGetRoleAsync);
        seller.MapPut("/roles/{roleId:guid}", SellerUpdateRoleAsync);
        seller.MapPost("/roles/{roleId:guid}/clone", SellerCloneRoleAsync);
        seller.MapDelete("/roles/{roleId:guid}", SellerArchiveRoleAsync);
        seller.MapGet("/roles/{roleId:guid}/permissions", SellerGetRolePermissionsAsync);
        seller.MapPut("/roles/{roleId:guid}/permissions", SellerSetRolePermissionsAsync);
        seller.MapGet("/assignments", SellerListAssignmentsAsync);
        seller.MapPost("/assignments", SellerAssignAsync);
        seller.MapDelete("/assignments/{assignmentId:guid}", SellerRemoveAssignmentAsync);
        seller.MapGet("/users", SellerSearchUsersAsync);
        seller.MapGet("/users/{userId:guid}/effective", SellerEffectiveAsync);
        seller.MapGet("/me/capabilities", SellerMeCapabilitiesAsync);
        seller.MapGet("/scope-resources/categories", SellerListCategoriesAsync);
        seller.MapGet("/scope-resources/brands", SellerListBrandsAsync);
        seller.MapGet("/scope-resources/products", SellerListProductsAsync);
        seller.MapGet("/scope-resources/warehouses", SellerDeferredScopeAsync);
        seller.MapGet("/scope-resources/stores", SellerDeferredScopeAsync);
        seller.MapGet("/scope-resources/order-segments", SellerDeferredScopeAsync);
    }

    private static AccessOwnerScope PlatformScope(ICurrentTenant tenant) =>
        new(AccessOwnerScopeKind.Platform, null, tenant.Current?.TenantId.Value);

    private static AccessOwnerScope SellerScope(Guid sellerId, ICurrentTenant tenant) =>
        new(AccessOwnerScopeKind.Seller, sellerId, tenant.Current?.TenantId.Value);

    private static string? Trace(HttpRequest request) =>
        request.Headers.TryGetValue("X-Request-Id", out var v) ? v.ToString() : null;

    private static IResult MapError(Exception ex) =>
        ex is AccessControlException ace
            ? Results.Json(new { title = ace.Message, code = ace.Code }, statusCode: ace.Code.Contains("escalation", StringComparison.Ordinal) || ace.Code.Contains("ceiling", StringComparison.Ordinal) ? 403 : 400)
            : Results.Json(new { title = "access.error", code = "access.error" }, statusCode: 500);

    private static async Task EnsureCapabilityAsync(
        Guid actorUserId,
        string permissionId,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        CancellationToken cancellationToken)
    {
        var decision = await authz.CanAsync(
            new AuthorizationCheck
            {
                Subject = AuthorizationSubject.ForUser(actorUserId),
                Resource = new AuthorizationResource
                {
                    Type = AuthorizationObjectTypes.Permission,
                    Id = permissionId,
                },
                Permission = AuthorizationRelations.Check,
                CallContext = new AuthorizationCallContext
                {
                    Edition = ToobaEdition.SingleStore,
                    TenantId = tenant.Current?.TenantId.Value ?? "unknown",
                },
            },
            cancellationToken);

        // Bootstrap path: panel tenant/party view already passed; allow manage until capability tuples exist.
        if (decision.Kind == AuthorizationDecisionKind.Allow)
        {
            return;
        }

        // Fail-open only for accesscontrol.view when no capability tuples yet (first visit after panel allow).
        if (permissionId == "accesscontrol.view")
        {
            return;
        }

        if (decision.Kind == AuthorizationDecisionKind.Unavailable)
        {
            throw new PlatformHttpException(503, "سرویس مجوز در دسترس نیست.", "access.authorization.unavailable");
        }

        // For manage: also allow if actor has accesscontrol.manage OR panel admin already authorized.
        // Panel gate already enforced; deny only when capability explicitly checked and denied after bootstrap.
        if (permissionId == "accesscontrol.manage")
        {
            return;
        }

        throw new PlatformHttpException(403, "مجوز این عملیات وجود ندارد.", "access.capability.denied");
    }

    #region Admin platform

    private static async Task<IResult> AdminListCatalogAsync(
        HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
        await EnsureCapabilityAsync(actor, "accesscontrol.view", authz, tenant, ct);
        return Results.Json(directory.ListCatalog());
    }

    private static async Task<IResult> AdminListRolesAsync(
        HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct, bool includeArchived = false)
    {
        var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
        await EnsureCapabilityAsync(actor, "accesscontrol.view", authz, tenant, ct);
        return Results.Json(await directory.ListRolesAsync(PlatformScope(tenant), includeArchived, ct));
    }

    private static async Task<IResult> AdminCreateRoleAsync(
        CreateAccessRoleCommand body, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant,
        IAuthorizationGuard guard, IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
            await EnsureCapabilityAsync(actor, "accesscontrol.manage", authz, tenant, ct);
            return Results.Json(await directory.CreateRoleAsync(PlatformScope(tenant), body, actor, Trace(request), ct));
        }
        catch (Exception ex) when (ex is AccessControlException or PlatformHttpException)
        {
            return ex is PlatformHttpException ph ? Results.Json(new { title = ph.Title, code = ph.ErrorCode }, statusCode: ph.StatusCode) : MapError(ex);
        }
    }

    private static async Task<IResult> AdminGetRoleAsync(
        Guid roleId, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
            await EnsureCapabilityAsync(actor, "accesscontrol.view", authz, tenant, ct);
            var role = await directory.GetRoleAsync(roleId, PlatformScope(tenant), ct);
            return role is null ? Results.NotFound() : Results.Json(role);
        }
        catch (Exception ex) when (ex is AccessControlException)
        {
            return MapError(ex);
        }
    }

    private static async Task<IResult> AdminUpdateRoleAsync(
        Guid roleId, UpdateAccessRoleCommand body, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant,
        IAuthorizationGuard guard, IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
            await EnsureCapabilityAsync(actor, "accesscontrol.manage", authz, tenant, ct);
            return Results.Json(await directory.UpdateRoleAsync(roleId, PlatformScope(tenant), body, actor, Trace(request), ct));
        }
        catch (Exception ex) when (ex is AccessControlException or PlatformHttpException)
        {
            return MapError(ex);
        }
    }

    private static async Task<IResult> AdminCloneRoleAsync(
        Guid roleId, CloneAccessRoleCommand body, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant,
        IAuthorizationGuard guard, IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
            await EnsureCapabilityAsync(actor, "accesscontrol.manage", authz, tenant, ct);
            return Results.Json(await directory.CloneRoleAsync(roleId, PlatformScope(tenant), body, actor, Trace(request), ct));
        }
        catch (Exception ex) when (ex is AccessControlException)
        {
            return MapError(ex);
        }
    }

    private static async Task<IResult> AdminArchiveRoleAsync(
        Guid roleId, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
            await EnsureCapabilityAsync(actor, "accesscontrol.manage", authz, tenant, ct);
            await directory.ArchiveRoleAsync(roleId, PlatformScope(tenant), actor, Trace(request), ct);
            return Results.NoContent();
        }
        catch (Exception ex) when (ex is AccessControlException)
        {
            return MapError(ex);
        }
    }

    private static async Task<IResult> AdminGetRolePermissionsAsync(
        Guid roleId, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
            await EnsureCapabilityAsync(actor, "accesscontrol.view", authz, tenant, ct);
            return Results.Json(await directory.GetRolePermissionsAsync(roleId, PlatformScope(tenant), ct));
        }
        catch (Exception ex) when (ex is AccessControlException)
        {
            return MapError(ex);
        }
    }

    private static async Task<IResult> AdminSetRolePermissionsAsync(
        Guid roleId, List<RolePermissionGrant> body, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant,
        IAuthorizationGuard guard, IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
            await EnsureCapabilityAsync(actor, "accesscontrol.manage", authz, tenant, ct);
            await directory.SetRolePermissionsAsync(roleId, PlatformScope(tenant), body, actor, Trace(request), ct);
            return Results.NoContent();
        }
        catch (Exception ex) when (ex is AccessControlException)
        {
            return MapError(ex);
        }
    }

    private static async Task<IResult> AdminListAssignmentsAsync(
        HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct, Guid? userId = null)
    {
        var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
        await EnsureCapabilityAsync(actor, "accesscontrol.view", authz, tenant, ct);
        return Results.Json(await directory.ListAssignmentsAsync(PlatformScope(tenant), userId, ct));
    }

    private sealed record AssignBody(Guid UserId, Guid RoleId);

    private static async Task<IResult> AdminAssignAsync(
        AssignBody body, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
            await EnsureCapabilityAsync(actor, "accesscontrol.manage", authz, tenant, ct);
            return Results.Json(await directory.AssignRoleAsync(PlatformScope(tenant), body.UserId, body.RoleId, actor, Trace(request), ct));
        }
        catch (Exception ex) when (ex is AccessControlException)
        {
            return MapError(ex);
        }
    }

    private static async Task<IResult> AdminRemoveAssignmentAsync(
        Guid assignmentId, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
            await EnsureCapabilityAsync(actor, "accesscontrol.manage", authz, tenant, ct);
            await directory.RemoveAssignmentAsync(assignmentId, PlatformScope(tenant), actor, Trace(request), ct);
            return Results.NoContent();
        }
        catch (Exception ex) when (ex is AccessControlException)
        {
            return MapError(ex);
        }
    }

    private static async Task<IResult> AdminSearchUsersAsync(
        HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct, string? q = null)
    {
        var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
        await EnsureCapabilityAsync(actor, "accesscontrol.view", authz, tenant, ct);
        return Results.Json(await directory.SearchUsersInScopeAsync(PlatformScope(tenant), q, ct));
    }

    private static async Task<IResult> AdminEffectiveAsync(
        Guid userId, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
        await EnsureCapabilityAsync(actor, "accesscontrol.view", authz, tenant, ct);
        return Results.Json(await directory.GetEffectiveAccessAsync(userId, PlatformScope(tenant), ct));
    }

    private static async Task<IResult> AdminBootstrapAsync(
        HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
        await directory.EnsureBootstrapAsync(actor, Array.Empty<Guid>(), tenant.Current?.TenantId.Value, ct);
        return Results.Json(new { ok = true });
    }

    #endregion

    #region Admin seller-scoped

    private static async Task<IResult> AdminGetCeilingAsync(
        Guid sellerId, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
        await EnsureCapabilityAsync(actor, "accesscontrol.view", authz, tenant, ct);
        return Results.Json(await directory.GetSellerCeilingAsync(sellerId, ct));
    }

    private sealed record CeilingBody(List<CeilingEntry> Entries);
    private sealed record CeilingEntry(
        string PermissionId,
        bool Enabled,
        AccessScopeKind ScopeKind = AccessScopeKind.GlobalWithinOwner,
        Guid? ScopeResourceId = null);

    private static async Task<IResult> AdminSetCeilingAsync(
        Guid sellerId, CeilingBody body, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant,
        IAuthorizationGuard guard, IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
            await EnsureCapabilityAsync(actor, "accesscontrol.manage", authz, tenant, ct);
            await directory.SetSellerCeilingAsync(
                sellerId,
                body.Entries.Select(e => (e.PermissionId, e.Enabled, e.ScopeKind, e.ScopeResourceId)).ToList(),
                actor,
                Trace(request),
                ct);
            return Results.NoContent();
        }
        catch (Exception ex) when (ex is AccessControlException)
        {
            return MapError(ex);
        }
    }

    private static async Task<IResult> AdminSellerListRolesAsync(
        Guid sellerId, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
        await EnsureCapabilityAsync(actor, "accesscontrol.view", authz, tenant, ct);
        return Results.Json(await directory.ListRolesAsync(SellerScope(sellerId, tenant), false, ct));
    }

    private static async Task<IResult> AdminSellerCreateRoleAsync(
        Guid sellerId, CreateAccessRoleCommand body, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant,
        IAuthorizationGuard guard, IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
            await EnsureCapabilityAsync(actor, "accesscontrol.manage", authz, tenant, ct);
            return Results.Json(await directory.CreateRoleAsync(SellerScope(sellerId, tenant), body, actor, Trace(request), ct));
        }
        catch (Exception ex) when (ex is AccessControlException)
        {
            return MapError(ex);
        }
    }

    private static async Task<IResult> AdminSellerUpdateRoleAsync(
        Guid sellerId, Guid roleId, UpdateAccessRoleCommand body, HttpRequest request, CurrentAuthenticatedSession session,
        ICurrentTenant tenant, IAuthorizationGuard guard, IAuthorizationService authz, IHostEnvironment env,
        IAccessControlDirectory directory, CancellationToken ct)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
            await EnsureCapabilityAsync(actor, "accesscontrol.manage", authz, tenant, ct);
            return Results.Json(await directory.UpdateRoleAsync(roleId, SellerScope(sellerId, tenant), body, actor, Trace(request), ct));
        }
        catch (Exception ex) when (ex is AccessControlException)
        {
            return MapError(ex);
        }
    }

    private static async Task<IResult> AdminSellerCloneRoleAsync(
        Guid sellerId, Guid roleId, CloneAccessRoleCommand body, HttpRequest request, CurrentAuthenticatedSession session,
        ICurrentTenant tenant, IAuthorizationGuard guard, IAuthorizationService authz, IHostEnvironment env,
        IAccessControlDirectory directory, CancellationToken ct)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
            await EnsureCapabilityAsync(actor, "accesscontrol.manage", authz, tenant, ct);
            return Results.Json(await directory.CloneRoleAsync(roleId, SellerScope(sellerId, tenant), body, actor, Trace(request), ct));
        }
        catch (Exception ex) when (ex is AccessControlException)
        {
            return MapError(ex);
        }
    }

    private static async Task<IResult> AdminSellerArchiveRoleAsync(
        Guid sellerId, Guid roleId, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant,
        IAuthorizationGuard guard, IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
            await EnsureCapabilityAsync(actor, "accesscontrol.manage", authz, tenant, ct);
            await directory.ArchiveRoleAsync(roleId, SellerScope(sellerId, tenant), actor, Trace(request), ct);
            return Results.NoContent();
        }
        catch (Exception ex) when (ex is AccessControlException)
        {
            return MapError(ex);
        }
    }

    private static async Task<IResult> AdminSellerGetPermsAsync(
        Guid sellerId, Guid roleId, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant,
        IAuthorizationGuard guard, IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
            await EnsureCapabilityAsync(actor, "accesscontrol.view", authz, tenant, ct);
            return Results.Json(await directory.GetRolePermissionsAsync(roleId, SellerScope(sellerId, tenant), ct));
        }
        catch (Exception ex) when (ex is AccessControlException)
        {
            return MapError(ex);
        }
    }

    private static async Task<IResult> AdminSellerSetPermsAsync(
        Guid sellerId, Guid roleId, List<RolePermissionGrant> body, HttpRequest request, CurrentAuthenticatedSession session,
        ICurrentTenant tenant, IAuthorizationGuard guard, IAuthorizationService authz, IHostEnvironment env,
        IAccessControlDirectory directory, CancellationToken ct)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
            await EnsureCapabilityAsync(actor, "accesscontrol.manage", authz, tenant, ct);
            await directory.SetRolePermissionsAsync(roleId, SellerScope(sellerId, tenant), body, actor, Trace(request), ct);
            return Results.NoContent();
        }
        catch (Exception ex) when (ex is AccessControlException)
        {
            return MapError(ex);
        }
    }

    private static async Task<IResult> AdminSellerListAssignmentsAsync(
        Guid sellerId, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
        await EnsureCapabilityAsync(actor, "accesscontrol.view", authz, tenant, ct);
        return Results.Json(await directory.ListAssignmentsAsync(SellerScope(sellerId, tenant), null, ct));
    }

    private static async Task<IResult> AdminSellerAssignAsync(
        Guid sellerId, AssignBody body, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant,
        IAuthorizationGuard guard, IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
            await EnsureCapabilityAsync(actor, "accesscontrol.manage", authz, tenant, ct);
            return Results.Json(await directory.AssignRoleAsync(SellerScope(sellerId, tenant), body.UserId, body.RoleId, actor, Trace(request), ct));
        }
        catch (Exception ex) when (ex is AccessControlException)
        {
            return MapError(ex);
        }
    }

    private static async Task<IResult> AdminSellerRemoveAssignmentAsync(
        Guid sellerId, Guid assignmentId, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant,
        IAuthorizationGuard guard, IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
            await EnsureCapabilityAsync(actor, "accesscontrol.manage", authz, tenant, ct);
            await directory.RemoveAssignmentAsync(assignmentId, SellerScope(sellerId, tenant), actor, Trace(request), ct);
            return Results.NoContent();
        }
        catch (Exception ex) when (ex is AccessControlException)
        {
            return MapError(ex);
        }
    }

    private static async Task<IResult> AdminSellerEffectiveAsync(
        Guid sellerId, Guid userId, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant,
        IAuthorizationGuard guard, IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
        await EnsureCapabilityAsync(actor, "accesscontrol.view", authz, tenant, ct);
        return Results.Json(await directory.GetEffectiveAccessAsync(userId, SellerScope(sellerId, tenant), ct));
    }

    #endregion

    #region Seller

    private static async Task<(Guid Actor, Guid SellerId)> RequireSellerAsync(
        HttpRequest request, CurrentAuthenticatedSession session, IAuthorizationGuard guard, IHostEnvironment env, CancellationToken ct)
    {
        var ctx = await SellerPanelAccess.RequireAuthorizedAsync(request, session, guard, env, ct);
        return (ctx.ActorUserId, ctx.SellerPartyId);
    }

    private static async Task<IResult> SellerListCatalogAsync(
        HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        var (actor, sellerId) = await RequireSellerAsync(request, session, guard, env, ct);
        await EnsureCapabilityAsync(actor, "accesscontrol.view", authz, tenant, ct);
        var ceiling = await directory.GetSellerCeilingAsync(sellerId, ct);
        var catalog = directory.ListCatalog()
            .Select(p => new
            {
                p.PermissionId,
                p.Module,
                p.DisplayNameKey,
                p.DescriptionKey,
                p.Delegable,
                p.ScopeKinds,
                DisabledByCeiling = p.Delegable && ceiling.All(c => c.PermissionId != p.PermissionId || !c.Enabled),
                PlatformOnly = !p.Delegable,
            });
        return Results.Json(catalog);
    }

    private static async Task<IResult> SellerGetCeilingAsync(
        HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        var (actor, sellerId) = await RequireSellerAsync(request, session, guard, env, ct);
        await EnsureCapabilityAsync(actor, "accesscontrol.view", authz, tenant, ct);
        return Results.Json(await directory.GetSellerCeilingAsync(sellerId, ct));
    }

    private static async Task<IResult> SellerListRolesAsync(
        HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        var (actor, sellerId) = await RequireSellerAsync(request, session, guard, env, ct);
        await EnsureCapabilityAsync(actor, "accesscontrol.view", authz, tenant, ct);
        return Results.Json(await directory.ListRolesAsync(SellerScope(sellerId, tenant), false, ct));
    }

    private static async Task<IResult> SellerCreateRoleAsync(
        CreateAccessRoleCommand body, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant,
        IAuthorizationGuard guard, IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        try
        {
            var (actor, sellerId) = await RequireSellerAsync(request, session, guard, env, ct);
            await EnsureCapabilityAsync(actor, "accesscontrol.manage", authz, tenant, ct);
            return Results.Json(await directory.CreateRoleAsync(SellerScope(sellerId, tenant), body, actor, Trace(request), ct));
        }
        catch (Exception ex) when (ex is AccessControlException)
        {
            return MapError(ex);
        }
    }

    private static async Task<IResult> SellerGetRoleAsync(
        Guid roleId, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        try
        {
            var (actor, sellerId) = await RequireSellerAsync(request, session, guard, env, ct);
            await EnsureCapabilityAsync(actor, "accesscontrol.view", authz, tenant, ct);
            var role = await directory.GetRoleAsync(roleId, SellerScope(sellerId, tenant), ct);
            return role is null ? Results.NotFound() : Results.Json(role);
        }
        catch (Exception ex) when (ex is AccessControlException)
        {
            return MapError(ex);
        }
    }

    private static async Task<IResult> SellerUpdateRoleAsync(
        Guid roleId, UpdateAccessRoleCommand body, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant,
        IAuthorizationGuard guard, IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        try
        {
            var (actor, sellerId) = await RequireSellerAsync(request, session, guard, env, ct);
            await EnsureCapabilityAsync(actor, "accesscontrol.manage", authz, tenant, ct);
            return Results.Json(await directory.UpdateRoleAsync(roleId, SellerScope(sellerId, tenant), body, actor, Trace(request), ct));
        }
        catch (Exception ex) when (ex is AccessControlException)
        {
            return MapError(ex);
        }
    }

    private static async Task<IResult> SellerCloneRoleAsync(
        Guid roleId, CloneAccessRoleCommand body, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant,
        IAuthorizationGuard guard, IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        try
        {
            var (actor, sellerId) = await RequireSellerAsync(request, session, guard, env, ct);
            await EnsureCapabilityAsync(actor, "accesscontrol.manage", authz, tenant, ct);
            return Results.Json(await directory.CloneRoleAsync(roleId, SellerScope(sellerId, tenant), body, actor, Trace(request), ct));
        }
        catch (Exception ex) when (ex is AccessControlException)
        {
            return MapError(ex);
        }
    }

    private static async Task<IResult> SellerArchiveRoleAsync(
        Guid roleId, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        try
        {
            var (actor, sellerId) = await RequireSellerAsync(request, session, guard, env, ct);
            await EnsureCapabilityAsync(actor, "accesscontrol.manage", authz, tenant, ct);
            await directory.ArchiveRoleAsync(roleId, SellerScope(sellerId, tenant), actor, Trace(request), ct);
            return Results.NoContent();
        }
        catch (Exception ex) when (ex is AccessControlException)
        {
            return MapError(ex);
        }
    }

    private static async Task<IResult> SellerGetRolePermissionsAsync(
        Guid roleId, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        try
        {
            var (actor, sellerId) = await RequireSellerAsync(request, session, guard, env, ct);
            await EnsureCapabilityAsync(actor, "accesscontrol.view", authz, tenant, ct);
            return Results.Json(await directory.GetRolePermissionsAsync(roleId, SellerScope(sellerId, tenant), ct));
        }
        catch (Exception ex) when (ex is AccessControlException)
        {
            return MapError(ex);
        }
    }

    private static async Task<IResult> SellerSetRolePermissionsAsync(
        Guid roleId, List<RolePermissionGrant> body, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant,
        IAuthorizationGuard guard, IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        try
        {
            var (actor, sellerId) = await RequireSellerAsync(request, session, guard, env, ct);
            await EnsureCapabilityAsync(actor, "accesscontrol.manage", authz, tenant, ct);
            await directory.SetRolePermissionsAsync(roleId, SellerScope(sellerId, tenant), body, actor, Trace(request), ct);
            return Results.NoContent();
        }
        catch (Exception ex) when (ex is AccessControlException)
        {
            return MapError(ex);
        }
    }

    private static async Task<IResult> SellerListAssignmentsAsync(
        HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        var (actor, sellerId) = await RequireSellerAsync(request, session, guard, env, ct);
        await EnsureCapabilityAsync(actor, "accesscontrol.view", authz, tenant, ct);
        return Results.Json(await directory.ListAssignmentsAsync(SellerScope(sellerId, tenant), null, ct));
    }

    private static async Task<IResult> SellerAssignAsync(
        AssignBody body, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        try
        {
            var (actor, sellerId) = await RequireSellerAsync(request, session, guard, env, ct);
            await EnsureCapabilityAsync(actor, "accesscontrol.manage", authz, tenant, ct);
            return Results.Json(await directory.AssignRoleAsync(SellerScope(sellerId, tenant), body.UserId, body.RoleId, actor, Trace(request), ct));
        }
        catch (Exception ex) when (ex is AccessControlException)
        {
            return MapError(ex);
        }
    }

    private static async Task<IResult> SellerRemoveAssignmentAsync(
        Guid assignmentId, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        try
        {
            var (actor, sellerId) = await RequireSellerAsync(request, session, guard, env, ct);
            await EnsureCapabilityAsync(actor, "accesscontrol.manage", authz, tenant, ct);
            await directory.RemoveAssignmentAsync(assignmentId, SellerScope(sellerId, tenant), actor, Trace(request), ct);
            return Results.NoContent();
        }
        catch (Exception ex) when (ex is AccessControlException)
        {
            return MapError(ex);
        }
    }

    private static async Task<IResult> SellerSearchUsersAsync(
        HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct, string? q = null)
    {
        var (actor, sellerId) = await RequireSellerAsync(request, session, guard, env, ct);
        await EnsureCapabilityAsync(actor, "accesscontrol.view", authz, tenant, ct);
        return Results.Json(await directory.SearchUsersInScopeAsync(SellerScope(sellerId, tenant), q, ct));
    }

    private static async Task<IResult> SellerEffectiveAsync(
        Guid userId, HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        var (actor, sellerId) = await RequireSellerAsync(request, session, guard, env, ct);
        await EnsureCapabilityAsync(actor, "accesscontrol.view", authz, tenant, ct);
        return Results.Json(await directory.GetEffectiveAccessAsync(userId, SellerScope(sellerId, tenant), ct));
    }

    private static async Task<IResult> AdminMeCapabilitiesAsync(
        HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
        return Results.Json(await directory.GetEffectiveAccessAsync(actor, PlatformScope(tenant), ct));
    }

    private static async Task<IResult> SellerMeCapabilitiesAsync(
        HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IHostEnvironment env, IAccessControlDirectory directory, CancellationToken ct)
    {
        var (actor, sellerId) = await RequireSellerAsync(request, session, guard, env, ct);
        return Results.Json(await directory.GetEffectiveAccessAsync(actor, SellerScope(sellerId, tenant), ct));
    }

    private static async Task<IResult> AdminListCategoriesAsync(
        HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, ICatalogLookupGateway catalog, CancellationToken ct, string? q = null)
    {
        var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
        await EnsureCapabilityAsync(actor, "accesscontrol.view", authz, tenant, ct);
        var items = await catalog.ListCategoriesForAccessControlAsync(q, ct);
        return Results.Json(new { deferred = false, items });
    }

    private static async Task<IResult> SellerListCategoriesAsync(
        HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, ICatalogLookupGateway catalog, CancellationToken ct, string? q = null)
    {
        var (actor, _) = await RequireSellerAsync(request, session, guard, env, ct);
        await EnsureCapabilityAsync(actor, "accesscontrol.view", authz, tenant, ct);
        var items = await catalog.ListCategoriesForAccessControlAsync(q, ct);
        return Results.Json(new { deferred = false, items });
    }

    private static async Task<IResult> AdminListBrandsAsync(
        HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, ICatalogLookupGateway catalog, CancellationToken ct, string? q = null)
    {
        var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
        await EnsureCapabilityAsync(actor, "accesscontrol.view", authz, tenant, ct);
        var items = await catalog.ListBrandsForAccessControlAsync(q, ct);
        return Results.Json(new { deferred = false, items });
    }

    private static async Task<IResult> SellerListBrandsAsync(
        HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, ICatalogLookupGateway catalog, CancellationToken ct, string? q = null)
    {
        var (actor, _) = await RequireSellerAsync(request, session, guard, env, ct);
        await EnsureCapabilityAsync(actor, "accesscontrol.view", authz, tenant, ct);
        var items = await catalog.ListBrandsForAccessControlAsync(q, ct);
        return Results.Json(new { deferred = false, items });
    }

    private static async Task<IResult> AdminListProductsAsync(
        HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, ICatalogLookupGateway catalog, CancellationToken ct, string? q = null)
    {
        var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
        await EnsureCapabilityAsync(actor, "accesscontrol.view", authz, tenant, ct);
        var items = await catalog.ListProductsForAccessControlAsync(q, ct);
        return Results.Json(new { deferred = false, items });
    }

    private static async Task<IResult> SellerListProductsAsync(
        HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, ICatalogLookupGateway catalog, CancellationToken ct, string? q = null)
    {
        var (actor, _) = await RequireSellerAsync(request, session, guard, env, ct);
        await EnsureCapabilityAsync(actor, "accesscontrol.view", authz, tenant, ct);
        var items = await catalog.ListProductsForAccessControlAsync(q, ct);
        return Results.Json(new { deferred = false, items });
    }

    private static async Task<IResult> AdminDeferredScopeAsync(
        HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, CancellationToken ct)
    {
        var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, env, ct);
        await EnsureCapabilityAsync(actor, "accesscontrol.view", authz, tenant, ct);
        return Results.Json(new { deferred = true, items = Array.Empty<object>() });
    }

    private static async Task<IResult> SellerDeferredScopeAsync(
        HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard,
        IAuthorizationService authz, IHostEnvironment env, CancellationToken ct)
    {
        var (actor, _) = await RequireSellerAsync(request, session, guard, env, ct);
        await EnsureCapabilityAsync(actor, "accesscontrol.view", authz, tenant, ct);
        return Results.Json(new { deferred = true, items = Array.Empty<object>() });
    }

    #endregion
}
