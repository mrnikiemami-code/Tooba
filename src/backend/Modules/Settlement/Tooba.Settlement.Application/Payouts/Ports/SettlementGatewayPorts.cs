namespace Tooba.Settlement.Application.Payouts.Ports;

/// <summary>
/// نتیجهٔ payout از درگاه.
/// </summary>
public sealed record GatewayPayoutResult(bool Succeeded, string? ProviderReference, string? FailureCode);

/// <summary>
/// قرارداد درگاه payout. PSP واقعی اینجا نیست.
/// </summary>
public interface IPayoutGateway
{
    /// <summary>کد پایدار درگاه.</summary>
    string ProviderCode { get; }

    /// <summary>payout را با idempotency نزد درگاه اجرا می‌کند.</summary>
    Task<GatewayPayoutResult> PayoutAsync(
        Guid payoutRequestId,
        Guid sellerPartyId,
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken cancellationToken);
}
