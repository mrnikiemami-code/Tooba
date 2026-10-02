using Microsoft.Extensions.Options;

namespace Tooba.Host.Outbox;

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
