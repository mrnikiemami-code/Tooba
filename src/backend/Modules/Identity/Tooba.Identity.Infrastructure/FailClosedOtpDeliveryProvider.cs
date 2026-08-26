using Tooba.Identity.Application;

namespace Tooba.Identity.Infrastructure;

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
