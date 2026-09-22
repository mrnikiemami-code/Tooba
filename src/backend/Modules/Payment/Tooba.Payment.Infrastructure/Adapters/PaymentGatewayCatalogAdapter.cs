#pragma warning disable CS1591
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tooba.Payment.Application.Ports;
using Tooba.Payment.Infrastructure.Providers;

namespace Tooba.Payment.Infrastructure.Adapters;

/// <summary>Infrastructure adapter for storefront gateway catalog / sandbox policy.</summary>
public sealed class PaymentGatewayCatalogAdapter : IPaymentGatewayCatalogPort
{
    private readonly PaymentGatewayOptions _options;
    private readonly IHostEnvironment _environment;

    public PaymentGatewayCatalogAdapter(
        IOptions<PaymentGatewayOptions> options,
        IHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public string DefaultProvider => _options.DefaultProvider;
    public bool ManualCardToCardEnabled => _options.ManualCardToCardEnabled;
    public string ManualProofRequirement => _options.NormalizedManualProofRequirement();
    public string ManualPaymentInstructions => _options.ManualPaymentInstructions ?? string.Empty;
    public string StoreDisplayName =>
        string.IsNullOrWhiteSpace(_options.StoreDisplayName) ? "Tooba" : _options.StoreDisplayName;
    public int ManualPaymentReviewHoldHours => Math.Clamp(_options.ManualPaymentReviewHoldHours, 1, 24 * 30);

    public bool IsOnlineGatewayOffered()
    {
        var mode = (_options.Mode ?? string.Empty).Trim();
        if (mode.Equals("Disabled", StringComparison.OrdinalIgnoreCase))
            return false;
        if (mode.Equals("Sandbox", StringComparison.OrdinalIgnoreCase))
            return true;
        if (mode.Equals("Webhook", StringComparison.OrdinalIgnoreCase))
        {
            return !string.IsNullOrWhiteSpace(_options.InitiateBaseUrl)
                && !string.IsNullOrWhiteSpace(_options.WebhookSigningSecret);
        }

        return false;
    }

    public bool IsSandboxSimulatorEnabled() =>
        !_environment.IsProduction()
        && (_options.Mode ?? string.Empty).Trim().Equals("Sandbox", StringComparison.OrdinalIgnoreCase);

    public void MarkSandboxDecline(string providerRequestReference) =>
        FakePaymentGateway.SandboxDeclinedReferences[providerRequestReference] = 1;
}

/// <summary>Infrastructure adapter for webhook HMAC validation.</summary>
public sealed class PaymentWebhookSignatureVerifierAdapter : IPaymentWebhookSignatureVerifier
{
    private readonly PaymentGatewayOptions _options;

    public PaymentWebhookSignatureVerifierAdapter(IOptions<PaymentGatewayOptions> options) =>
        _options = options.Value;

    public string SignatureHeaderName => PaymentWebhookSignatureValidator.SignatureHeaderName;

    public bool TryValidate(ReadOnlySpan<byte> body, string? signatureHeader, out string errorCode) =>
        PaymentWebhookSignatureValidator.TryValidate(
            _options.WebhookSigningSecret,
            body,
            signatureHeader,
            out errorCode);
}
