#pragma warning disable CS1591
namespace Tooba.Payment.Contracts.Hold;

public sealed record PaymentMethodHoldOverride(
    string ProviderCode,
    int? OnlinePaymentHoldHours,
    int? ManualPaymentInitialHoldHours,
    int? ManualPaymentReviewHoldHours);

public interface IPaymentHoldSettingsGateway
{
    Task<IReadOnlyList<PaymentMethodHoldOverride>> ListMethodOverridesAsync(CancellationToken cancellationToken);
    Task UpsertMethodOverrideAsync(
        string providerCode,
        int? onlinePaymentHoldHours,
        int? manualPaymentInitialHoldHours,
        int? manualPaymentReviewHoldHours,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

public interface ICommerceHoldPolicySource
{
    int ResolveOnlineHoldHours(string? providerCode);
    int ResolveManualInitialHoldHours(string? providerCode);
    int ResolveManualReviewHoldHours(string? providerCode);
    DateTimeOffset ResolveInitialExpiresAt(DateTimeOffset utcNow);
    DateTimeOffset ResolveUnpaidTimeoutAt(string providerCode, DateTimeOffset utcNow);
    DateTimeOffset ResolveManualReviewExpiresAt(DateTimeOffset utcNow);
}
