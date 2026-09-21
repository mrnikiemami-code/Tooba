using System.Diagnostics.Metrics;
using MassTransit;
using System.Diagnostics;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Correlation;
using Tooba.BuildingBlocks.Observability.Messaging;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.Persistence;

namespace Tooba.Host;

/// <summary>
/// آداپتور ناشر: Outbox را به MassTransit SQL Transport می‌سپارد. handler کسب‌وکار را صدا نمی‌زند.
/// </summary>
internal sealed class MassTransitIntegrationEventPublisher : IIntegrationEventPublisher
{
    private static readonly Counter<long> Published = ToobaTelemetry.Meter.CreateCounter<long>("tooba.messaging.published");

    private readonly IBus _bus;
    private readonly IIntegrationEventSerializer _serializer;

    /// <summary>
    /// ناشر را به bus MassTransit و serializer type map وصل می‌کند.
    /// </summary>
    public MassTransitIntegrationEventPublisher(
        IBus bus,
        IIntegrationEventSerializer serializer)
    {
        _bus = bus;
        _serializer = serializer;
    }

    /// <inheritdoc />
    public async Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        var meta = integrationEvent.Metadata;
        var correlationId = MessagingCorrelation.ResolveForPublish(meta.CorrelationId, meta.EventId);
        using var correlationScope = CorrelationIdContext.BeginScope(correlationId);

        Activity? owned = null;
        var current = Activity.Current;
        if (current is null)
        {
            owned = ToobaTelemetry.ActivitySource.StartActivity("tooba.messaging.publish");
            current = owned;
        }

        using (owned)
        {
            ToobaTraceEnricher.Enrich(current, correlationId, requestKind: TracingRequestKinds.Message);
            current?.SetTag("tooba.event_type", meta.EventType);
            current?.SetTag("tooba.tenant_id", meta.TenantId ?? string.Empty);
            current?.SetTag("tooba.edition", meta.Edition.ToString());
            current?.SetTag("tooba.deployment_id", meta.DeploymentId);
            current?.SetTag("tooba.event_id", meta.EventId.ToString());

            var envelope = new ToobaIntegrationTransportMessage
            {
                EventType = meta.EventType,
                Version = meta.Version,
                EventId = meta.EventId,
                OccurredAt = meta.OccurredAt,
                TenantId = meta.TenantId,
                Edition = meta.Edition.ToString(),
                DeploymentId = meta.DeploymentId,
                CorrelationId = correlationId,
                PayloadJson = _serializer.SerializePayload(integrationEvent),
            };

            await _bus.Publish(
                envelope,
                context =>
                {
                    if (CorrelationIdContext.TryParseGuid(correlationId, out var corrGuid))
                    {
                        context.CorrelationId = corrGuid;
                    }

                    context.Headers.Set("tooba.event-type", meta.EventType);
                    context.Headers.Set("tooba.tenant-id", meta.TenantId ?? string.Empty);
                    context.Headers.Set("tooba.edition", meta.Edition.ToString());
                    context.Headers.Set("tooba.deployment-id", meta.DeploymentId);
                    context.Headers.Set("tooba.event-id", meta.EventId.ToString("N"));
                    context.Headers.Set(CorrelationIdConstants.HeaderName, correlationId);
                    var traceParent = MessagingCorrelation.CaptureTraceParent();
                    if (!string.IsNullOrWhiteSpace(traceParent))
                    {
                        context.Headers.Set("traceparent", traceParent);
                    }

                    var traceState = MessagingCorrelation.CaptureTraceState();
                    if (!string.IsNullOrWhiteSpace(traceState))
                    {
                        context.Headers.Set("tracestate", traceState);
                    }
                },
                cancellationToken).ConfigureAwait(false);
        }

        Published.Add(1);
    }
}
