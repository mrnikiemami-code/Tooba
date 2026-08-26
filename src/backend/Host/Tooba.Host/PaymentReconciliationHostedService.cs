using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Payment.Application;
using Tooba.Payment.Infrastructure;
using Tooba.Persistence;

namespace Tooba.Host;

/// <summary>
/// کارگر reconciliation: پرداخت‌های Pending قدیمی را Verify می‌کند؛ callback گم‌شده جبران می‌شود.
/// </summary>
internal sealed class PaymentReconciliationHostedService : BackgroundService
{
    public const string WorkerName = "payment-reconciliation";

    private readonly IOutboxPollTargetSource _targets;
    private readonly WorkerCommerceContextFactory _workerContext;
    private readonly IServiceScopeFactory _scopes;
    private readonly PaymentReconciliationHostOptions _options;
    private readonly BackgroundWorkerRegistry _registry;
    private readonly PaymentGatewayInstrumentation _telemetry;
    private readonly ILogger<PaymentReconciliationHostedService> _logger;

    /// <summary>
    /// کارگر را به اهداف Tenant و زمینهٔ بدون HTTP وصل می‌کند.
    /// </summary>
    public PaymentReconciliationHostedService(
        IOutboxPollTargetSource targets,
        WorkerCommerceContextFactory workerContext,
        IServiceScopeFactory scopes,
        IOptions<PaymentReconciliationHostOptions> options,
        BackgroundWorkerRegistry registry,
        PaymentGatewayInstrumentation telemetry,
        ILogger<PaymentReconciliationHostedService> logger)
    {
        _targets = targets;
        _workerContext = workerContext;
        _scopes = scopes;
        _options = options.Value;
        _registry = registry;
        _telemetry = telemetry;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Payment reconciliation worker is disabled by configuration.");
            return;
        }

        var delay = TimeSpan.FromSeconds(Math.Max(15, _options.PollIntervalSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ReconcileOnceAsync(stoppingToken).ConfigureAwait(false);
                _registry.RecordSuccess(WorkerName, processed);
                if (processed > 0)
                {
                    _telemetry.RecordReconcile(processed);
                    _logger.LogInformation(
                        "Payment reconciliation cycle completed. ProcessedCount={ProcessedCount}",
                        processed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _registry.RecordFailure(WorkerName, ex.GetType().Name);
                _logger.LogWarning(ex, "Payment reconciliation loop error. ErrorType={ErrorType}", ex.GetType().Name);
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

    private async Task<int> ReconcileOnceAsync(CancellationToken cancellationToken)
    {
        var total = 0;
        foreach (var target in _targets.GetTargets())
        {
            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                var assigner = scope.ServiceProvider.GetRequiredService<ICommerceContextAssigner>();
                assigner.Assign(_workerContext.FromPollTarget(target, Guid.NewGuid().ToString("N")));
                var reconciliation = scope.ServiceProvider.GetRequiredService<IPaymentReconciliationDirectory>();
                using var activity = ToobaTelemetry.ActivitySource.StartActivity("tooba.payment.reconcile");
                activity?.SetTag("tooba.tenant_id", target.TenantId ?? string.Empty);
                var processed = await reconciliation.ReconcileStalePendingAsync(
                    DateTimeOffset.UtcNow,
                    TimeSpan.FromMinutes(Math.Max(1, _options.PendingAgeMinutes)),
                    _options.BatchSize,
                    cancellationToken).ConfigureAwait(false);
                total += processed;
            }
            catch (Exception ex)
            {
                _registry.RecordFailure(WorkerName, ex.GetType().Name);
                _logger.LogWarning(
                    ex,
                    "Payment reconciliation failed for one tenant. TenantId={TenantId} ErrorType={ErrorType}",
                    target.TenantId ?? string.Empty,
                    ex.GetType().Name);
            }
        }

        return total;
    }
}
