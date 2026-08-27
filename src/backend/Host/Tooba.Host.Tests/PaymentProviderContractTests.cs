using Tooba.Payment.Application;
using Tooba.Payment.Infrastructure;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// قرارداد قابل‌استفادهٔ مجدد برای هر production adapter (فعلاً WebhookPaymentGateway).
/// </summary>
public sealed class PaymentProviderContractTests
{
    private static WebhookPaymentGateway CreateConfiguredGateway() =>
        new(
            new HttpClient(),
            Microsoft.Extensions.Options.Options.Create(new PaymentGatewayOptions
            {
                WebhookSigningSecret = "contract-secret",
                StatusQueryBaseUrl = "https://psp.example/status",
                InitiateBaseUrl = "https://psp.example/pay",
                AllowedStatusQueryHosts = ["psp.example"],
                VerifyMaxAttempts = 2,
            }),
            new PaymentGatewayInstrumentation());

    [Fact]
    public async Task Initiation_mapping_returns_reference_and_external_redirect()
    {
        var gateway = CreateConfiguredGateway();
        var paymentId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var init = await gateway.InitiateAsync(paymentId, 250000m, "IRR", CancellationToken.None);
        Assert.StartsWith("wh-", init.ProviderRequestReference, StringComparison.Ordinal);
        Assert.Contains("https://psp.example/pay", init.RedirectUrl, StringComparison.Ordinal);
        Assert.Contains(paymentId.ToString("D"), init.RedirectUrl, StringComparison.Ordinal);
        Assert.StartsWith("https://psp.example/pay", init.RedirectUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Success_verification_ignores_callback_claim()
    {
        var gateway = CreateConfiguredGateway();
        var reference = "wh-success";
        WebhookPaymentGateway.TestStatusOverrides[reference] =
            new GatewayVerification(true, "txn-ok", null);
        try
        {
            var verified = await gateway.VerifyAsync(reference, callbackClaimsSuccess: false, CancellationToken.None);
            Assert.True(verified.VerifiedSuccess);
            Assert.Equal("txn-ok", verified.ProviderTransactionReference);
        }
        finally
        {
            WebhookPaymentGateway.TestStatusOverrides.TryRemove(reference, out _);
        }
    }

    [Fact]
    public async Task Failure_verification_is_definitive()
    {
        var gateway = CreateConfiguredGateway();
        var reference = "wh-fail";
        WebhookPaymentGateway.TestStatusOverrides[reference] =
            new GatewayVerification(false, null, "GATEWAY_REJECTED");
        try
        {
            var verified = await gateway.VerifyAsync(reference, callbackClaimsSuccess: true, CancellationToken.None);
            Assert.False(verified.VerifiedSuccess);
            Assert.Equal("GATEWAY_REJECTED", verified.FailureCode);
            Assert.False(PaymentGatewayOutcomes.IsIndeterminate(verified.FailureCode));
        }
        finally
        {
            WebhookPaymentGateway.TestStatusOverrides.TryRemove(reference, out _);
        }
    }

    [Fact]
    public async Task Unknown_pending_is_indeterminate()
    {
        var gateway = CreateConfiguredGateway();
        var reference = "wh-pending";
        WebhookPaymentGateway.TestStatusOverrides[reference] =
            new GatewayVerification(false, null, "GATEWAY_PENDING");
        try
        {
            var verified = await gateway.VerifyAsync(reference, true, CancellationToken.None);
            Assert.False(verified.VerifiedSuccess);
            Assert.True(PaymentGatewayOutcomes.IsIndeterminate(verified.FailureCode));
        }
        finally
        {
            WebhookPaymentGateway.TestStatusOverrides.TryRemove(reference, out _);
        }
    }

    [Fact]
    public void Invalid_authenticity_rejected_by_signature_validator()
    {
        var body = "{\"providerEventId\":\"evt\"}"u8.ToArray();
        Assert.False(PaymentWebhookSignatureValidator.TryValidate("secret", body, "sha256=deadbeef", out _));
    }

    [Fact]
    public async Task Unconfigured_production_adapter_fail_closed_on_initiate()
    {
        var gateway = new WebhookPaymentGateway(
            new HttpClient(),
            Microsoft.Extensions.Options.Options.Create(new PaymentGatewayOptions
            {
                Mode = "Webhook",
            }),
            new PaymentGatewayInstrumentation());
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            gateway.InitiateAsync(Guid.NewGuid(), 1m, "IRR", CancellationToken.None));
        Assert.Equal("payment.gateway.unconfigured", ex.Message);
    }
}
