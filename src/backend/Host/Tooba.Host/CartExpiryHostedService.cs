using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Cart.Application;
using Tooba.Persistence;

namespace Tooba.Host;

/// <summary>
/// کارگر سرور: سبد منقضی را آزاد می‌کند تا موجودی Held برای مرورگر بسته‌شده قفل نماند.
/// </summary>
internal sealed class CartExpiryHostedService : BackgroundService
{
    private readonly IOutboxPollTargetSource _targets;
    private readonly WorkerCommerceContextFactory _workerContext;
    private readonly IServiceScopeFactory _scopes;
    private readonly OutboxHostOptions _options;
    private readonly ILogger<CartExpiryHostedService> _logger;

    /// <summary>
    /// کارگر را به اهداف Tenant و زمینهٔ بدون HTTP وصل می‌کند.
    /// </summary>
    public CartExpiryHostedService(
        IOutboxPollTargetSource targets,
        WorkerCommerceContextFactory workerContext,
        IServiceScopeFactory scopes,
        IOptions<OutboxHostOptions> options,
        ILogger<CartExpiryHostedService> logger)
    {
        _targets = targets;
        _workerContext = workerContext;
        _scopes = scopes;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Cart expiry worker is disabled with Outbox dispatcher.");
            return;
        }

        var delay = TimeSpan.FromSeconds(Math.Max(5, _options.PollIntervalSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReconcileOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Cart expiry loop error. ErrorType={ErrorType}", ex.GetType().Name);
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
    /// برای هر Tenant فعال، سبد سررسیدشده را در سرور منقضی می‌کند.
    /// </summary>
    private async Task ReconcileOnceAsync(CancellationToken cancellationToken)
    {
        foreach (var target in _targets.GetTargets())
        {
            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                var assigner = scope.ServiceProvider.GetRequiredService<ICommerceContextAssigner>();
                assigner.Assign(_workerContext.FromPollTarget(target, Guid.NewGuid().ToString("N")));
                var carts = scope.ServiceProvider.GetRequiredService<ICartDirectory>();
                await carts.ExpireDueCartsAsync(DateTimeOffset.UtcNow, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "Cart expiry failed for one tenant. TenantId={TenantId} ErrorType={ErrorType}",
                    target.TenantId ?? string.Empty,
                    ex.GetType().Name);
            }
        }
    }
}
