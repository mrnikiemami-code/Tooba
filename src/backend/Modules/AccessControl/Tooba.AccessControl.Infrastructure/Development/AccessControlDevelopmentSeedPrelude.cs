using Tooba.AccessControl.Application.Development.Seller;
using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Application.Ports;
using Tooba.AccessControl.Contracts.Development;
using Tooba.AccessControl.Contracts.Enums;

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

        // TB-SHIPPING-MVP-SCRATCH-BOOTSTRAP-062: bootstrap نقش مدیر پلتفرم به Context فروشنده وابسته نیست.
        // در یک فروشگاه Development تازه، سازمان‌های فروشنده ممکن است هنوز seed نشده باشند؛ پیش‌تر در آن حالت
        // این متد پیش از فراخوانی EnsureBootstrapAsync بازمی‌گشت و نقش platform-admin، role_permissions و
        // assignment مدیر هرگز ساخته نمی‌شد و همهٔ سطوح admin به‌صورت fail-closed با 403 باقی می‌ماندند.
        // اکنون bootstrap کاتالوگ/نقش مدیر همیشه و با همان منطق قرارداد اجرا می‌شود؛ تنها seed نمایشی Support
        // به حضور Context فروشنده گره خورده است.
        IReadOnlyList<Guid> sellerPartyIds = seller is null ? [] : [seller.ActorA.SellerPartyId];

        await _access.EnsureBootstrapAsync(
            adminActorUserId,
            sellerPartyIds,
            tenantId,
            cancellationToken).ConfigureAwait(false);

        if (seller is null)
            return null;

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

    /// <inheritdoc />
    public async Task<AccessControlMarketplaceSellerAccess?> EnsureMarketplaceSellerAccessAsync(
        Guid? platformAdminUserId,
        CancellationToken cancellationToken)
    {
        // The REAL identities are resolved by the existing dev-context store (Identity user + Party
        // membership). No fixed default actor id and no invented business tenant is used here.
        await _sellerDevContexts.EnsureAsync(cancellationToken).ConfigureAwait(false);
        var seller = _sellerDevContexts.Current;
        if (seller is null)
        {
            return null;
        }

        var pairs = new[] { seller.ActorA, seller.ActorB };
        var sellerPartyIds = pairs
            .Select(pair => pair.SellerPartyId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        if (sellerPartyIds.Length == 0)
        {
            return null;
        }

        // Existing authority: creates the seller-owner role + delegable catalog permissions + platform
        // seller ceilings per seller scope. Tenant is NULL on purpose (Marketplace has no store tenant).
        await _access.EnsureBootstrapAsync(
            platformAdminUserId,
            sellerPartyIds,
            tenantId: null,
            cancellationToken).ConfigureAwait(false);

        var scopes = new List<AccessControlMarketplaceSellerScope>(pairs.Length);
        foreach (var pair in pairs)
        {
            if (pair.SellerPartyId == Guid.Empty || pair.ActorUserId == Guid.Empty)
            {
                continue;
            }

            var owner = new AccessOwnerScope(
                AccessOwnerScopeKind.Seller,
                pair.SellerPartyId,
                TenantId: null);

            var roles = await _access.ListRolesAsync(owner, includeArchived: false, cancellationToken)
                .ConfigureAwait(false);
            var ownerRole = roles.FirstOrDefault(role =>
                string.Equals(role.Code, "seller-owner", StringComparison.Ordinal));
            if (ownerRole is null)
            {
                continue;
            }

            // Idempotent: an existing assignment of THIS user in THIS own scope is never duplicated.
            var assignments = await _access.ListAssignmentsAsync(owner, pair.ActorUserId, cancellationToken)
                .ConfigureAwait(false);
            var present = assignments.Any(assignment =>
                assignment.RoleId == ownerRole.Id && assignment.UserId == pair.ActorUserId);

            var created = false;
            if (!present)
            {
                await _access.AssignRoleAsync(
                    owner,
                    pair.ActorUserId,
                    ownerRole.Id,
                    platformAdminUserId ?? pair.ActorUserId,
                    traceId: "marketplace-seller-access-commissioning",
                    cancellationToken).ConfigureAwait(false);
                created = true;
            }
            else
            {
                // Keep the derived capability tuples in step with the owned role, through the same authority.
                await _access.SyncUserCapabilityTuplesAsync(pair.ActorUserId, owner, cancellationToken)
                    .ConfigureAwait(false);
            }

            scopes.Add(new AccessControlMarketplaceSellerScope(
                pair.SellerPartyId,
                pair.ActorUserId,
                ownerRole.Id,
                created));
        }

        // Publish the REAL pairs LAST so a legacy synthetic Marketplace pair does not stay published.
        _sellerDevContexts.Publish(seller.ActorA, seller.ActorB);

        return scopes.Count == 0 ? null : new AccessControlMarketplaceSellerAccess(scopes);
    }
}
