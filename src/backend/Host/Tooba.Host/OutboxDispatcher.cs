using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NodaTime;
using Tooba.BuildingBlocks;
using Tooba.Persistence;

namespace Tooba.Host;

/// <summary>
/// یک دور poll: Tenantها جدا، شکست یکی دیگری را خراب نمی‌کند، زمینه از پیام است نه Host.
/// </summary>
internal sealed class OutboxDispatcher
{
    private static readonly Counter<long> TenantFailures = ToobaTelemetry.Meter.CreateCounter<long>("tooba.outbox.tenant_failures");
    private static readonly Counter<long> Retries = ToobaTelemetry.Meter.CreateCounter<long>("tooba.outbox.retries");
    private static readonly Counter<long> DeadLetters = ToobaTelemetry.Meter.CreateCounter<long>("tooba.outbox.dead_letters");

    private readonly IOutboxPollTargetSource _targets;
    private readonly IEnumerable<IOutboxModuleRegistration> _modules;
    private readonly IOutboxDispatcherStore _store;
    private readonly IIntegrationEventSerializer _serializer;
    private readonly IDatabaseConnectionResolver _connections;
    private readonly WorkerCommerceContextFactory _workerContext;
    private readonly IServiceScopeFactory _scopes;
    private readonly OutboxHostOptions _options;
    private readonly ILogger<OutboxDispatcher> _logger;

    /// <summary>
    /// dispatcher را به store، serializer، registry و DI وصل می‌کند.
    /// </summary>
    public OutboxDispatcher(
        IOutboxPollTargetSource targets,
        IEnumerable<IOutboxModuleRegistration> modules,
        IOutboxDispatcherStore store,
        IIntegrationEventSerializer serializer,
        IDatabaseConnectionResolver connections,
        WorkerCommerceContextFactory workerContext,
        IServiceScopeFactory scopes,
        IOptions<OutboxHostOptions> options,
        ILogger<OutboxDispatcher> logger)
    {
        _targets = targets;
        _modules = modules;
        _store = store;
        _serializer = serializer;
        _connections = connections;
        _workerContext = workerContext;
        _scopes = scopes;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// همهٔ اهداف فعال را یک‌بار poll می‌کند. /ready به نتیجهٔ این حلقه وابسته نیست.
    /// </summary>
    public async Task DispatchOnceAsync(CancellationToken cancellationToken)
    {
        foreach (var target in _targets.GetTargets())
        {
            try
            {
                await DispatchTargetAsync(target, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                TenantFailures.Add(1);
                _logger.LogWarning(
                    "Outbox poll failed for one tenant/target. TenantId={TenantId} ErrorType={ErrorType}",
                    target.TenantId ?? string.Empty,
                    ex.GetType().Name);
            }
        }
    }

    private async Task DispatchTargetAsync(OutboxPollTarget target, CancellationToken cancellationToken)
    {
        var connectionString = _connections.Resolve(target.ConnectionReference);
        foreach (var module in _modules)
        {
            var claimed = await _store.ClaimAsync(
                connectionString,
                module.Schema,
                module.TableName,
                _options.BatchSize,
                _options.LockSeconds,
                cancellationToken).ConfigureAwait(false);

            foreach (var message in claimed)
            {
                using var activity = ToobaTelemetry.ActivitySource.StartActivity("tooba.outbox.dispatch");
                activity?.SetTag("tooba.event_type", message.EventType);
                activity?.SetTag("tooba.tenant_id", message.TenantId ?? string.Empty);
                activity?.SetTag("tooba.module_schema", module.Schema);

                try
                {
                    var integration = _serializer.Deserialize(message);
                    await using var scope = _scopes.CreateAsyncScope();
                    var assigner = scope.ServiceProvider.GetRequiredService<ICommerceContextAssigner>();
                    assigner.Assign(_workerContext.FromOutbox(message, message.CorrelationId ?? message.Id.ToString("N")));
                    var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
                    await publisher.PublishAsync(integration, cancellationToken).ConfigureAwait(false);
                    await _store.MarkProcessedAsync(
                        connectionString,
                        module.Schema,
                        module.TableName,
                        message.Id,
                        cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    var sanitized = OutboxErrorSanitizer.Sanitize(ex);
                    if (message.AttemptCount >= _options.MaxAttempts)
                    {
                        DeadLetters.Add(1);
                        await _store.MarkDeadLetterAsync(
                            connectionString,
                            module.Schema,
                            module.TableName,
                            message.Id,
                            sanitized,
                            cancellationToken).ConfigureAwait(false);
                        _logger.LogWarning(
                            "Outbox message dead-lettered. EventType={EventType} TenantId={TenantId} ErrorType={ErrorType}",
                            message.EventType,
                            message.TenantId ?? string.Empty,
                            ex.GetType().Name);
                    }
                    else
                    {
                        Retries.Add(1);
                        var delay = _options.RetryBaseDelaySeconds * (1 << Math.Min(message.AttemptCount - 1, 8));
                        var next = SystemClock.Instance.GetCurrentInstant()
                            .Plus(Duration.FromSeconds(Math.Max(delay, _options.RetryBaseDelaySeconds)));
                        await _store.MarkRetryAsync(
                            connectionString,
                            module.Schema,
                            module.TableName,
                            message.Id,
                            next,
                            sanitized,
                            cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}

/// <summary>
/// حلقهٔ پس‌زمینهٔ Outbox. آماده بودن فرآیند به خالی بودن صف وابسته نیست.
/// </summary>
internal sealed class OutboxDispatcherHostedService : BackgroundService
{
    private readonly OutboxDispatcher _dispatcher;
    private readonly OutboxHostOptions _options;
    private readonly ILogger<OutboxDispatcherHostedService> _logger;

    /// <summary>
    /// HostedService را به dispatcher و تنظیمات poll وصل می‌کند.
    /// </summary>
    public OutboxDispatcherHostedService(
        OutboxDispatcher dispatcher,
        IOptions<OutboxHostOptions> options,
        ILogger<OutboxDispatcherHostedService> logger)
    {
        _dispatcher = dispatcher;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Outbox dispatcher is disabled by configuration.");
            return;
        }

        var delay = TimeSpan.FromSeconds(Math.Max(1, _options.PollIntervalSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _dispatcher.DispatchOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Outbox dispatcher loop error. ErrorType={ErrorType}", ex.GetType().Name);
            }

            try
            {
                await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
