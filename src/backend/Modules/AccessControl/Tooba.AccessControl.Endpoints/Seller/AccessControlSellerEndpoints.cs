using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.AccessControl.Application;
using Tooba.AccessControl.Application.Authorization;
using Tooba.AccessControl.Application.Commands.ArchiveRole;
using Tooba.AccessControl.Application.Commands.AssignRole;
using Tooba.AccessControl.Application.Commands.CloneRole;
using Tooba.AccessControl.Application.Commands.CreateRole;
using Tooba.AccessControl.Application.Commands.RemoveAssignment;
using Tooba.AccessControl.Application.Commands.SetRolePermissions;
using Tooba.AccessControl.Application.Commands.UpdateRole;
using Tooba.AccessControl.Application.Queries.GetEffectiveAccess;
using Tooba.AccessControl.Application.Queries.GetRole;
using Tooba.AccessControl.Application.Queries.GetRolePermissions;
using Tooba.AccessControl.Application.Queries.GetSellerCeiling;
using Tooba.AccessControl.Application.Queries.ListAssignments;
using Tooba.AccessControl.Application.Queries.ListRoles;
using Tooba.AccessControl.Application.Queries.ListSellerPermissionCatalog;
using Tooba.AccessControl.Application.Queries.SearchAccessUsers;
using Tooba.AccessControl.Domain;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Security;

namespace Tooba.AccessControl.Endpoints.Seller;

