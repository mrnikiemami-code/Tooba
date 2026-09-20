using System.Diagnostics;
using System.Diagnostics.Metrics;
using Tooba.BuildingBlocks;

namespace Tooba.Host;

/// <summary>
/// تله‌متری مجوز بدون برچسب UserId/TenantId/ResourceId.
/// </summary>
internal sealed class AuthorizationInstrumentation
{
    private readonly Counter<long> _checks;
    private readonly Counter<long> _infrastructure;
    private readonly Histogram<double> _latencyMs;

    /// <summary>
    /// شمارنده‌ها را روی Meter Tooba می‌سازد.
    /// </summary>
    public AuthorizationInstrumentation()
    {
        var meter = ToobaTelemetry.Meter;
        _checks = meter.CreateCounter<long>("tooba.authorization.check");
        _infrastructure = meter.CreateCounter<long>("tooba.authorization.infrastructure");
        _latencyMs = meter.CreateHistogram<double>("tooba.authorization.check.duration", "ms");
    }

    /// <summary>
    /// نتیجه را با برچسب کران‌دار ثبت می‌کند. توکن لاگ نمی‌شود.
    /// </summary>
    public void Record(AuthorizationDecisionKind kind, string resourceType, string permission, ToobaEdition edition, long elapsedMs)
    {
        var outcome = kind switch
        {
            AuthorizationDecisionKind.Allow => "allow",
            AuthorizationDecisionKind.Deny => "deny",
            AuthorizationDecisionKind.Unavailable => "unavailable",
            _ => "error",
        };
        var tags = new TagList
        {
            { "outcome", outcome },
            { "resource_type", resourceType },
            { "permission", permission },
            { "edition", edition.ToString() },
        };
        _checks.Add(1, tags);
        _latencyMs.Record(elapsedMs, tags);
        if (kind == AuthorizationDecisionKind.Unavailable)
        {
            _infrastructure.Add(1, new TagList { { "kind", "unavailable" }, { "resource_type", resourceType } });
        }
    }

    /// <summary>
    /// retry/timeout زیرساخت را جدا از DENY ثبت می‌کند.
    /// </summary>
    public void RecordInfrastructure(string kind, string resourceType)
    {
        _infrastructure.Add(1, new TagList
        {
            { "kind", kind },
            { "resource_type", resourceType },
        });
    }
}
