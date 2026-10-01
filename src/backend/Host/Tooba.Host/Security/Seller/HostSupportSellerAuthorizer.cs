using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Security;
using Tooba.Support.Endpoints.Seller;

namespace Tooba.Host.Security.Seller;

/// <summary>
/// Host transport adapter for Support seller Endpoints auth + capabilities.
/// The panel gate comes from the neutral Host seller seam; the effective-permission
/// projection comes from the neutral platform effective-access seam. No AccessControl
/// Application/Domain type is referenced.
/// </summary>
public sealed class HostSupportSellerAuthorizer(
    ISellerPanelAccess sellerAccess,
    IPlatformEffectiveAccessReader effectiveAccess) : ISupportSellerAuthorizer
{
    /// <inheritdoc />
    public async Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
        HttpContext httpContext, string permissionId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionId);

        var (actorUserId, sellerPartyId) = await sellerAccess.RequireAuthorizedAsync(
            httpContext.Request, cancellationToken);

        await EnsureSellerCapabilityAsync(actorUserId, sellerPartyId, permissionId, cancellationToken);
        return (actorUserId, sellerPartyId);
    }

    private async Task EnsureSellerCapabilityAsync(
        Guid actorUserId,
        Guid sellerPartyId,
        string permissionId,
        CancellationToken cancellationToken)
    {
        var grants = await effectiveAccess.GetEffectivePermissionsAsync(
            actorUserId,
            PlatformAccessOwnerKind.Seller,
            sellerPartyId,
            cancellationToken);
        var allowed = grants.Any(p =>
            p.PermissionId == permissionId
            && !p.DeniedByCeiling
            && p.ScopeKind == PlatformAccessScopeKind.GlobalWithinOwner);
        if (!allowed)
        {
            throw new SemanticException(new SemanticError(SellerSecurityErrorCodes.AuthorizationDenied));
        }
    }
}
