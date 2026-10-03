using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.ModuleContracts;
using Tooba.OperatorProfile.Application.Ports;
using Tooba.OperatorProfile.Contracts.Errors;
using Tooba.OperatorProfile.Contracts.Ports;
using Tooba.OperatorProfile.Infrastructure.Adapters;
using Tooba.OperatorProfile.Infrastructure.Persistence;
using Tooba.OperatorProfile.Infrastructure.Profiles;
using Tooba.Persistence;

namespace Tooba.OperatorProfile.Infrastructure;

/// <summary>ماژول مستقل پروفایل توصیفی اپراتور با schema، قرارداد و Outbox اختصاصی.</summary>
public sealed class OperatorProfileModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "OperatorProfile";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton<IOutboxModuleRegistration, OperatorProfileOutboxRegistration>();
        services.AddSingleton<IErrorCatalogContributor, OperatorProfileErrorCatalogContributor>();
        services.AddSingleton<IErrorResourceSet, OperatorProfileErrorResourceSet>();
        services.AddScoped<IOperatorProfileDirectory, OperatorProfileDirectory>();
        services.AddScoped<IActorDisplayLookup, ActorDisplayLookupAdapter>();
        services.AddModuleSchemaMigrator<OperatorProfileDbContext>("OperatorProfile", ModuleSchemaMigrationOrder.OperatorProfile);
        services.AddDbContext<OperatorProfileDbContext>((sp, options) =>
        {
            var connection = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connection,
                OperatorProfileDbContext.Schema,
                typeof(OperatorProfileDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}
