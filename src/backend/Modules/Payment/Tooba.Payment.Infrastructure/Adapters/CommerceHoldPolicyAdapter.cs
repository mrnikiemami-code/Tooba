using Tooba.Payment.Application.Ports;
using Tooba.Payment.Contracts.Hold;

namespace Tooba.Payment.Infrastructure.Adapters;

internal sealed class CommerceHoldPolicyAdapter(ICommerceHoldPolicySource source) : ICommerceHoldPolicy
{
    public int ResolveOnlineHoldHours(string? providerCode) => source.ResolveOnlineHoldHours(providerCode);
    public int ResolveManualInitialHoldHours(string? providerCode) => source.ResolveManualInitialHoldHours(providerCode);
    public int ResolveManualReviewHoldHours(string? providerCode) => source.ResolveManualReviewHoldHours(providerCode);
    public DateTimeOffset ResolveInitialExpiresAt(DateTimeOffset utcNow) => source.ResolveInitialExpiresAt(utcNow);
    public DateTimeOffset ResolveUnpaidTimeoutAt(string providerCode, DateTimeOffset utcNow) =>
        source.ResolveUnpaidTimeoutAt(providerCode, utcNow);
    public DateTimeOffset ResolveManualReviewExpiresAt(DateTimeOffset utcNow) =>
        source.ResolveManualReviewExpiresAt(utcNow);
}
