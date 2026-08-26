using Tooba.Identity.Application;
using Tooba.Identity.Domain;
using Tooba.Identity.Infrastructure;
using Xunit;

namespace Tooba.Host.Tests;

public sealed class OtpDeliveryProviderTests
{
    [Fact]
    public async Task Capturing_provider_succeeds_without_logging_code()
    {
        var provider = new CapturingOtpDeliveryProvider();
        var outcome = await provider.DeliverAsync(
            new OtpDeliveryMessage(OtpPurpose.PasswordReset, "user@example.com", "654321"),
            CancellationToken.None);
        Assert.Equal(OtpDeliveryOutcomeKind.Succeeded, outcome.Kind);
        Assert.Equal("654321", provider.LastCode);
    }

    [Fact]
    public async Task Fail_closed_provider_returns_misconfigured()
    {
        var provider = new FailClosedOtpDeliveryProvider(new OtpDeliveryInstrumentation());
        var outcome = await provider.DeliverAsync(
            new OtpDeliveryMessage(OtpPurpose.IdentifierVerification, "user@example.com", "111111"),
            CancellationToken.None);
        Assert.Equal(OtpDeliveryOutcomeKind.Misconfigured, outcome.Kind);
    }

    [Fact]
    public async Task Sender_maps_misconfigured_to_identity_error_code()
    {
        var sender = new OtpDeliveryProviderSender(new FailClosedOtpDeliveryProvider(new OtpDeliveryInstrumentation()));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendAsync(OtpPurpose.Login, "user@example.com", "123456", CancellationToken.None));
        Assert.Equal("identity.otp.delivery.unconfigured", ex.Message);
    }
}
