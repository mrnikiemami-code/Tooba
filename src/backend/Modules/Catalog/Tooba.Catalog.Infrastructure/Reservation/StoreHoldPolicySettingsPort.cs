using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Contracts.Reservation;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Reservation;

/// <summary>Catalog persistence for StoreHoldPolicySettings cart/payment hour fields.</summary>
public sealed class StoreHoldPolicySettingsPort : IStoreHoldPolicySettingsPort
{
    private readonly CatalogDbContext _catalog;
    private readonly IClock _clock;

    /// <summary>Creates the port.</summary>
    public StoreHoldPolicySettingsPort(CatalogDbContext catalog, IClock clock)
    {
        _catalog = catalog;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<StoreHoldPolicyHoursSnapshot> GetHoursAsync(CancellationToken cancellationToken)
    {
        var store = await _catalog.StoreHoldPolicySettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SettingsId == StoreHoldPolicySettings.SingletonId, cancellationToken);
        return new StoreHoldPolicyHoursSnapshot(
            store?.CartPersistenceHours,
            store?.OnlinePaymentHoldHours,
            store?.ManualPaymentInitialHoldHours,
            store?.ManualPaymentReviewHoldHours);
    }

    /// <inheritdoc />
    public async Task SaveHoursAsync(StoreHoldPolicyHoursWrite write, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var store = await _catalog.StoreHoldPolicySettings
            .SingleOrDefaultAsync(x => x.SettingsId == StoreHoldPolicySettings.SingletonId, cancellationToken);
        if (store is null)
        {
            store = StoreHoldPolicySettings.CreateDefault(now);
            _catalog.StoreHoldPolicySettings.Add(store);
        }

        store.Replace(
            write.CartPersistenceHours,
            write.OnlinePaymentHoldHours,
            write.ManualPaymentInitialHoldHours,
            write.ManualPaymentReviewHoldHours,
            now);
        await _catalog.SaveChangesAsync(cancellationToken);
    }
}
