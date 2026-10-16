using System.Diagnostics.Metrics;
using System.Runtime.ExceptionServices;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Correlation;
using Tooba.BuildingBlocks.Observability.Logging;
using Tooba.BuildingBlocks.Observability.Messaging;
using Tooba.Host.Outbox;
using Tooba.Persistence;

namespace Tooba.Host.Transport;

/// <summary>
/// آداپتور مصرف MassTransit. handlerهای Tooba را صدا می‌زند و Tenant را از پاکت پایدار بازسازی می‌کند نه از Host.
/// </summary>
internal sealed class ToobaIntegrationTransportConsumer : IConsumer<ToobaIntegrationTransportMessage>
{
    private static readonly Counter<long> Consumed = ToobaTelemetry.Meter.CreateCounter<long>("tooba.messaging.consumed");
    private static readonly Counter<long> HandlerFailures = ToobaTelemetry.Meter.CreateCounter<long>("tooba.messaging.handler_failures");

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
            var invoked = await InvokeRegisteredHandlersAsync(
                _services,
                integration,
                _logger,
                context.CancellationToken).ConfigureAwait(false);

            Consumed.Add(1);
            _logger.LogInformation(
                "Integration message consumed. EventType={EventType} TenantId={TenantId} Edition={Edition} DeploymentId={DeploymentId} EventId={EventId} HandlersPresent={HandlersPresent}",
                envelope.EventType,
                envelope.TenantId ?? string.Empty,
                envelope.Edition,
                envelope.DeploymentId,
                envelope.EventId,
                invoked > 0);
        }
    }

    /// <summary>
    /// هر handler ثبت‌شدهٔ همین رویداد را مستقل صدا می‌زند. شکست یک ماژول تحویل بقیهٔ ماژول‌های
    /// هم‌قرارداد را متوقف نمی‌کند (هر ماژول Inbox خودش را دارد و تکراری نوشته نمی‌شود)، ولی در پایان
    /// خطاها با نام handler دوباره پرتاب می‌شوند تا retry/dead-letter حمل‌ونقل و observability شکست حفظ شود.
    /// </summary>
    internal static async Task<int> InvokeRegisteredHandlersAsync(
        IServiceProvider services,
        IIntegrationEvent integration,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(integration);

        var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(integration.GetType());
        var method = handlerType.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.HandleAsync))
            ?? throw new InvalidOperationException("Integration handler contract is missing HandleAsync.");

        var failures = new List<(string Handler, Exception Error)>();
        var invoked = 0;
        foreach (var handler in services.GetServices(handlerType))
        {
            if (handler is null)
            {
                continue;
            }

            invoked++;
            var handlerName = handler.GetType().Name;
            try
            {
                var task = (Task?)method.Invoke(handler, [integration, cancellationToken])
                    ?? throw new InvalidOperationException("Integration handler returned no task.");
                await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                var error = Unwrap(ex);
                if (error is OperationCanceledException && cancellationToken.IsCancellationRequested)
                {
                    ExceptionDispatchInfo.Capture(error).Throw();
                }

                failures.Add((handlerName, error));
                HandlerFailures.Add(1);
                logger.LogWarning(
                    "Integration handler failed. EventType={EventType} Handler={Handler} ErrorType={ErrorType}",
                    integration.Metadata.EventType,
                    handlerName,
                    error.GetType().Name);
            }
        }

        if (failures.Count > 0)
        {
            throw new IntegrationHandlerDeliveryException(failures);
        }

        return invoked;
    }

    /// <summary>
    /// خطای reflection را باز می‌کند تا نوع واقعی شکست handler (نه TargetInvocationException) دیده شود.
    /// </summary>
    private static Exception Unwrap(Exception exception) =>
        exception is System.Reflection.TargetInvocationException { InnerException: { } inner } ? inner : exception;

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

/// <summary>
/// شکست یک یا چند handler ماژول در تحویل یک رویداد Integration. فقط نام نوع خطا نگه داشته می‌شود
/// (بدون پیام خام) تا observability پایدار باشد و retry/dead-letter حمل‌ونقل فعال بماند.
/// </summary>
internal sealed class IntegrationHandlerDeliveryException : Exception
{
    /// <summary>
    /// شکست‌ها را به ترتیب فراخوانی handlerها نگه می‌دارد.
    /// </summary>
    public IntegrationHandlerDeliveryException(IReadOnlyList<(string Handler, Exception Error)> failures)
        : base(BuildMessage(failures))
    {
        Failures = failures;
    }

    /// <summary>handlerهای شکست‌خورده و نوع خطای هرکدام.</summary>
    public IReadOnlyList<(string Handler, Exception Error)> Failures { get; }

    private static string BuildMessage(IReadOnlyList<(string Handler, Exception Error)> failures)
    {
        var parts = failures.Select(x => $"{x.Handler}:{x.Error.GetType().Name}");
        return "Integration handler delivery failed: " + string.Join(", ", parts);
    }
}
