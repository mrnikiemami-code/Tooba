using System.Diagnostics.Metrics;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Correlation;
using Tooba.BuildingBlocks.Observability.Logging;
using Tooba.BuildingBlocks.Observability.Messaging;
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
        using var correlationScope = MessagingCorrelation.BeginConsumeScope(
            envelope.CorrelationId,
            context.CorrelationId,
            envelope.EventId);
        var correlationId = CorrelationIdContext.Current ?? envelope.EventId.ToString("N");

        using var fallbackActivity = MessagingCorrelation.BeginConsumeActivity(envelope.EventType, correlationId);
        System.Diagnostics.Activity.Current?.SetTag("tooba.tenant_id", envelope.TenantId ?? string.Empty);
        System.Diagnostics.Activity.Current?.SetTag("tooba.edition", envelope.Edition);
        System.Diagnostics.Activity.Current?.SetTag("tooba.deployment_id", envelope.DeploymentId);
        System.Diagnostics.Activity.Current?.SetTag("tooba.event_id", envelope.EventId.ToString());
        System.Diagnostics.Activity.Current?.SetTag("tooba.endpoint", "tooba-integration");

        var shape = ToOutboxShape(envelope, correlationId);
        var integration = _serializer.Deserialize(shape);
        var assigner = _services.GetRequiredService<ICommerceContextAssigner>();
        assigner.Assign(_workerContext.FromOutbox(shape, correlationId));

        var currentTenant = _services.GetRequiredService<ICurrentTenant>().Current?.TenantId.Value;
        if (!string.IsNullOrWhiteSpace(envelope.TenantId)
            && !string.Equals(currentTenant, envelope.TenantId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Consumer tenant context does not match durable TenantId.");
        }

        var logState = ObservabilityLogScope.CreateState(correlationId, tenantId: envelope.TenantId);
        using (ObservabilityLogScope.Begin(_logger, logState))
        {
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
    }

    /// <summary>
    /// شکل Outbox را فقط برای deserialize و بازسازی زمینه می‌سازد؛ جدول Outbox را دوباره نمی‌نویسد.
    /// </summary>
    private static OutboxMessage ToOutboxShape(ToobaIntegrationTransportMessage envelope, string correlationId) =>
        new()
        {
            Id = envelope.EventId,
            OccurredAt = Instant.FromDateTimeOffset(envelope.OccurredAt),
            EventType = envelope.EventType,
            Payload = envelope.PayloadJson,
            CorrelationId = correlationId,
            Version = envelope.Version,
            TenantId = envelope.TenantId,
            DeploymentId = envelope.DeploymentId,
            Edition = envelope.Edition,
        };
}
