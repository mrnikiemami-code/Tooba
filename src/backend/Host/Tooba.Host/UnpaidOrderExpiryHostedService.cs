using System.Diagnostics.Metrics;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Payment.Application;
using Tooba.Persistence;

namespace Tooba.Host;

/// <summary>
/// کارگر سرور: پرداخت unpaid سررسید را Expired می‌کند و رزرو سفارش را آزاد می‌کند.
/// </summary>
internal sealed class UnpaidOrderExpiryHostedService : BackgroundService
{
    public const string WorkerName = "unpaid-order-expiry";

    private static readonly Counter<long> ExpiredPayments = ToobaTelemetry.Meter.CreateCounter<long>("tooba.unpaid_expiry.expired");

    private readonly IOutboxPollTargetSource _targets;
    private readonly WorkerCommerceContextFactory _workerContext;
    private readonly IServiceScopeFactory _scopes;
    private readonly UnpaidOrderExpiryHostOptions _options;
    private readonly BackgroundWorkerRegistry _registry;
    private readonly ILogger<UnpaidOrderExpiryHostedService> _logger;

    /// <summary>کارگر را به اهداف Tenant وصل می‌کند.</summary>
    public UnpaidOrderExpiryHostedService(
        IOutboxPollTargetSource targets,
        WorkerCommerceContextFactory workerContext,
        IServiceScopeFactory scopes,
        IOptions<UnpaidOrderExpiryHostOptions> options,
        BackgroundWorkerRegistry registry,
        ILogger<UnpaidOrderExpiryHostedService> logger)
    {
        _targets = targets;
        _workerContext = workerContext;
        _scopes = scopes;
        _options = options.Value;
        _registry = registry;
        _logger = logger;
    }

    /// <inheritdoc />
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
                _registry.RecordSuccess(WorkerName, processed);
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
                _registry.RecordFailure(WorkerName, ex.GetType().Name);
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
                var expiry = scope.ServiceProvider.GetRequiredService<IPaymentExpiryDirectory>();
                var projection = scope.ServiceProvider.GetRequiredService<IOrderPaymentProjection>();
                var checkoutIds = await expiry.ExpireDueUnpaidAsync(
                    DateTimeOffset.UtcNow,
                    _options.BatchSize,
                    cancellationToken).ConfigureAwait(false);
                foreach (var checkoutId in checkoutIds.Distinct())
                {
                    await projection.ReleaseReservationsAfterManualRejectAsync(checkoutId, cancellationToken)
                        .ConfigureAwait(false);
                }

                total += checkoutIds.Count;
            }
            catch (Exception ex)
            {
                _registry.RecordFailure(WorkerName, ex.GetType().Name);
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
