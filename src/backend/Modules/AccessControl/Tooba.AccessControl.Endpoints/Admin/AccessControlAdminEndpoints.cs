using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.AccessControl.Application;
using Tooba.AccessControl.Application.Authorization;
using Tooba.AccessControl.Application.Commands.ArchiveRole;
using Tooba.AccessControl.Application.Commands.CloneRole;
using Tooba.AccessControl.Application.Commands.CreateRole;
using Tooba.AccessControl.Application.Commands.EnsureBootstrap;
using Tooba.AccessControl.Application.Commands.SetRolePermissions;
using Tooba.AccessControl.Application.Commands.UpdateRole;
using Tooba.AccessControl.Application.Queries.GetEffectiveAccess;
using Tooba.AccessControl.Application.Queries.GetRole;
using Tooba.AccessControl.Application.Queries.GetRolePermissions;
using Tooba.AccessControl.Application.Queries.ListPermissionCatalog;
using Tooba.AccessControl.Application.Queries.ListRoles;
using Tooba.AccessControl.Domain;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Security;

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
    }

    private static async Task<IResult> BootstrapAsync(
        HttpRequest request,
        ISender sender,
        IAdminPanelAccess adminPanelAccess,
        ICurrentTenant tenant,
        CancellationToken cancellationToken)
    {
        var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
        await sender.Send(
            new EnsureAccessControlBootstrapCommand(actor, tenant.Current?.TenantId.Value),
            cancellationToken);
        return Results.Json(new { ok = true });
    }

    private static async Task<IResult> MeCapabilitiesAsync(
        HttpRequest request,
        ISender sender,
        IAdminPanelAccess adminPanelAccess,
        ICurrentTenant tenant,
        CancellationToken cancellationToken)
    {
        var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
        return Results.Json(await sender.Send(
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
        CancellationToken cancellationToken)
    {
        var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
        await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, cancellationToken);
        return Results.Json(await sender.Send(new ListPermissionCatalogQuery(), cancellationToken));
    }

    private static async Task<IResult> ListRolesAsync(
        HttpRequest request,
        ISender sender,
        IAdminPanelAccess adminPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        CancellationToken cancellationToken,
        bool includeArchived = false)
    {
        var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
        await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, cancellationToken);
        return Results.Json(await sender.Send(
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
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
            await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, cancellationToken);
            var role = await sender.Send(
                new GetRoleQuery(
                    roleId,
                    AccessOwnerScopeKind.Platform,
                    null,
                    tenant.Current?.TenantId.Value),
                cancellationToken);
            return role is null ? Results.NotFound() : Results.Json(role);
        }
        catch (AccessControlException ace)
        {
            return Results.Json(new { title = ace.Message, code = ace.Code }, statusCode: ace.Code.Contains("escalation", StringComparison.Ordinal) || ace.Code.Contains("ceiling", StringComparison.Ordinal) ? 403 : 400);
        }
    }

    private static async Task<IResult> CreateRoleAsync(
        CreateAccessRoleCommand body,
        HttpRequest request,
        ISender sender,
        IAdminPanelAccess adminPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
            await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.manage", authz, tenant, cancellationToken);
            return Results.Json(await sender.Send(
                new CreateRoleCommand(
                    actor,
                    tenant.Current?.TenantId.Value,
                    body.Name,
                    body.Code,
                    body.Description,
                    Trace(request)),
                cancellationToken));
        }
        catch (Exception ex) when (ex is AccessControlException or PlatformHttpException)
        {
            return ex is PlatformHttpException ph
                ? Results.Json(new { title = ph.Title, code = ph.ErrorCode }, statusCode: ph.StatusCode)
                : MapAccessError(ex);
        }
    }

    private static async Task<IResult> UpdateRoleAsync(
        Guid roleId,
        UpdateAccessRoleCommand body,
        HttpRequest request,
        ISender sender,
        IAdminPanelAccess adminPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
            await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.manage", authz, tenant, cancellationToken);
            return Results.Json(await sender.Send(
                new UpdateRoleCommand(
                    roleId,
                    actor,
                    tenant.Current?.TenantId.Value,
                    body.Name,
                    body.Description,
                    Trace(request)),
                cancellationToken));
        }
        catch (Exception ex) when (ex is AccessControlException or PlatformHttpException)
        {
            return MapAccessError(ex);
        }
    }

    private static async Task<IResult> CloneRoleAsync(
        Guid roleId,
        CloneAccessRoleCommand body,
        HttpRequest request,
        ISender sender,
        IAdminPanelAccess adminPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
            await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.manage", authz, tenant, cancellationToken);
            return Results.Json(await sender.Send(
                new CloneRoleCommand(
                    roleId,
                    actor,
                    tenant.Current?.TenantId.Value,
                    body.Name,
                    body.Code,
                    body.Description,
                    Trace(request)),
                cancellationToken));
        }
        catch (AccessControlException ace)
        {
            return MapAccessError(ace);
        }
    }

    private static async Task<IResult> ArchiveRoleAsync(
        Guid roleId,
        HttpRequest request,
        ISender sender,
        IAdminPanelAccess adminPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
            await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.manage", authz, tenant, cancellationToken);
            await sender.Send(
                new ArchiveRoleCommand(
                    roleId,
                    actor,
                    tenant.Current?.TenantId.Value,
                    Trace(request)),
                cancellationToken);
            return Results.NoContent();
        }
        catch (AccessControlException ace)
        {
            return MapAccessError(ace);
        }
    }

    private static async Task<IResult> GetRolePermissionsAsync(
        Guid roleId,
        HttpRequest request,
        ISender sender,
        IAdminPanelAccess adminPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
            await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, cancellationToken);
            return Results.Json(await sender.Send(
                new GetRolePermissionsQuery(
                    roleId,
                    tenant.Current?.TenantId.Value),
                cancellationToken));
        }
        catch (AccessControlException ace)
        {
            return MapAccessError(ace);
        }
    }

    private static async Task<IResult> SetRolePermissionsAsync(
        Guid roleId,
        List<RolePermissionGrant> body,
        HttpRequest request,
        ISender sender,
        IAdminPanelAccess adminPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = await adminPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
            await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.manage", authz, tenant, cancellationToken);
            await sender.Send(
                new SetRolePermissionsCommand(
                    roleId,
                    actor,
                    tenant.Current?.TenantId.Value,
                    body,
                    Trace(request)),
                cancellationToken);
            return Results.NoContent();
        }
        catch (AccessControlException ace)
        {
            return MapAccessError(ace);
        }
    }

    private static string? Trace(HttpRequest request) =>
        request.Headers.TryGetValue("X-Request-Id", out var v) ? v.ToString() : null;

    private static IResult MapAccessError(Exception ex) =>
        ex is AccessControlException ace
            ? Results.Json(new { title = ace.Message, code = ace.Code }, statusCode: ace.Code.Contains("escalation", StringComparison.Ordinal) || ace.Code.Contains("ceiling", StringComparison.Ordinal) ? 403 : 400)
            : Results.Json(new { title = "access.error", code = "access.error" }, statusCode: 500);
}
