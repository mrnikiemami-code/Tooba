using System.Diagnostics;
using Tooba.BuildingBlocks.Observability.Correlation;
using Tooba.BuildingBlocks.Observability.Tracing;

namespace Tooba.BuildingBlocks.Observability.Messaging;

/// <summary>بازیابی و انتشار Correlation برای مسیرهای publish/consume/outbox.</summary>
public static class MessagingCorrelation
{
    /// <summary>
    /// Correlation معتبر ورودی را می‌پذیرد؛ در غیر این صورت از Ambient یا EventId می‌سازد — نه Guid تصادفی جدا.
    /// </summary>
    public static string ResolveForPublish(string? metadataCorrelationId, Guid eventId)
    {
        if (!string.IsNullOrWhiteSpace(metadataCorrelationId)
            && CorrelationIdContext.TryNormalize(metadataCorrelationId, out var fromMeta))
        {
            return fromMeta;
        }

        if (!string.IsNullOrWhiteSpace(CorrelationIdContext.Current))
        {
            return CorrelationIdContext.Current!;
        }

        return eventId.ToString("N");
    }

    /// <summary>Scope مصرف؛ مقدار قبلی AsyncLocal با Dispose بازمی‌گردد.</summary>
    public static IDisposable BeginConsumeScope(string? incomingCorrelationId, Guid? transportCorrelationId, Guid eventId)
    {
        string chosen;
        if (!string.IsNullOrWhiteSpace(incomingCorrelationId)
            && CorrelationIdContext.TryNormalize(incomingCorrelationId, out var fromEnvelope))
        {
            chosen = fromEnvelope;
        }
        else if (transportCorrelationId is Guid g && g != Guid.Empty)
        {
            chosen = g.ToString("N");
        }
        else
        {
            chosen = eventId.ToString("N");
        }

        return CorrelationIdContext.BeginScope(chosen);
    }

    /// <summary>Activity موجود MassTransit را enrich می‌کند؛ فقط در غیاب آن fallback می‌سازد.</summary>
    public static Activity? BeginConsumeActivity(string eventType, string? correlationId)
    {
        var current = Activity.Current;
        if (current is not null && ToobaTracingPolicy.IsMassTransitOwnedActivity(current))
        {
            ToobaTraceEnricher.Enrich(current, correlationId, requestKind: TracingRequestKinds.Message);
            current.SetTag("tooba.event_type", eventType);
            return null;
        }

        if (current is not null)
        {
            ToobaTraceEnricher.Enrich(current, correlationId, requestKind: TracingRequestKinds.Message);
            current.SetTag("tooba.event_type", eventType);
            return null;
        }

        var fallback = ToobaTelemetry.ActivitySource.StartActivity("tooba.messaging.consume", ActivityKind.Consumer);
        ToobaTraceEnricher.Enrich(fallback, correlationId, requestKind: TracingRequestKinds.Message);
        fallback?.SetTag("tooba.event_type", eventType);
        return fallback;
    }

    /// <summary>W3C traceparent جاری برای ماندگاری اختیاری Outbox.</summary>
    public static string? CaptureTraceParent()
    {
        var activity = Activity.Current;
        if (activity is null || activity.IdFormat != ActivityIdFormat.W3C)
        {
            return null;
        }

        return activity.Id;
    }

    /// <summary>tracestate جاری.</summary>
    public static string? CaptureTraceState() => Activity.Current?.TraceStateString;
}
