using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Security;
using Tooba.Party.Endpoints.Seller;

namespace Tooba.Host.Security.Seller;

/// <summary>
/// Host transport adapter for the Party seller-settings Endpoints auth seam.
/// <para>
/// The panel gate comes from the neutral Host seller seam (<see cref="ISellerPanelAccess"/>) and the
/// effective-permission projection from the neutral platform seam
/// (<see cref="IPlatformEffectiveAccessReader"/>). No Party Application/Domain/Infrastructure type,
/// no AccessControl Application/Domain type, no service locator and no business logic lives here.
/// </para>
/// </summary>
public sealed class HostPartySellerAuthorizer(
    ISellerPanelAccess sellerAccess,
    IPlatformEffectiveAccessReader effectiveAccess) : IPartySellerAuthorizer
{
    private const string ViewPermission = "seller.settings.view";
    private const string ManagePermission = "seller.settings.manage";

    /// <inheritdoc />
    public async Task<(Guid ActorUserId, Guid SellerPartyId, bool CanManage)> RequireViewAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var (actorUserId, sellerPartyId) = await sellerAccess.RequireAuthorizedAsync(
            httpContext.Request, cancellationToken);

        // View is mandatory: absence is fail-closed 403.
        await EnsureSellerCapabilityAsync(actorUserId, sellerPartyId, ViewPermission, cancellationToken);

        // Manage is optional on the read route: absence only yields CanManage=false, never a hard failure.
        var canManage = await HasSellerCapabilityAsync(
            actorUserId, sellerPartyId, ManagePermission, cancellationToken);
        return (actorUserId, sellerPartyId, canManage);
    }

    /// <inheritdoc />
    public async Task<(Guid ActorUserId, Guid SellerPartyId)> RequireManageAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var (actorUserId, sellerPartyId) = await sellerAccess.RequireAuthorizedAsync(
            httpContext.Request, cancellationToken);

        await EnsureSellerCapabilityAsync(actorUserId, sellerPartyId, ManagePermission, cancellationToken);
        return (actorUserId, sellerPartyId);
    }

    private async Task EnsureSellerCapabilityAsync(
        Guid actorUserId,
        Guid sellerPartyId,
        string permissionId,
        CancellationToken cancellationToken)
    {
        if (!await HasSellerCapabilityAsync(actorUserId, sellerPartyId, permissionId, cancellationToken))
        {
            throw new SemanticException(new SemanticError(SellerSecurityErrorCodes.AuthorizationDenied));
        }
    }

    private async Task<bool> HasSellerCapabilityAsync(
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
        return grants.Any(p =>
            p.PermissionId == permissionId
            && !p.DeniedByCeiling
            && p.ScopeKind == PlatformAccessScopeKind.GlobalWithinOwner);
    }
}
