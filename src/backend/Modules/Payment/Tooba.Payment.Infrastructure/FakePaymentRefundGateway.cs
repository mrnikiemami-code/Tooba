using Tooba.Payment.Application;

namespace Tooba.Payment.Infrastructure;

/// <summary>
/// درگاه refund آزمایشی. موفقیت sandbox؛ پسوند -FAIL-REFUND شکست می‌دهد.
/// </summary>
public sealed class FakePaymentRefundGateway : IPaymentRefundGateway
{
    /// <summary>
    /// اگر کلید idempotency این پسوند را داشته باشد، refund شکست می‌خورد.
    /// </summary>
    public const string FailRefundSuffix = "-FAIL-REFUND";

    /// <inheritdoc />
    public Task<GatewayRefundResult> RefundAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        _ = amount;
        _ = currency;
        if (idempotencyKey.Contains(FailRefundSuffix, StringComparison.Ordinal))
        {
            return Task.FromResult(new GatewayRefundResult(false, null, "GATEWAY_REFUND_REJECTED"));
        }

        return Task.FromResult(new GatewayRefundResult(true, $"refund-txn-{paymentId:N}", null));
    }
}

/// <summary>
/// Production fail-closed وقتی refund gateway پیکربندی نشده است.
/// </summary>
public sealed class FailClosedPaymentRefundGateway : IPaymentRefundGateway
{
    /// <inheritdoc />
    public Task<GatewayRefundResult> RefundAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        _ = paymentId;
        _ = amount;
        _ = currency;
        _ = idempotencyKey;
        throw new InvalidOperationException("payment.refund.gateway.unconfigured");
    }
}
