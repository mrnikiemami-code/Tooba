using Tooba.AccessControl.Application.Development.Seller;
using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Contracts.Development;
using Tooba.AccessControl.Domain;

namespace Tooba.AccessControl.Infrastructure.Development;

/// <summary>
/// AccessControl-owned adapter implementing the Contracts development seed prelude.
/// Translates neutral Host calls into seller-dev-context + directory bootstrap/tuple sync.
/// </summary>
internal sealed class AccessControlDevelopmentSeedPrelude : IAccessControlDevelopmentSeedPrelude
{
    private readonly ISellerDevContextStore _sellerDevContexts;
    private readonly IAccessControlDirectory _access;

    public AccessControlDevelopmentSeedPrelude(
        ISellerDevContextStore sellerDevContexts,
        IAccessControlDirectory access)
    {
        _sellerDevContexts = sellerDevContexts;
        _access = access;
    }

    /// <inheritdoc />
    public async Task<AccessControlDevelopmentSeedActors?> EnsureSupportSeedPrerequisitesAsync(
        Guid adminActorUserId,
        string tenantId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        await _sellerDevContexts.EnsureAsync(cancellationToken).ConfigureAwait(false);
        var seller = _sellerDevContexts.Current;
        if (seller is null)
            return null;

        await _access.EnsureBootstrapAsync(
            adminActorUserId,
            [seller.ActorA.SellerPartyId],
            tenantId,
            cancellationToken).ConfigureAwait(false);

        await _access.SyncUserCapabilityTuplesAsync(
            seller.ActorA.ActorUserId,
            new AccessOwnerScope(
                AccessOwnerScopeKind.Seller,
                seller.ActorA.SellerPartyId,
                tenantId),
            cancellationToken).ConfigureAwait(false);

        return new AccessControlDevelopmentSeedActors(
            seller.ActorA.SellerPartyId,
            seller.ActorA.ActorUserId);
    }
}
