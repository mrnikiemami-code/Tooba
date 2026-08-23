using System.Diagnostics.Metrics;
using MassTransit;
using Tooba.BuildingBlocks;
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
        using var activity = ToobaTelemetry.ActivitySource.StartActivity("tooba.messaging.publish");
        var meta = integrationEvent.Metadata;
        activity?.SetTag("tooba.event_type", meta.EventType);
        activity?.SetTag("tooba.tenant_id", meta.TenantId ?? string.Empty);
        activity?.SetTag("tooba.edition", meta.Edition.ToString());
        activity?.SetTag("tooba.deployment_id", meta.DeploymentId);
        activity?.SetTag("tooba.event_id", meta.EventId.ToString());

        var envelope = new ToobaIntegrationTransportMessage
        {
            EventType = meta.EventType,
            Version = meta.Version,
            EventId = meta.EventId,
            OccurredAt = meta.OccurredAt,
            TenantId = meta.TenantId,
            Edition = meta.Edition.ToString(),
            DeploymentId = meta.DeploymentId,
            CorrelationId = meta.CorrelationId,
            PayloadJson = _serializer.SerializePayload(integrationEvent),
        };

        await _bus.Publish(
            envelope,
            context =>
            {
                context.CorrelationId = meta.EventId;
                context.Headers.Set("tooba.event-type", meta.EventType);
                context.Headers.Set("tooba.tenant-id", meta.TenantId ?? string.Empty);
                context.Headers.Set("tooba.edition", meta.Edition.ToString());
                context.Headers.Set("tooba.deployment-id", meta.DeploymentId);
                context.Headers.Set("tooba.event-id", meta.EventId.ToString("N"));
            },
            cancellationToken).ConfigureAwait(false);

        Published.Add(1);
    }
}
