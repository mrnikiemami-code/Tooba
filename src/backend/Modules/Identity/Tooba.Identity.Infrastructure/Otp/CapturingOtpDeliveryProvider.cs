using Tooba.Identity.Application;
using Tooba.Identity.Contracts;

namespace Tooba.Identity.Infrastructure.Otp;

/// <summary>
/// Dev/Test provider: captures last OTP in memory, never logs code.
/// </summary>
public sealed class CapturingOtpDeliveryProvider : IOtpDeliveryProvider
{
    /// <summary>
    /// Last captured code for integration tests only.
    /// </summary>
    public string? LastCode { get; private set; }

    /// <summary>
    /// Last destination for tests.
    /// </summary>
    public string? LastDestination { get; private set; }

    /// <inheritdoc />
    public Task<OtpDeliveryOutcome> DeliverAsync(OtpDeliveryMessage message, CancellationToken cancellationToken)
    {
        LastDestination = message.Destination;
        LastCode = message.OneTimeCode;
        return Task.FromResult(new OtpDeliveryOutcome(OtpDeliveryOutcomeKind.Succeeded, "capture"));
    }
}
