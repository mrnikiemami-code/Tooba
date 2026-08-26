using Tooba.Payment.Application;
using Tooba.Payment.Infrastructure;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// سیاست Production درگاه: fail-closed، webhook misconfig، امضای callback.
/// </summary>
public sealed class PaymentProductionPolicyTests
{
    [Fact]
    public async Task Fail_closed_gateway_rejects_initiate_with_stable_code()
    {
        var gateway = new FailClosedPaymentGateway(new PaymentGatewayInstrumentation());
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            gateway.InitiateAsync(Guid.NewGuid(), 100m, "IRR", CancellationToken.None));
        Assert.Equal("payment.gateway.unconfigured", ex.Message);
    }

    [Fact]
    public async Task Fail_closed_gateway_rejects_verify_with_stable_code()
    {
        var gateway = new FailClosedPaymentGateway(new PaymentGatewayInstrumentation());
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            gateway.VerifyAsync("ref-1", true, CancellationToken.None));
        Assert.Equal("payment.gateway.unconfigured", ex.Message);
    }

    [Fact]
    public async Task Webhook_gateway_misconfigured_initiate_is_fail_closed()
    {
        var gateway = new WebhookPaymentGateway(
            new HttpClient(),
            Microsoft.Extensions.Options.Options.Create(new PaymentGatewayOptions()),
            new PaymentGatewayInstrumentation());
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            gateway.InitiateAsync(Guid.NewGuid(), 100m, "IRR", CancellationToken.None));
        Assert.Equal("payment.gateway.unconfigured", ex.Message);
    }

    [Fact]
    public async Task Webhook_gateway_verify_uses_status_override_not_callback_text()
    {
        var reference = "wh-test-ref";
        WebhookPaymentGateway.TestStatusOverrides[reference] =
            new GatewayVerification(true, "txn-override", null);
        try
        {
            var gateway = new WebhookPaymentGateway(
                new HttpClient(),
                Microsoft.Extensions.Options.Options.Create(new PaymentGatewayOptions
                {
                    StatusQueryBaseUrl = "https://payments.test/status",
                }),
                new PaymentGatewayInstrumentation());
            var verified = await gateway.VerifyAsync(reference, callbackClaimsSuccess: false, CancellationToken.None);
            Assert.True(verified.VerifiedSuccess);
            Assert.Equal("txn-override", verified.ProviderTransactionReference);
        }
        finally
        {
            WebhookPaymentGateway.TestStatusOverrides.TryRemove(reference, out _);
        }
    }

    [Fact]
    public void Signature_validator_rejects_tampered_body()
    {
        var secret = "test-secret";
        var body = "{\"paymentId\":\"00000000-0000-0000-0000-000000000001\"}"u8.ToArray();
        var valid = PaymentWebhookSignatureValidator.ComputeSignature(secret, body);
        body[^1] = (byte)(body[^1] ^ 0x01);
        Assert.False(PaymentWebhookSignatureValidator.TryValidate(secret, body, valid, out _));
    }

    [Fact]
    public void Signature_validator_accepts_valid_hmac()
    {
        var secret = "test-secret";
        var body = "{\"providerEventId\":\"evt-1\"}"u8.ToArray();
        var valid = PaymentWebhookSignatureValidator.ComputeSignature(secret, body);
        Assert.True(PaymentWebhookSignatureValidator.TryValidate(secret, body, valid, out var error));
        Assert.Equal(string.Empty, error);
    }
}
