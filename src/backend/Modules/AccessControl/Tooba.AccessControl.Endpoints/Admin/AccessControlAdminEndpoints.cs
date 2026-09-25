using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.AccessControl.Application;
using Tooba.AccessControl.Application.Authorization;
using Tooba.AccessControl.Application.Commands.EnsureBootstrap;using Tooba.AccessControl.Application.Queries.GetEffectiveAccess;
using Tooba.AccessControl.Application.Queries.GetRole;
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
}
