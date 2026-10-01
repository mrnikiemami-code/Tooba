using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Persistence;

namespace Tooba.Order.Infrastructure.ReservationCycle;

/// <summary>
/// Order-owned worker shell for unpaid-payment expiry. Host supplies only neutral worker/context seams.
/// Business reconciliation remains in <see cref="IUnpaidOrderExpiryReconciler"/>.
/// </summary>
internal sealed class UnpaidOrderExpiryWorker : BackgroundService
{
    private static readonly Counter<long> ExpiredPayments =
        ToobaTelemetry.Meter.CreateCounter<long>("tooba.unpaid_expiry.expired");

    private readonly IOutboxPollTargetSource _targets;
    private readonly IWorkerCommerceContextFactory _workerContext;
    private readonly IServiceScopeFactory _scopes;
    private readonly UnpaidOrderExpiryWorkerOptions _options;
    private readonly ILogger<UnpaidOrderExpiryWorker> _logger;

    public UnpaidOrderExpiryWorker(
        IOutboxPollTargetSource targets,
        IWorkerCommerceContextFactory workerContext,
        IServiceScopeFactory scopes,
        IOptions<UnpaidOrderExpiryWorkerOptions> options,
        ILogger<UnpaidOrderExpiryWorker> logger)
    {
        _targets = targets;
        _workerContext = workerContext;
        _scopes = scopes;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Unpaid order expiry worker is disabled by configuration.");
            return;
        }

        var delay = TimeSpan.FromSeconds(Math.Max(5, _options.PollIntervalSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ReconcileOnceAsync(stoppingToken).ConfigureAwait(false);
                if (processed > 0)
                {
                    ExpiredPayments.Add(processed);
                    _logger.LogInformation(
                        "Unpaid expiry cycle completed. ExpiredCount={ExpiredCount}",
                        processed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unpaid expiry loop error. ErrorType={ErrorType}", ex.GetType().Name);
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
                var reconciler = scope.ServiceProvider.GetRequiredService<IUnpaidOrderExpiryReconciler>();
                total += await reconciler.ReconcileAsync(_options.BatchSize, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Unpaid expiry failed for one tenant. TenantId={TenantId} ErrorType={ErrorType}",
                    target.TenantId ?? string.Empty,
                    ex.GetType().Name);
            }
        }

        return total;
    }
}
