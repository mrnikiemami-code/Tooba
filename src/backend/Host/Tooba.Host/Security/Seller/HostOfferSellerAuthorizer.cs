using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Security;
using Tooba.Offer.Endpoints.Seller;

namespace Tooba.Host.Security.Seller;

/// <summary>
/// Host transport adapter for the Offer seller auth seam. Reads keep the existing seller view
/// scope; writes additionally require the existing product.create/product.edit capability.
/// </summary>
public sealed class HostOfferSellerAuthorizer(
    ISellerPanelAccess sellerAccess,
    IPlatformEffectiveAccessReader effectiveAccess) : IOfferSellerAuthorizer
{
    /// <inheritdoc />
    public Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return sellerAccess.RequireAuthorizedAsync(httpContext.Request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<(Guid ActorUserId, Guid SellerPartyId)> RequireCreateAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
        => RequireCapabilityAsync(httpContext, "product.create", cancellationToken);

    /// <inheritdoc />
    public Task<(Guid ActorUserId, Guid SellerPartyId)> RequireEditAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
        => RequireCapabilityAsync(httpContext, "product.edit", cancellationToken);

    private async Task<(Guid ActorUserId, Guid SellerPartyId)> RequireCapabilityAsync(
        HttpContext httpContext,
        string permissionId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        var (actor, seller) = await sellerAccess.RequireAuthorizedAsync(httpContext.Request, cancellationToken);
        var grants = await effectiveAccess.GetEffectivePermissionsAsync(
            actor,
            PlatformAccessOwnerKind.Seller,
            seller,
            cancellationToken);
        var allowed = grants.Any(grant =>
            grant.PermissionId == permissionId
            && !grant.DeniedByCeiling
            && grant.ScopeKind == PlatformAccessScopeKind.GlobalWithinOwner);
        if (!allowed)
        {
            throw new SemanticException(new SemanticError(SellerSecurityErrorCodes.AuthorizationDenied));
        }
        return (actor, seller);
    }
}
