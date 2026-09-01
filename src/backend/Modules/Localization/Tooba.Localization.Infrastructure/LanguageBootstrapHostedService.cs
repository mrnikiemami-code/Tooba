using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tooba.Localization.Application;
using Tooba.Localization.Infrastructure.Persistence;

namespace Tooba.Localization.Infrastructure;

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
            var db = scope.ServiceProvider.GetRequiredService<LocalizationDbContext>();
            await db.Database.MigrateAsync(cancellationToken);
            var directory = scope.ServiceProvider.GetRequiredService<ILanguageDirectory>();
            await directory.BootstrapAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Language bootstrap failed; Host continues with fail-safe locale helpers.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