/// <summary>مسیرهای نازک پنل فروشنده برای Access Control.</summary>
public static class AccessControlSellerEndpoints
{
    /// <summary>مسیرهای seller را ثبت می‌کند.</summary>
    /// <param name="group">گروه مسیر seller/access-control.</param>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/me/capabilities", MeCapabilitiesAsync);
        group.MapGet("/permissions", ListPermissionsAsync);
        group.MapGet("/roles", ListRolesAsync);
        group.MapPost("/roles", CreateRoleAsync);
        group.MapGet("/roles/{roleId:guid}", GetRoleAsync);
        group.MapPut("/roles/{roleId:guid}", UpdateRoleAsync);
        group.MapPost("/roles/{roleId:guid}/clone", CloneRoleAsync);
        group.MapDelete("/roles/{roleId:guid}", ArchiveRoleAsync);
        group.MapGet("/roles/{roleId:guid}/permissions", GetRolePermissionsAsync);
        group.MapPut("/roles/{roleId:guid}/permissions", SetRolePermissionsAsync);
        group.MapGet("/ceiling", GetCeilingAsync);
        group.MapGet("/assignments", ListAssignmentsAsync);
        group.MapPost("/assignments", AssignAsync);
        group.MapDelete("/assignments/{assignmentId:guid}", RemoveAssignmentAsync);
        group.MapGet("/users/{userId:guid}/effective", EffectiveAsync);
        group.MapGet("/users", SearchUsersAsync);
    }

    private static async Task<IResult> SearchUsersAsync(
        HttpRequest request,
        ISender sender,
        ISellerPanelAccess sellerPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        CancellationToken cancellationToken,
        string? q = null)
    {
        var (actor, sellerId) = await sellerPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
        await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, cancellationToken);
        return Results.Json(await sender.Send(
            new SearchAccessUsersQuery(
                AccessOwnerScopeKind.Seller,
                sellerId,
                tenant.Current?.TenantId.Value,
                q),
            cancellationToken));
    }

    private static async Task<IResult> EffectiveAsync(
        Guid userId,
        HttpRequest request,
        ISender sender,
        ISellerPanelAccess sellerPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        CancellationToken cancellationToken)
    {
        var (actor, sellerId) = await sellerPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
        await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, cancellationToken);
        return Results.Json(await sender.Send(
            new GetEffectiveAccessQuery(
                userId,
                AccessOwnerScopeKind.Seller,
                sellerId,
                tenant.Current?.TenantId.Value),
            cancellationToken));
    }

    private static async Task<IResult> GetCeilingAsync(
        HttpRequest request,
        ISender sender,
        ISellerPanelAccess sellerPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        CancellationToken cancellationToken)
    {
        var (actor, sellerId) = await sellerPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
        await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, cancellationToken);
        return Results.Json(await sender.Send(new GetSellerCeilingQuery(sellerId), cancellationToken));
    }

    private static async Task<IResult> ListAssignmentsAsync(
        HttpRequest request,
        ISender sender,
        ISellerPanelAccess sellerPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        CancellationToken cancellationToken)
    {
        var (actor, sellerId) = await sellerPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
        await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, cancellationToken);
        return Results.Json(await sender.Send(
            new ListAssignmentsQuery(
                AccessOwnerScopeKind.Seller,
                sellerId,
                tenant.Current?.TenantId.Value,
                UserId: null),
            cancellationToken));
    }

    private static async Task<IResult> AssignAsync(
        SellerAssignBody body,
        HttpRequest request,
        ISender sender,
        ISellerPanelAccess sellerPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        CancellationToken cancellationToken)
    {
        try
        {
            var (actor, sellerId) = await sellerPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
            await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.manage", authz, tenant, cancellationToken);
            return Results.Json(await sender.Send(
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
        catch (AccessControlException ace)
        {
            return MapAccessError(ace);
        }
    }

    private static async Task<IResult> RemoveAssignmentAsync(
        Guid assignmentId,
        HttpRequest request,
        ISender sender,
        ISellerPanelAccess sellerPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        CancellationToken cancellationToken)
    {
        try
        {
            var (actor, sellerId) = await sellerPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
            await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.manage", authz, tenant, cancellationToken);
            await sender.Send(
                new RemoveAssignmentCommand(
                    assignmentId,
                    AccessOwnerScopeKind.Seller,
                    sellerId,
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

    private static async Task<IResult> ListRolesAsync(
        HttpRequest request,
        ISender sender,
        ISellerPanelAccess sellerPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        CancellationToken cancellationToken)
    {
        var (actor, sellerId) = await sellerPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
        await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, cancellationToken);
        return Results.Json(await sender.Send(
            new ListRolesQuery(
                AccessOwnerScopeKind.Seller,
                sellerId,
                tenant.Current?.TenantId.Value,
                IncludeArchived: false),
            cancellationToken));
    }

    private static async Task<IResult> CreateRoleAsync(
        CreateAccessRoleCommand body,
        HttpRequest request,
        ISender sender,
        ISellerPanelAccess sellerPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        CancellationToken cancellationToken)
    {
        try
        {
            var (actor, sellerId) = await sellerPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
            await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.manage", authz, tenant, cancellationToken);
            return Results.Json(await sender.Send(
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
        catch (AccessControlException ace)
        {
            return MapAccessError(ace);
        }
    }

    private static async Task<IResult> GetRoleAsync(
        Guid roleId,
        HttpRequest request,
        ISender sender,
        ISellerPanelAccess sellerPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        CancellationToken cancellationToken)
    {
        try
        {
            var (actor, sellerId) = await sellerPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
            await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, cancellationToken);
            var role = await sender.Send(
                new GetRoleQuery(
                    roleId,
                    AccessOwnerScopeKind.Seller,
                    sellerId,
                    tenant.Current?.TenantId.Value),
                cancellationToken);
            return role is null ? Results.NotFound() : Results.Json(role);
        }
        catch (AccessControlException ace)
        {
            return MapAccessError(ace);
        }
    }

    private static async Task<IResult> UpdateRoleAsync(
        Guid roleId,
        UpdateAccessRoleCommand body,
        HttpRequest request,
        ISender sender,
        ISellerPanelAccess sellerPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        CancellationToken cancellationToken)
    {
        try
        {
            var (actor, sellerId) = await sellerPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
            await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.manage", authz, tenant, cancellationToken);
            return Results.Json(await sender.Send(
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
        catch (AccessControlException ace)
        {
            return MapAccessError(ace);
        }
    }

    private static async Task<IResult> CloneRoleAsync(
        Guid roleId,
        CloneAccessRoleCommand body,
        HttpRequest request,
        ISender sender,
        ISellerPanelAccess sellerPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        CancellationToken cancellationToken)
    {
        try
        {
            var (actor, sellerId) = await sellerPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
            await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.manage", authz, tenant, cancellationToken);
            return Results.Json(await sender.Send(
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
        catch (AccessControlException ace)
        {
            return MapAccessError(ace);
        }
    }

    private static async Task<IResult> ArchiveRoleAsync(
        Guid roleId,
        HttpRequest request,
        ISender sender,
        ISellerPanelAccess sellerPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        CancellationToken cancellationToken)
    {
        try
        {
            var (actor, sellerId) = await sellerPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
            await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.manage", authz, tenant, cancellationToken);
            await sender.Send(
                new ArchiveRoleCommand(
                    roleId,
                    AccessOwnerScopeKind.Seller,
                    sellerId,
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
        ISellerPanelAccess sellerPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        CancellationToken cancellationToken)
    {
        try
        {
            var (actor, sellerId) = await sellerPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
            await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, cancellationToken);
            return Results.Json(await sender.Send(
                new GetRolePermissionsQuery(
                    roleId,
                    AccessOwnerScopeKind.Seller,
                    sellerId,
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
        ISellerPanelAccess sellerPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        CancellationToken cancellationToken)
    {
        try
        {
            var (actor, sellerId) = await sellerPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
            await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.manage", authz, tenant, cancellationToken);
            await sender.Send(
                new SetRolePermissionsCommand(
                    roleId,
                    AccessOwnerScopeKind.Seller,
                    sellerId,
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

    /// <summary>بدنهٔ تخصیص نقش به کاربر.</summary>
    private sealed record SellerAssignBody(Guid UserId, Guid RoleId);

    private static IResult MapAccessError(AccessControlException ace) =>
        Results.Json(
            new { title = ace.Message, code = ace.Code },
            statusCode: ace.Code.Contains("escalation", StringComparison.Ordinal)
                || ace.Code.Contains("ceiling", StringComparison.Ordinal)
                    ? 403
                    : 400);

    private static async Task<IResult> MeCapabilitiesAsync(
        HttpRequest request,
        ISender sender,
        ISellerPanelAccess sellerPanelAccess,
        ICurrentTenant tenant,
        CancellationToken cancellationToken)
    {
        var (actor, sellerId) = await sellerPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
        return Results.Json(await sender.Send(
            new GetEffectiveAccessQuery(
                actor,
                AccessOwnerScopeKind.Seller,
                sellerId,
                tenant.Current?.TenantId.Value),
            cancellationToken));
    }

    private static async Task<IResult> ListPermissionsAsync(
        HttpRequest request,
        ISender sender,
        ISellerPanelAccess sellerPanelAccess,
        IAuthorizationService authz,
        ICurrentTenant tenant,
        CancellationToken cancellationToken)
    {
        var (actor, sellerId) = await sellerPanelAccess.RequireAuthorizedAsync(request, cancellationToken);
        await AccessControlCapabilityGate.EnsureAsync(actor, "accesscontrol.view", authz, tenant, cancellationToken);
        return Results.Json(await sender.Send(new ListSellerPermissionCatalogQuery(sellerId), cancellationToken));
    }
}
