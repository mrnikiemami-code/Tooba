using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Tooba.BuildingBlocks.Observability.Logging;

/// <summary>
/// Scope لاگ مرکزی برای Correlation/Trace/Request — بدون تکرار دستی روی هر Log call.
/// </summary>
public static class ObservabilityLogScope
{
    /// <summary>Scope ساختاریافته را روی logger باز می‌کند.</summary>
    public static IDisposable Begin(ILogger logger, ObservabilityLogScopeState state)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(state);
        return logger.BeginScope(state.ToDictionary()) ?? EmptyScope.Instance;
    }

    /// <summary>State را از شناسه‌های شناخته‌شده و Activity می‌سازد.</summary>
    public static ObservabilityLogScopeState CreateState(
        string correlationId,
        string? requestId = null,
        string? tenantId = null,
        string? storeId = null,
        string? actorId = null,
        string? clientIp = null,
        string? httpMethod = null,
        string? httpPath = null,
        Activity? activity = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        activity ??= Activity.Current;
        return new ObservabilityLogScopeState(
            CorrelationId: correlationId,
            TraceId: activity?.TraceId.ToString(),
            SpanId: activity?.SpanId.ToString(),
            RequestId: requestId,
            TenantId: tenantId,
            StoreId: storeId,
            ActorId: actorId,
            ClientIp: clientIp,
            HttpMethod: httpMethod,
            HttpPath: httpPath);
    }

    private sealed class EmptyScope : IDisposable
    {
        internal static readonly EmptyScope Instance = new();
        public void Dispose()
        {
        }
    }
}

/// <summary>State تغییرناپذیر برای BeginScope.</summary>
/// <param name="CorrelationId">همبستگی.</param>
/// <param name="TraceId">Trace.</param>
/// <param name="SpanId">Span.</param>
/// <param name="RequestId">Request.</param>
/// <param name="TenantId">Tenant.</param>
/// <param name="StoreId">Store.</param>
/// <param name="ActorId">Actor.</param>
/// <param name="ClientIp">IP اختیاری.</param>
/// <param name="HttpMethod">متد HTTP.</param>
/// <param name="HttpPath">مسیر HTTP.</param>
public sealed record ObservabilityLogScopeState(
    string CorrelationId,
    string? TraceId,
    string? SpanId,
    string? RequestId,
    string? TenantId,
    string? StoreId,
    string? ActorId,
    string? ClientIp,
    string? HttpMethod = null,
    string? HttpPath = null)
{
    /// <summary>Dictionary برای ILogger.BeginScope.</summary>
    public Dictionary<string, object> ToDictionary()
    {
        var state = new Dictionary<string, object>(12)
        {
            [ObservabilityLogScopeKeys.CorrelationId] = CorrelationId,
        };
        Add(state, ObservabilityLogScopeKeys.TraceId, TraceId);
        Add(state, ObservabilityLogScopeKeys.SpanId, SpanId);
        Add(state, ObservabilityLogScopeKeys.RequestId, RequestId);
        Add(state, ObservabilityLogScopeKeys.TenantId, TenantId);
        Add(state, ObservabilityLogScopeKeys.StoreId, StoreId);
        Add(state, ObservabilityLogScopeKeys.ActorId, ActorId);
        Add(state, ObservabilityLogScopeKeys.ClientIp, ClientIp);
        Add(state, ObservabilityLogScopeKeys.HttpMethod, HttpMethod);
        Add(state, ObservabilityLogScopeKeys.HttpPath, HttpPath);
        return state;
    }

    private static void Add(Dictionary<string, object> state, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            state[key] = value;
        }
    }
}
