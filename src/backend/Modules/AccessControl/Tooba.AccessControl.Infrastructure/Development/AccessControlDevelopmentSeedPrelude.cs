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
}
