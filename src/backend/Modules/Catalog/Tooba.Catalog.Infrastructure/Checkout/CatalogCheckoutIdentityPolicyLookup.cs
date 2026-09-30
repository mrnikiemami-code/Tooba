using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Contracts.Checkout;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Checkout;

/// <summary>Contracts reader for effective checkout-identity policy.</summary>
public sealed class CatalogCheckoutIdentityPolicyLookup(CatalogDbContext catalog)
    : ICatalogCheckoutIdentityPolicyLookup
{
    /// <inheritdoc />
    public async Task<CatalogCheckoutIdentityPolicySnapshot> GetEffectiveAsync(
        CancellationToken cancellationToken)
    {
        var row = await catalog.StoreCheckoutIdentitySettings.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.SettingsId == StoreCheckoutIdentitySettings.SingletonId,
                cancellationToken);
        var policy = row?.Policy ?? CheckoutIdentityPolicyKind.AuthenticatedOnly;
        return new CatalogCheckoutIdentityPolicySnapshot(policy.ToString());
    }
}
