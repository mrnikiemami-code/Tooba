using System.Diagnostics.Metrics;
using Tooba.BuildingBlocks;

namespace Tooba.Host;

/// <summary>
/// متریک‌های احراز بدون password/token/OTP.
/// </summary>
internal sealed class AuthenticationInstrumentation
{
    private readonly Counter<long> _events;

    public AuthenticationInstrumentation()
    {
        _events = ToobaTelemetry.Meter.CreateCounter<long>("tooba.authentication.event");
    }

    public void Record(string outcome, string operation) =>
        _events.Add(1, new KeyValuePair<string, object?>("outcome", outcome), new KeyValuePair<string, object?>("operation", operation));

    public void RecordThrottled(string operation) => Record("throttled", operation);
}
