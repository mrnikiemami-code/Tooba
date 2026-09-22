namespace Tooba.Catalog.Contracts.Checkout;

/// <summary>Read-only store checkout abuse settings (Catalog-owned).</summary>
public sealed record StoreCheckoutAbuseSettingsSnapshot(
    Guid SettingsId,
    int MaxOpenUnpaidOrdersPerCustomer,
    int ReservationCommitWindowMinutes,
    int MaxCheckoutCommitsPerCustomerInWindow);

/// <summary>Catalog boundary for checkout abuse thresholds — no Catalog DbContext leakage.</summary>
public interface IStoreCheckoutAbuseSettingsReader
{
    /// <summary>Loads the singleton store checkout abuse settings snapshot.</summary>
    Task<StoreCheckoutAbuseSettingsSnapshot> GetAsync(CancellationToken cancellationToken);
}
