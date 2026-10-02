using Tooba.Identity.Application.Models;
using Tooba.Identity.Application.Options;
using Tooba.Identity.Application.Ports;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Auth;

namespace Tooba.Identity.Infrastructure.Otp;

/// <summary>
/// Production fail-closed when provider mode is Disabled or misconfigured.
/// </summary>
public sealed class FailClosedOtpDeliveryProvider : IOtpDeliveryProvider
{
    private readonly OtpDeliveryInstrumentation _telemetry;

    /// <summary>Fail-closed production placeholder.</summary>
    public FailClosedOtpDeliveryProvider(OtpDeliveryInstrumentation telemetry) => _telemetry = telemetry;

    /// <inheritdoc />
    public Task<OtpDeliveryOutcome> DeliverAsync(OtpDeliveryMessage message, CancellationToken cancellationToken)
    {
        _telemetry.RecordDelivery("misconfigured");
        return Task.FromResult(new OtpDeliveryOutcome(OtpDeliveryOutcomeKind.Misconfigured));
    }
}
