using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Contracts.Reservation;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Reservation;

/// <summary>Catalog-owned reader for the store-scoped Cart persistence override.</summary>
public sealed class StoreCartPersistenceHoursReader(CatalogDbContext catalog) : IStoreCartPersistenceHoursReader
{
    /// <inheritdoc />
    public async Task<int?> GetStoreCartPersistenceHoursAsync(CancellationToken cancellationToken)
    {
        var store = await catalog.StoreHoldPolicySettings.AsNoTracking()
            .Where(x => x.SettingsId == StoreHoldPolicySettings.SingletonId)
            .Select(x => x.CartPersistenceHours)
            .SingleOrDefaultAsync(cancellationToken);
        return store;
    }
}
