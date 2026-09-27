namespace Tooba.Catalog.Application.Settings.CheckoutAbuse.Models;

/// <summary>Admin checkout-abuse settings view (parity with Host CheckoutAbuseSettingsView).</summary>
public sealed record CheckoutAbuseSettingsView(
    int MaxOpenUnpaidOrdersPerCustomer,
    int ReservationCommitWindowMinutes,
    int MaxCheckoutCommitsPerCustomerInWindow,
    int MinOpenUnpaid,
    int MaxOpenUnpaid,
    int MinWindowMinutes,
    int MaxWindowMinutes,
    int MinCommits,
    int MaxCommits);
