using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.AccessControl.Application;
using Tooba.AccessControl.Application.Authorization;
using Tooba.AccessControl.Application.Roles.Commands;
using Tooba.AccessControl.Application.Assignments.Commands;
using Tooba.AccessControl.Application.Bootstrap.Commands;
using Tooba.AccessControl.Application.Permissions.Commands;
using Tooba.AccessControl.Application.Access.Queries;
using Tooba.AccessControl.Application.Roles.Queries;
using Tooba.AccessControl.Application.Permissions.Queries;
using Tooba.AccessControl.Application.Assignments.Queries;
using Tooba.AccessControl.Contracts.Enums;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Security;

using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Application.Permissions;
namespace Tooba.AccessControl.Endpoints.Admin;

/// <summary>مسیرهای نازک پنل مدیر برای Access Control.</summary>
public static class AccessControlAdminEndpoints
{
    /// <summary>مسیرهای admin را ثبت می‌کند.</summary>
    /// <param name="group">گروه مسیر admin/access-control.</param>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapPost("/bootstrap", BootstrapAsync);
        group.MapGet("/me/capabilities", MeCapabilitiesAsync);
        group.MapGet("/permissions", ListPermissionsAsync);
        group.MapGet("/roles", ListRolesAsync);
        group.MapGet("/roles/{roleId:guid}", GetRoleAsync);
        group.MapPost("/roles", CreateRoleAsync);
        group.MapPut("/roles/{roleId:guid}", UpdateRoleAsync);
        group.MapPost("/roles/{roleId:guid}/clone", CloneRoleAsync);
        group.MapDelete("/roles/{roleId:guid}", ArchiveRoleAsync);
        group.MapGet("/roles/{roleId:guid}/permissions", GetRolePermissionsAsync);
        group.MapPut("/roles/{roleId:guid}/permissions", SetRolePermissionsAsync);
        group.MapGet("/assignments", ListAssignmentsAsync);
        group.MapPost("/assignments", AssignAsync);
        group.MapDelete("/assignments/{assignmentId:guid}", RemoveAssignmentAsync);
        group.MapGet("/users/{userId:guid}/effective", EffectiveAsync);
        group.MapGet("/users", SearchUsersAsync);
        MapScopeResources(group.MapGroup("/scope-resources"));
    }

    private static void MapScopeResources(RouteGroupBuilder group)
    {
        group.MapGet("/categories", (HttpRequest request, ISender sender, IAdminPanelAccess access, IAuthorizationService authz, ICurrentTenant tenant, ApiResponseFactory api, CancellationToken ct, string? q = null) =>
            ListScopeResourceAsync(request, sender, access, authz, tenant, api, AccessScopeResourceKind.Category, q, ct));
        group.MapGet("/brands", (HttpRequest request, ISender sender, IAdminPanelAccess access, IAuthorizationService authz, ICurrentTenant tenant, ApiResponseFactory api, CancellationToken ct, string? q = null) =>
            ListScopeResourceAsync(request, sender, access, authz, tenant, api, AccessScopeResourceKind.Brand, q, ct));
        group.MapGet("/products", (HttpRequest request, ISender sender, IAdminPanelAccess access, IAuthorizationService authz, ICurrentTenant tenant, ApiResponseFactory api, CancellationToken ct, string? q = null) =>
            ListScopeResourceAsync(request, sender, access, authz, tenant, api, AccessScopeResourceKind.Product, q, ct));
        group.MapGet("/warehouses", (HttpRequest request, ISender sender, IAdminPanelAccess access, IAuthorizationService authz, ICurrentTenant tenant, ApiResponseFactory api, CancellationToken ct) =>
            ListScopeResourceAsync(request, sender, access, authz, tenant, api, AccessScopeResourceKind.Warehouse, null, ct));
        group.MapGet("/stores", (HttpRequest request, ISender sender, IAdminPanelAccess access, IAuthorizationService authz, ICurrentTenant tenant, ApiResponseFactory api, CancellationToken ct) =>
            ListScopeResourceAsync(request, sender, access, authz, tenant, api, AccessScopeResourceKind.Store, null, ct));
        group.MapGet("/order-segments", (HttpRequest request, ISender sender, IAdminPanelAccess access, IAuthorizationService authz, ICurrentTenant tenant, ApiResponseFactory api, CancellationToken ct) =>
            ListScopeResourceAsync(request, sender, access, authz, tenant, api, AccessScopeResourceKind.OrderSegment, null, ct));
    }

    private static async Task<IResult> ListScopeResourceAsync(
        HttpRequest request,
        ISender sender,
        IAdminPanelAccess adminPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        AccessScopeResourceKind kind,
        string? q,
        CancellationToken cancellationToken)
    {
        var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
        await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, cancellationToken);
        return api.From(await sender.Send(new ListScopeResourcesQuery(kind, q), cancellationToken));
    }

    private static async Task<IResult> SearchUsersAsync(
        HttpRequest request,
        ISender sender,
        IAdminPanelAccess adminPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken,
        string? q = null)
    {
        var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
        await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, cancellationToken);
        return api.From(await sender.Send(
            new SearchAccessUsersQuery(
                AccessOwnerScopeKind.Platform,
                null,
                tenant.Current?.TenantId.Value,
                q),
            cancellationToken));
    }

    private static async Task<IResult> ListAssignmentsAsync(
        HttpRequest request,
        ISender sender,
        IAdminPanelAccess adminPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken,
        Guid? userId = null)
    {
        var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
        await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, cancellationToken);
        return api.From(await sender.Send(
            new ListAssignmentsQuery(
                AccessOwnerScopeKind.Platform,
                null,
                tenant.Current?.TenantId.Value,
                userId),
            cancellationToken));
    }

    private static async Task<IResult> AssignAsync(
        AdminAssignBody body,
        HttpRequest request,
        ISender sender,
        IAdminPanelAccess adminPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
            var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
            await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.manage", authz, tenant, cancellationToken);
            return api.From(await sender.Send(
                new AssignRoleCommand(
                    AccessOwnerScopeKind.Platform,
                    null,
                    body.UserId,
                    body.RoleId,
                    actor,
                    tenant.Current?.TenantId.Value,
                    Trace(request)),
                cancellationToken));
    }

    private static async Task<IResult> RemoveAssignmentAsync(
        Guid assignmentId,
        HttpRequest request,
        ISender sender,
        IAdminPanelAccess adminPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
            var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
            await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.manage", authz, tenant, cancellationToken);
            return api.From(await sender.Send(new RemoveAssignmentCommand(
                    assignmentId,
                    AccessOwnerScopeKind.Platform,
                    null,
                    actor,
                    tenant.Current?.TenantId.Value,
                    Trace(request)), cancellationToken));
    }

    private static async Task<IResult> EffectiveAsync(
        Guid userId,
        HttpRequest request,
        ISender sender,
        IAdminPanelAccess adminPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
        await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, cancellationToken);
        return api.From(await sender.Send(
            new GetEffectiveAccessQuery(
                userId,
                AccessOwnerScopeKind.Platform,
                null,
                tenant.Current?.TenantId.Value),
            cancellationToken));
    }

    private static async Task<IResult> BootstrapAsync(
        HttpRequest request,
        ISender sender,
        IAdminPanelAccess adminPanelAccess,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
        return api.From(await sender.Send(new EnsureAccessControlBootstrapCommand(actor, tenant.Current?.TenantId.Value), cancellationToken));
    }

    private static async Task<IResult> MeCapabilitiesAsync(
        HttpRequest request,
        ISender sender,
        IAdminPanelAccess adminPanelAccess,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
        return api.From(await sender.Send(
            new GetEffectiveAccessQuery(
                actor,
                AccessOwnerScopeKind.Platform,
                null,
                tenant.Current?.TenantId.Value),
            cancellationToken));
    }

    private static async Task<IResult> ListPermissionsAsync(
        HttpRequest request,
        ISender sender,
        IAdminPanelAccess adminPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
        await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, cancellationToken);
        return api.From(await sender.Send(new ListPermissionCatalogQuery(), cancellationToken));
    }

    private static async Task<IResult> ListRolesAsync(
        HttpRequest request,
        ISender sender,
        IAdminPanelAccess adminPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken,
        bool includeArchived = false)
    {
        var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
        await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, cancellationToken);
        return api.From(await sender.Send(
            new ListRolesQuery(
                AccessOwnerScopeKind.Platform,
                null,
                tenant.Current?.TenantId.Value,
                includeArchived),
            cancellationToken));
    }

    private static async Task<IResult> GetRoleAsync(
        Guid roleId,
        HttpRequest request,
        ISender sender,
        IAdminPanelAccess adminPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
            var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
            await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, cancellationToken);
            return api.From(await sender.Send(new GetRoleQuery(
                    roleId,
                    AccessOwnerScopeKind.Platform,
                    null,
                    tenant.Current?.TenantId.Value), cancellationToken));
    }

    private static async Task<IResult> CreateRoleAsync(
        CreateAccessRoleCommand body,
        HttpRequest request,
        ISender sender,
        IAdminPanelAccess adminPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
            var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
            await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.manage", authz, tenant, cancellationToken);
            return api.From(await sender.Send(
                new CreateRoleCommand(
                    AccessOwnerScopeKind.Platform,
                    null,
                    actor,
                    tenant.Current?.TenantId.Value,
                    body.Name,
                    body.Code,
                    body.Description,
                    Trace(request)),
                cancellationToken));
    }

    private static async Task<IResult> UpdateRoleAsync(
        Guid roleId,
        UpdateAccessRoleCommand body,
        HttpRequest request,
        ISender sender,
        IAdminPanelAccess adminPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
            var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
            await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.manage", authz, tenant, cancellationToken);
            return api.From(await sender.Send(
                new UpdateRoleCommand(
                    roleId,
                    AccessOwnerScopeKind.Platform,
                    null,
                    actor,
                    tenant.Current?.TenantId.Value,
                    body.Name,
                    body.Description,
                    Trace(request)),
                cancellationToken));
    }

    private static async Task<IResult> CloneRoleAsync(
        Guid roleId,
        CloneAccessRoleCommand body,
        HttpRequest request,
        ISender sender,
        IAdminPanelAccess adminPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
            var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
            await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.manage", authz, tenant, cancellationToken);
            return api.From(await sender.Send(
                new CloneRoleCommand(
                    roleId,
                    AccessOwnerScopeKind.Platform,
                    null,
                    actor,
                    tenant.Current?.TenantId.Value,
                    body.Name,
                    body.Code,
                    body.Description,
                    Trace(request)),
                cancellationToken));
    }

    private static async Task<IResult> ArchiveRoleAsync(
        Guid roleId,
        HttpRequest request,
        ISender sender,
        IAdminPanelAccess adminPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
            var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
            await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.manage", authz, tenant, cancellationToken);
            return api.From(await sender.Send(new ArchiveRoleCommand(
                    roleId,
                    AccessOwnerScopeKind.Platform,
                    null,
                    actor,
                    tenant.Current?.TenantId.Value,
                    Trace(request)), cancellationToken));
    }

    private static async Task<IResult> GetRolePermissionsAsync(
        Guid roleId,
        HttpRequest request,
        ISender sender,
        IAdminPanelAccess adminPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
            var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
            await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, cancellationToken);
            return api.From(await sender.Send(
                new GetRolePermissionsQuery(
                    roleId,
                    AccessOwnerScopeKind.Platform,
                    null,
                    tenant.Current?.TenantId.Value),
                cancellationToken));
    }

    private static async Task<IResult> SetRolePermissionsAsync(
        Guid roleId,
        List<RolePermissionGrant> body,
        HttpRequest request,
        ISender sender,
        IAdminPanelAccess adminPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
            var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
            await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.manage", authz, tenant, cancellationToken);
            return api.From(await sender.Send(new SetRolePermissionsCommand(
                    roleId,
                    AccessOwnerScopeKind.Platform,
                    null,
                    actor,
                    tenant.Current?.TenantId.Value,
                    body,
                    Trace(request)), cancellationToken));
    }

    private static string? Trace(HttpRequest request) =>
        request.Headers.TryGetValue("X-Request-Id", out var v) ? v.ToString() : null;

    /// <summary>بدنهٔ تخصیص نقش به کاربر در سطح Platform.</summary>
    private sealed record AdminAssignBody(Guid UserId, Guid RoleId);
}
