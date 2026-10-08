using Tooba.Promotion.Application.Promotions.Ports;

namespace Tooba.Promotion.Infrastructure.Directories;

/// <summary>
/// دفتر مصرف آینده. سقف هم‌زمان در این foundation قفل و شمرده نمی‌شود.
/// </summary>
public sealed class DeferredPromotionRedemptionLedger : IPromotionRedemptionLedger
{
    /// <inheritdoc />
    public Task<bool> CanRedeemAsync(Guid promotionId, Guid? customerPartyId, CancellationToken cancellationToken) =>
        Task.FromResult(true);
}
