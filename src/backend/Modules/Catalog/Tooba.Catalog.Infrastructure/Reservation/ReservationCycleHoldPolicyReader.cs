using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Contracts.Reservation;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Reservation;

/// <summary>Catalog-owned reader for store/offer/category reservation hold overrides.</summary>
public sealed class ReservationCycleHoldPolicyReader(CatalogDbContext catalog) : IReservationCycleHoldPolicyReader
{
    /// <inheritdoc />
    public async Task<StoreReservationHoldOverrideSnapshot?> GetStoreOverrideAsync(
        CancellationToken cancellationToken)
    {
        var store = await catalog.StoreHoldPolicySettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SettingsId == StoreHoldPolicySettings.SingletonId, cancellationToken);
        if (store is null)
        {
            return null;
        }

        return new StoreReservationHoldOverrideSnapshot(
            store.InitialReservationHoldMinutes,
            store.RetryReservationHoldMinutes,
            store.MaxReservationCycles);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReservationCycleHoldOverrideSnapshot>> GetOverridesAsync(
        IReadOnlyList<Guid> offerIds,
        IReadOnlyList<Guid> categoryIds,
        CancellationToken cancellationToken)
    {
        if (offerIds.Count == 0 && categoryIds.Count == 0)
        {
            return [];
        }

        var rows = await catalog.ReservationCyclePolicyOverrides.AsNoTracking()
            .Where(x =>
                (x.ScopeKind == ReservationCyclePolicyOverride.OfferScope && offerIds.Contains(x.ScopeId))
                || (x.ScopeKind == ReservationCyclePolicyOverride.CategoryScope && categoryIds.Contains(x.ScopeId)))
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new ReservationCycleHoldOverrideSnapshot(
                x.ScopeKind,
                x.ScopeId,
                x.InitialReservationHoldMinutes,
                x.RetryReservationHoldMinutes,
                x.MaxReservationCycles))
            .ToArray();
    }
}
