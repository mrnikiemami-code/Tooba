using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks;

namespace Tooba.Localization.Infrastructure.Bootstrap;

/// <summary>bootstrap idempotent fa/en — فقط وقتی جدول خالی است.</summary>
public sealed class LanguageBootstrapHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LanguageBootstrapHostedService> _logger;

    public LanguageBootstrapHostedService(IServiceScopeFactory scopeFactory, ILogger<LanguageBootstrapHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            await LanguageBootstrapRunner.MigrateAndSeedAsync(scope.ServiceProvider, cancellationToken);
        }
        catch (PlatformHttpException ex) when (ex.StatusCode == StatusCodes.Status503ServiceUnavailable)
        {
            // در استارت میزبان هیچ زمینهٔ تجاری درخواستی تثبیت نشده است، پس resolve کردن اتصال
            // به‌درستی fail-closed می‌شود. این وضعیت «خطای نامنتظر» نیست: در Development گام
            // مالک Localization (LanguageBootstrapDevelopmentSeed.PostMigration) دانه را داخل
            // scope تثبیت‌شده اجرا می‌کند و این سرویس دیگر هشدار کاذب تولید نمی‌کند.
            _logger.LogInformation(
                ex,
                "Language bootstrap skipped: no commerce context is bound at host startup. "
                + "Development initialization order is owned by the Localization post-migration seed step.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Language bootstrap failed; Host continues with fail-safe locale helpers.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
