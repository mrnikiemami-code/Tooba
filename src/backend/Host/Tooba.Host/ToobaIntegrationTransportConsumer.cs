using System.Diagnostics.Metrics;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Tooba.BuildingBlocks;
using Tooba.Persistence;

namespace Tooba.Host;

/// <summary>
/// آداپتور مصرف MassTransit. handlerهای Tooba را صدا می‌زند و Tenant را از پاکت پایدار بازسازی می‌کند نه از Host.
/// </summary>
internal sealed class ToobaIntegrationTransportConsumer : IConsumer<ToobaIntegrationTransportMessage>
{
    private static readonly Counter<long> Consumed = ToobaTelemetry.Meter.CreateCounter<long>("tooba.messaging.consumed");

    private readonly IServiceProvider _services;
    private readonly IIntegrationEventSerializer _serializer;
    private readonly WorkerCommerceContextFactory _workerContext;
    private readonly ILogger<ToobaIntegrationTransportConsumer> _logger;

    /// <summary>
    /// مصرف‌کننده را به serializer، کارخانهٔ زمینه و DI همان consume-scope وصل می‌کند.
    /// </summary>
    public ToobaIntegrationTransportConsumer(
        IServiceProvider services,
        IIntegrationEventSerializer serializer,
        WorkerCommerceContextFactory workerContext,
        ILogger<ToobaIntegrationTransportConsumer> logger)
    {
        _services = services;
        _serializer = serializer;
        _workerContext = workerContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<ToobaIntegrationTransportMessage> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var envelope = context.Message;
        using var activity = ToobaTelemetry.ActivitySource.StartActivity("tooba.messaging.consume");
        activity?.SetTag("tooba.event_type", envelope.EventType);
        activity?.SetTag("tooba.tenant_id", envelope.TenantId ?? string.Empty);
        activity?.SetTag("tooba.edition", envelope.Edition);
        activity?.SetTag("tooba.deployment_id", envelope.DeploymentId);
        activity?.SetTag("tooba.event_id", envelope.EventId.ToString());
        activity?.SetTag("tooba.endpoint", "tooba-integration");

        var shape = ToOutboxShape(envelope);
        var integration = _serializer.Deserialize(shape);
        var assigner = _services.GetRequiredService<ICommerceContextAssigner>();
        var traceId = envelope.CorrelationId ?? envelope.EventId.ToString("N");
        assigner.Assign(_workerContext.FromOutbox(shape, traceId));

        var currentTenant = _services.GetRequiredService<ICurrentTenant>().Current?.TenantId.Value;
        if (!string.IsNullOrWhiteSpace(envelope.TenantId)
            && !string.Equals(currentTenant, envelope.TenantId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Consumer tenant context does not match durable TenantId.");
        }

        var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(integration.GetType());
        var handlers = _services.GetServices(handlerType);
        var any = false;
        foreach (var handler in handlers)
        {
            if (handler is null)
            {
                continue;
            }

            any = true;
            var method = handlerType.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.HandleAsync))
                ?? throw new InvalidOperationException("Integration handler contract is missing HandleAsync.");
            var task = (Task?)method.Invoke(handler, [integration, context.CancellationToken])
                ?? throw new InvalidOperationException("Integration handler returned no task.");
            await task.ConfigureAwait(false);
        }

        Consumed.Add(1);
        _logger.LogInformation(
            "Integration message consumed. EventType={EventType} TenantId={TenantId} Edition={Edition} DeploymentId={DeploymentId} EventId={EventId} HandlersPresent={HandlersPresent}",
            envelope.EventType,
            envelope.TenantId ?? string.Empty,
            envelope.Edition,
            envelope.DeploymentId,
            envelope.EventId,
            any);
    }

    /// <summary>
    /// شکل Outbox را فقط برای deserialize و بازسازی زمینه می‌سازد؛ جدول Outbox را دوباره نمی‌نویسد.
    /// </summary>
    private static OutboxMessage ToOutboxShape(ToobaIntegrationTransportMessage envelope) =>
        new()
        {
            Id = envelope.EventId,
            OccurredAt = Instant.FromDateTimeOffset(envelope.OccurredAt),
            EventType = envelope.EventType,
            Payload = envelope.PayloadJson,
            CorrelationId = envelope.CorrelationId,
            Version = envelope.Version,
            TenantId = envelope.TenantId,
            DeploymentId = envelope.DeploymentId,
            Edition = envelope.Edition,
        };
}
