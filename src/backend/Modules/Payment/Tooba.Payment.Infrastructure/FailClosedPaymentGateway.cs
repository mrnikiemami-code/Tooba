using Tooba.Payment.Application;

namespace Tooba.Payment.Infrastructure;

/// <summary>
/// Production fail-closed وقتی Mode=Disabled یا Webhook پیکربندی نشده است.
/// </summary>
public sealed class FailClosedPaymentGateway : IPaymentGateway
{
    /// <summary>
    /// کد پایدار درگاه fail-closed.
    /// </summary>
    public const string ProviderCodeValue = "fail-closed";

    private readonly PaymentGatewayInstrumentation _telemetry;

    /// <summary>
    /// درگاه Production که بدون PSP واقعی شروع/Verify نمی‌کند.
    /// </summary>
    public FailClosedPaymentGateway(PaymentGatewayInstrumentation telemetry) => _telemetry = telemetry;

    /// <inheritdoc />
    public string ProviderCode => ProviderCodeValue;

    /// <inheritdoc />
    public Task<GatewayInitiation> InitiateAsync(
        Guid paymentId,
        decimal amount,
        string currency,
        CancellationToken cancellationToken)
    {
        _ = paymentId;
        _ = amount;
        _ = currency;
        _telemetry.RecordInitiate("misconfigured");
        throw new InvalidOperationException("payment.gateway.unconfigured");
    }

    /// <inheritdoc />
    public Task<GatewayVerification> VerifyAsync(
        string providerRequestReference,
        bool callbackClaimsSuccess,
        CancellationToken cancellationToken)
    {
        _ = providerRequestReference;
        _ = callbackClaimsSuccess;
        _telemetry.RecordVerify("misconfigured");
        throw new InvalidOperationException("payment.gateway.unconfigured");
    }
}
