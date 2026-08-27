using Tooba.Settlement.Application;

namespace Tooba.Settlement.Infrastructure;

/// <summary>
/// درگاه payout آزمایشی. dev همیشه موفق می‌شود.
/// </summary>
public sealed class FakePayoutGateway : IPayoutGateway
{
    /// <inheritdoc />
    public string ProviderCode => "fake-payout";

    /// <inheritdoc />
    public Task<GatewayPayoutResult> PayoutAsync(
        Guid payoutRequestId,
        Guid sellerPartyId,
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        _ = sellerPartyId;
        _ = amount;
        _ = currency;
        _ = cancellationToken;
        return Task.FromResult(new GatewayPayoutResult(true, $"fake-payout-{payoutRequestId:N}-{idempotencyKey}", null));
    }
}
