using System.Diagnostics.Metrics;
using Tooba.BuildingBlocks;

namespace Tooba.Identity.Infrastructure;

/// <summary>
/// متریک تحویل OTP بدون destination/code.
/// </summary>
public sealed class OtpDeliveryInstrumentation
{
    private readonly Counter<long> _deliveries;

    /// <summary>Creates OTP delivery metrics.</summary>
    public OtpDeliveryInstrumentation()
    {
        _deliveries = ToobaTelemetry.Meter.CreateCounter<long>("tooba.identity.otp.delivery");
    }

    /// <summary>Records delivery outcome without destination/code.</summary>
    public void RecordDelivery(string outcome) =>
        _deliveries.Add(1, new KeyValuePair<string, object?>("outcome", outcome));
}
