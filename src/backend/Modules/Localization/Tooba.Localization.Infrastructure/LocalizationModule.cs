using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.Localization.Application.Ports;
using Tooba.Localization.Contracts.Ports;
using Tooba.Localization.Infrastructure.Adapters;
using Tooba.Localization.Infrastructure.Bootstrap;
using Tooba.Localization.Infrastructure.Languages;
using Tooba.Localization.Infrastructure.Persistence;
using Tooba.ModuleContracts;
using Tooba.Persistence;

namespace Tooba.Localization.Infrastructure;

/// <summary>ماژول Localization با schema مستقل.</summary>
public sealed class LocalizationModule : IToobaModule
{
    public string Name => "Localization";

    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton<IOutboxModuleRegistration, LocalizationOutboxRegistration>();
        services.AddScoped<ILanguageDirectory, LanguageDirectory>();
        services.AddScoped<ILanguageLookup, LanguageLookupBridge>();
        services.AddScoped<ILanguageActivationPort, LanguageActivationBridge>();
        services.AddHostedService<LanguageBootstrapHostedService>();
        services.AddModuleSchemaMigrator<LocalizationDbContext>("Localization", ModuleSchemaMigrationOrder.Localization);
        services.AddDbContext<LocalizationDbContext>((sp, options) =>
        {
            var connection = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connection,
                LocalizationDbContext.Schema,
                typeof(LocalizationDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}
