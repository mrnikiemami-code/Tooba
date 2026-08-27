using Tooba.Settlement.Application;

namespace Tooba.Settlement.Infrastructure;

/// <summary>
/// Production fail-closed وقتی درگاه payout واقعی پیکربندی نشده است.
/// </summary>
public sealed class FailClosedPayoutGateway : IPayoutGateway
{
    /// <summary>کد پایدار درگاه fail-closed.</summary>
    public const string ProviderCodeValue = "fail-closed-payout";

    /// <inheritdoc />
    public string ProviderCode => ProviderCodeValue;

    /// <inheritdoc />
    public Task<GatewayPayoutResult> PayoutAsync(
        Guid payoutRequestId,
        Guid sellerPartyId,
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        _ = payoutRequestId;
        _ = sellerPartyId;
        _ = amount;
        _ = currency;
        _ = idempotencyKey;
        _ = cancellationToken;
        throw new InvalidOperationException("payout.gateway.unconfigured");
    }
}
