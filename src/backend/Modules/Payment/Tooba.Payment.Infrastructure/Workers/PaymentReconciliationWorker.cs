using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Payment.Application.Commands.ReconcileStalePayments;
using Tooba.Payment.Infrastructure.Providers;
using Tooba.Persistence;

namespace Tooba.Payment.Infrastructure.Workers;

/// <summary>
/// Payment-owned stale-payment reconciliation worker: tenant loop, scoped <see cref="ISender"/> dispatch,
/// per-tenant failure isolation, and reconciliation telemetry. The tenant target source, commerce-context
/// factory, and worker registry are generic platform seams supplied by Host; Payment never references Host.
/// </summary>
public sealed class PaymentReconciliationWorker : BackgroundService
{
    /// <summary>نام پایدار کارگر در <see cref="IBackgroundWorkerRegistry"/>.</summary>
    public const string WorkerName = "payment-reconciliation";

    private readonly IOutboxPollTargetSource _targets;
    private readonly IWorkerCommerceContextFactory _workerContext;
    private readonly IServiceScopeFactory _scopes;
    private readonly PaymentReconciliationOptions _options;
    private readonly IBackgroundWorkerRegistry _registry;
    private readonly PaymentGatewayInstrumentation _telemetry;
    private readonly ILogger<PaymentReconciliationWorker> _logger;

    /// <summary>
    /// کارگر را به اهداف Tenant و زمینهٔ بدون HTTP وصل می‌کند.
    /// </summary>
    /// <param name="targets">منبع اهداف poll.</param>
    /// <param name="workerContext">سازندهٔ زمینهٔ کارگر بدون خواندن هدر HTTP.</param>
    /// <param name="scopes">سازندهٔ scope برای هر هدف.</param>
    /// <param name="options">knobs زمان‌بندی Payment-owned.</param>
    /// <param name="registry">رجیستری وضعیت کارگر.</param>
    /// <param name="telemetry">تله‌متری reconciliation درگاه.</param>
    /// <param name="logger">لاگر.</param>
    public PaymentReconciliationWorker(
        IOutboxPollTargetSource targets,
        IWorkerCommerceContextFactory workerContext,
        IServiceScopeFactory scopes,
        IOptions<PaymentReconciliationOptions> options,
        IBackgroundWorkerRegistry registry,
        PaymentGatewayInstrumentation telemetry,
        ILogger<PaymentReconciliationWorker> logger)
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

        var delay = _options.NormalizedPollInterval;
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
                var ids = scope.ServiceProvider.GetRequiredService<IIdGenerator>();
                assigner.Assign(_workerContext.FromPollTarget(target, ids.NewId().ToString("N")));
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var result = await sender.Send(
                    new ReconcileStalePaymentsCommand(
                        _options.NormalizedPendingAge,
                        _options.NormalizedBatchSize),
                    cancellationToken).ConfigureAwait(false);
                if (result.IsFailure)
                    throw new InvalidOperationException(result.Errors[0].Code);
                total += result.Value;
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
