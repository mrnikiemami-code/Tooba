using System.Diagnostics.Metrics;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Cart.Application.Lifetime;
using Tooba.Persistence;

namespace Tooba.Host;

/// <summary>
/// Host shell only: tenant loop, config, logging, telemetry.
/// Cart expiry business reconciliation lives in <see cref="ICartExpiryReconciler"/>.
/// </summary>
internal sealed class CartExpiryHostedService : BackgroundService
{
    public const string WorkerName = "cart-expiry";

    private static readonly Counter<long> ExpiredCarts = ToobaTelemetry.Meter.CreateCounter<long>("tooba.cart_expiry.expired");
    private static readonly Counter<long> TenantFailures = ToobaTelemetry.Meter.CreateCounter<long>("tooba.cart_expiry.tenant_failures");

    private readonly IOutboxPollTargetSource _targets;
    private readonly WorkerCommerceContextFactory _workerContext;
    private readonly IServiceScopeFactory _scopes;
    private readonly CartExpiryHostOptions _options;
    private readonly BackgroundWorkerRegistry _registry;
    private readonly ILogger<CartExpiryHostedService> _logger;

    /// <summary>
    /// کارگر را به اهداف Tenant و زمینهٔ بدون HTTP وصل می‌کند.
    /// </summary>
    public CartExpiryHostedService(
        IOutboxPollTargetSource targets,
        WorkerCommerceContextFactory workerContext,
        IServiceScopeFactory scopes,
        IOptions<CartExpiryHostOptions> options,
        BackgroundWorkerRegistry registry,
        ILogger<CartExpiryHostedService> logger)
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
            _logger.LogInformation("Cart expiry worker is disabled by configuration.");
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
                    ExpiredCarts.Add(processed);
                    _logger.LogInformation(
                        "Cart expiry cycle completed. ExpiredCount={ExpiredCount}",
                        processed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                TenantFailures.Add(1);
                _registry.RecordFailure(WorkerName, ex.GetType().Name);
                _logger.LogWarning(ex, "Cart expiry loop error. ErrorType={ErrorType}", ex.GetType().Name);
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

    /// <summary>
    /// برای هر Tenant فعال، یک reconciler تحت مالکیت Cart را صدا می‌زند.
    /// </summary>
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
                var reconciler = scope.ServiceProvider.GetRequiredService<ICartExpiryReconciler>();
                total += await reconciler.ReconcileAsync(_options.BatchSize, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                TenantFailures.Add(1);
                _registry.RecordFailure(WorkerName, ex.GetType().Name);
                _logger.LogWarning(
                    ex,
                    "Cart expiry failed for one tenant. TenantId={TenantId} ErrorType={ErrorType}",
                    target.TenantId ?? string.Empty,
                    ex.GetType().Name);
            }
        }

        return total;
    }
}
