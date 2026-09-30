namespace Tooba.Catalog.Contracts.Checkout;

/// <summary>Effective store checkout-identity policy (Catalog-owned; no DbContext leakage).</summary>
public sealed record CatalogCheckoutIdentityPolicySnapshot(string Policy);

/// <summary>Catalog boundary for storefront/checkout identity policy reads.</summary>
public interface ICatalogCheckoutIdentityPolicyLookup
{
    /// <summary>
    /// Loads effective policy name: <c>GuestAllowed</c> or <c>AuthenticatedOnly</c>
    /// (missing row → AuthenticatedOnly).
    /// </summary>
    Task<CatalogCheckoutIdentityPolicySnapshot> GetEffectiveAsync(CancellationToken cancellationToken);
}
