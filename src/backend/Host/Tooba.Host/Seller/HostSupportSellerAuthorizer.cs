#pragma warning disable CS1591
using Tooba.AccessControl.Application;
using Tooba.AccessControl.Domain;
using Tooba.BuildingBlocks;
using Tooba.Host.Seller;
using Tooba.Support.Application.Errors;
using Tooba.Support.Endpoints.Seller;

using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Application.Permissions;
namespace Tooba.Host.Seller;

/// <summary>Host transport adapter for Support seller Endpoints auth + capabilities.</summary>
public sealed class HostSupportSellerAuthorizer : ISupportSellerAuthorizer
{
    private readonly IAccessControlDirectory _access;

    public HostSupportSellerAuthorizer(IAccessControlDirectory access) => _access = access;

    public async Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
        HttpContext httpContext, string permissionId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionId);

        var session = httpContext.RequestServices.GetRequiredService<CurrentAuthenticatedSession>();
        var guard = httpContext.RequestServices.GetRequiredService<IAuthorizationGuard>();
        var environment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();

        var (actorUserId, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
            httpContext.Request, session, guard, environment, cancellationToken);

        await EnsureSellerCapabilityAsync(actorUserId, sellerPartyId, permissionId, cancellationToken);
        return (actorUserId, sellerPartyId);
    }

    private async Task EnsureSellerCapabilityAsync(
        Guid actorUserId,
        Guid sellerPartyId,
        string permissionId,
        CancellationToken cancellationToken)
    {
        var effective = await _access.GetEffectiveAccessAsync(
            actorUserId,
            new AccessOwnerScope(AccessOwnerScopeKind.Seller, sellerPartyId),
            cancellationToken);
        var allowed = effective.Permissions.Any(p =>
            p.PermissionId == permissionId
            && !p.DeniedByCeiling
            && p.ScopeKind == AccessScopeKind.GlobalWithinOwner);
        if (!allowed)
            throw new PlatformHttpException(403, "مجوز پشتیبانی وجود ندارد.", SupportErrorCodes.SellerAuthorizationDenied);
    }
}
