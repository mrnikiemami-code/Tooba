using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Tooba.Host;

/// <summary>
/// محدودسازی نرخ auth-sensitive با کلید IP+operation. 429 enumeration-safe.
/// </summary>
internal sealed class AuthenticationRateLimitThrottleSeam : IAuthenticationThrottleSeam
{
    private readonly AuthSecurityHostOptions _options;
    private readonly AuthenticationInstrumentation _telemetry;
    private readonly ConcurrentDictionary<string, WindowCounter> _windows = new(StringComparer.Ordinal);

    public AuthenticationRateLimitThrottleSeam(
        IOptions<AuthSecurityHostOptions> options,
        AuthenticationInstrumentation telemetry)
    {
        _options = options.Value;
        _telemetry = telemetry;
    }

    /// <inheritdoc />
    public bool TryAcquire(HttpContext context, string operation)
    {
        var key = BuildKey(context, operation);
        var now = DateTimeOffset.UtcNow;
        var window = TimeSpan.FromSeconds(Math.Max(1, _options.AuthRateLimitWindowSeconds));
        var limit = Math.Max(1, _options.AuthRateLimitPermitLimit);
        var counter = _windows.AddOrUpdate(
            key,
            _ => new WindowCounter(now, 1),
            (_, existing) => existing.WindowStart + window <= now
                ? new WindowCounter(now, 1)
                : new WindowCounter(existing.WindowStart, existing.Count + 1));

        if (counter.Count > limit)
        {
            _telemetry.RecordThrottled(operation);
            return false;
        }

        return true;
    }

    private static string BuildKey(HttpContext context, string operation)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"{ip}:{operation}";
    }

    private sealed record WindowCounter(DateTimeOffset WindowStart, int Count);
}
