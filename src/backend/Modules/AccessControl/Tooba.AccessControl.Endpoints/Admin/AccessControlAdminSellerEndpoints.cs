using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.AccessControl.Application;
using Tooba.AccessControl.Application.Authorization;
using Tooba.AccessControl.Application.Roles.Commands;
using Tooba.AccessControl.Application.Assignments.Commands;
using Tooba.AccessControl.Application.Permissions.Commands;
using Tooba.AccessControl.Application.Ceiling.Commands;
using Tooba.AccessControl.Application.Access.Queries;
using Tooba.AccessControl.Application.Permissions.Queries;
using Tooba.AccessControl.Application.Ceiling.Queries;
using Tooba.AccessControl.Application.Assignments.Queries;
using Tooba.AccessControl.Application.Roles.Queries;
using Tooba.AccessControl.Contracts.Enums;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Security;

using Tooba.AccessControl.Application.Permissions.Models;
using Tooba.AccessControl.Application.Roles.Models;
using Tooba.AccessControl.Application.Permissions;
namespace Tooba.AccessControl.Endpoints.Admin;

/// <summary>مسیرهای نازک پنل مدیر برای نقش‌های محدودهٔ فروشنده.</summary>
public static class AccessControlAdminSellerEndpoints
{
    /// <summary>مسیرهای admin/sellers/{sellerId}/access-control را ثبت می‌کند.</summary>
    /// <param name="group">گروه مسیر AdminSeller.</param>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/roles", ListRolesAsync);
        group.MapPost("/roles", CreateRoleAsync);
        group.MapPut("/roles/{roleId:guid}", UpdateRoleAsync);
        group.MapPost("/roles/{roleId:guid}/clone", CloneRoleAsync);
        group.MapDelete("/roles/{roleId:guid}", ArchiveRoleAsync);
        group.MapGet("/roles/{roleId:guid}/permissions", GetRolePermissionsAsync);
        group.MapPut("/roles/{roleId:guid}/permissions", SetRolePermissionsAsync);
        group.MapGet("/ceiling", GetCeilingAsync);
        group.MapPut("/ceiling", SetCeilingAsync);
        group.MapGet("/assignments", ListAssignmentsAsync);
        group.MapPost("/assignments", AssignAsync);
        group.MapDelete("/assignments/{assignmentId:guid}", RemoveAssignmentAsync);
        group.MapGet("/users/{userId:guid}/effective", EffectiveAsync);
    }

    private static async Task<IResult> GetCeilingAsync(
        Guid sellerId,
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
        return api.From(await sender.Send(new GetSellerCeilingQuery(sellerId), cancellationToken));
    }

    private static async Task<IResult> SetCeilingAsync(
        Guid sellerId,
        AdminSellerCeilingBody body,
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
            return api.From(await sender.Send(new SetSellerCeilingCommand(
                    sellerId,
                    body.Entries
                        .Select(e => new SellerCeilingEntryInput(e.PermissionId, e.Enabled, e.ScopeKind, e.ScopeResourceId))
                        .ToList(),
                    actor,
                    Trace(request)), cancellationToken));
    }

    private static async Task<IResult> ListAssignmentsAsync(
        Guid sellerId,
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
            new ListAssignmentsQuery(
                AccessOwnerScopeKind.Seller,
                sellerId,
                tenant.Current?.TenantId.Value,
                UserId: null),
            cancellationToken));
    }

    private static async Task<IResult> AssignAsync(
        Guid sellerId,
        AdminSellerAssignBody body,
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
                    AccessOwnerScopeKind.Seller,
                    sellerId,
                    body.UserId,
                    body.RoleId,
                    actor,
                    tenant.Current?.TenantId.Value,
                    Trace(request)),
                cancellationToken));
    }

    private static async Task<IResult> RemoveAssignmentAsync(
        Guid sellerId,
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
                    AccessOwnerScopeKind.Seller,
                    sellerId,
                    actor,
                    tenant.Current?.TenantId.Value,
                    Trace(request)), cancellationToken));
    }

    private static async Task<IResult> EffectiveAsync(
        Guid sellerId,
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
                AccessOwnerScopeKind.Seller,
                sellerId,
                tenant.Current?.TenantId.Value),
            cancellationToken));
    }

    private static async Task<IResult> ListRolesAsync(
        Guid sellerId,
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
            new ListRolesQuery(
                AccessOwnerScopeKind.Seller,
                sellerId,
                tenant.Current?.TenantId.Value,
                IncludeArchived: false),
            cancellationToken));
    }

    private static async Task<IResult> CreateRoleAsync(
        Guid sellerId,
        CreateRoleRequest body,
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
                    AccessOwnerScopeKind.Seller,
                    sellerId,
                    actor,
                    tenant.Current?.TenantId.Value,
                    body.Name,
                    body.Code,
                    body.Description,
                    Trace(request)),
                cancellationToken));
    }

    private static async Task<IResult> UpdateRoleAsync(
        Guid sellerId,
        Guid roleId,
        UpdateRoleRequest body,
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
                    AccessOwnerScopeKind.Seller,
                    sellerId,
                    actor,
                    tenant.Current?.TenantId.Value,
                    body.Name,
                    body.Description,
                    Trace(request)),
                cancellationToken));
    }

    private static async Task<IResult> CloneRoleAsync(
        Guid sellerId,
        Guid roleId,
        CloneRoleRequest body,
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
                    AccessOwnerScopeKind.Seller,
                    sellerId,
                    actor,
                    tenant.Current?.TenantId.Value,
                    body.Name,
                    body.Code,
                    body.Description,
                    Trace(request)),
                cancellationToken));
    }

    private static async Task<IResult> ArchiveRoleAsync(
        Guid sellerId,
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
                    AccessOwnerScopeKind.Seller,
                    sellerId,
                    actor,
                    tenant.Current?.TenantId.Value,
                    Trace(request)), cancellationToken));
    }

    private static async Task<IResult> GetRolePermissionsAsync(
        Guid sellerId,
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
                    AccessOwnerScopeKind.Seller,
                    sellerId,
                    tenant.Current?.TenantId.Value),
                cancellationToken));
    }

    private static async Task<IResult> SetRolePermissionsAsync(
        Guid sellerId,
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
                    AccessOwnerScopeKind.Seller,
                    sellerId,
                    actor,
                    tenant.Current?.TenantId.Value,
                    body,
                    Trace(request)), cancellationToken));
    }

    private static string? Trace(HttpRequest request) =>
        request.Headers.TryGetValue("X-Request-Id", out var v) ? v.ToString() : null;

    /// <summary>بدنهٔ تنظیم سقف فروشنده — قرارداد JSON عمومی.</summary>
    private sealed record AdminSellerCeilingBody(List<AdminSellerCeilingEntry> Entries);

    /// <summary>ردیف سقف — قرارداد JSON عمومی.</summary>
    private sealed record AdminSellerCeilingEntry(
        string PermissionId,
        bool Enabled,
        AccessScopeKind ScopeKind = AccessScopeKind.GlobalWithinOwner,
        Guid? ScopeResourceId = null);

    /// <summary>بدنهٔ تخصیص نقش به کاربر.</summary>
    private sealed record AdminSellerAssignBody(Guid UserId, Guid RoleId);


}
