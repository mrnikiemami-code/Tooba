using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Contracts.Checkout;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Checkout;

/// <summary>Catalog-owned reader for store checkout abuse settings.</summary>
public sealed class StoreCheckoutAbuseSettingsReader(CatalogDbContext catalog) : IStoreCheckoutAbuseSettingsReader
{
    /// <inheritdoc />
    public async Task<StoreCheckoutAbuseSettingsSnapshot> GetAsync(CancellationToken cancellationToken)
    {
        var row = await catalog.StoreCheckoutAbuseSettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.SettingsId == StoreCheckoutAbuseSettings.SingletonId, cancellationToken);
        return new StoreCheckoutAbuseSettingsSnapshot(
            StoreCheckoutAbuseSettings.SingletonId,
            row?.MaxOpenUnpaidOrdersPerCustomer ?? StoreCheckoutAbuseSettings.DefaultMaxOpenUnpaid,
            row?.ReservationCommitWindowMinutes ?? StoreCheckoutAbuseSettings.DefaultWindowMinutes,
            row?.MaxCheckoutCommitsPerCustomerInWindow ?? StoreCheckoutAbuseSettings.DefaultMaxCommits);
    }
}
