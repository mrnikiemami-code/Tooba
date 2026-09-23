using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Cart.Application.Lifetime;
using Tooba.Persistence;

namespace Tooba.Cart.Infrastructure.Lifetime;

/// <summary>
/// Cart-owned expiry worker: tenant loop, cancellation, per-tenant failure isolation, and telemetry.
/// Business reconciliation stays in <see cref="ICartExpiryReconciler"/>; the tenant target source,
/// commerce-context factory, and worker registry are generic platform seams supplied by Host.
/// </summary>
public sealed class CartExpiryWorker : BackgroundService
{
    /// <summary>نام پایدار کارگر در <see cref="IBackgroundWorkerRegistry"/>.</summary>
    public const string WorkerName = "cart-expiry";

    private static readonly Counter<long> ExpiredCarts = ToobaTelemetry.Meter.CreateCounter<long>("tooba.cart_expiry.expired");
    private static readonly Counter<long> TenantFailures = ToobaTelemetry.Meter.CreateCounter<long>("tooba.cart_expiry.tenant_failures");

    private readonly IOutboxPollTargetSource _targets;
    private readonly IWorkerCommerceContextFactory _workerContext;
    private readonly IIdGenerator _ids;
    private readonly IServiceScopeFactory _scopes;
    private readonly CartExpiryOptions _options;
    private readonly IBackgroundWorkerRegistry _registry;
    private readonly ILogger<CartExpiryWorker> _logger;

    /// <summary>
    /// کارگر را به اهداف Tenant و زمینهٔ بدون HTTP وصل می‌کند.
    /// </summary>
    /// <param name="targets">منبع اهداف poll.</param>
    /// <param name="workerContext">سازندهٔ زمینهٔ کارگر.</param>
    /// <param name="ids">تولید شناسهٔ همبستگی.</param>
    /// <param name="scopes">سازندهٔ scope.</param>
    /// <param name="options">knobs اجرای کارگر.</param>
    /// <param name="registry">رجیستری وضعیت کارگر.</param>
    /// <param name="logger">لاگر.</param>
    public CartExpiryWorker(
        IOutboxPollTargetSource targets,
        IWorkerCommerceContextFactory workerContext,
        IIdGenerator ids,
        IServiceScopeFactory scopes,
        IOptions<CartExpiryOptions> options,
        IBackgroundWorkerRegistry registry,
        ILogger<CartExpiryWorker> logger)
    {
        _targets = targets;
        _workerContext = workerContext;
        _ids = ids;
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
                assigner.Assign(_workerContext.FromPollTarget(target, _ids.NewId().ToString("N")));
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
