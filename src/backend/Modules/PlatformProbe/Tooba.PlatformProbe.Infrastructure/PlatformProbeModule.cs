using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Persistence;
using Tooba.PlatformProbe.Infrastructure.Persistence;

namespace Tooba.PlatformProbe.Infrastructure;

/// <summary>
/// نمونهٔ disposable ثبت PlatformProbe. قابلیت کسب‌وکار نیست و فقط ترکیب و مرز ماژول را ثابت می‌کند.
/// </summary>
public sealed class PlatformProbeModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "PlatformProbe";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddSingleton<IOutboxModuleRegistration, PlatformProbeOutboxRegistration>();
        services.AddDbContext<PlatformProbeDbContext>((sp, options) =>
        {
            var connectionString = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connectionString,
                PlatformProbeDbContext.Schema,
                typeof(PlatformProbeDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}
