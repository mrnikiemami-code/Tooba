using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.Localization.Application;
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
        services.AddHostedService<LanguageBootstrapHostedService>();
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

public sealed class LocalizationOutboxRegistration : IOutboxModuleRegistration
{
    public string Schema => LocalizationDbContext.Schema;
    public string TableName => OutboxMessageMapping.TableName;
    public Type DbContextType => typeof(LocalizationDbContext);
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;
    public string GetEventTypeName(Type integrationEventType) =>
        throw new InvalidOperationException("Localization integration event is not registered.");
    public Type? ResolveEventClrType(string eventTypeName) => null;
}
