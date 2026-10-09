using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.ModuleContracts;
using Tooba.Persistence;
using Tooba.Tax.Application.Ports;
using Tooba.Tax.Contracts.Errors;
using Tooba.Tax.Contracts.Ports;
using Tooba.Tax.Infrastructure.Adapters;
using Tooba.Tax.Infrastructure.Outbox;
using Tooba.Tax.Infrastructure.Persistence;

namespace Tooba.Tax.Infrastructure.DependencyInjection;

/// <summary>
/// ماژول Tax: قواعد مؤثر به تاریخ و محاسبهٔ جدا از Pricing. فاکتور و پرداخت اینجا نیستند.
/// </summary>
public sealed class TaxModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Tax";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddSingleton<IOutboxModuleRegistration, TaxOutboxRegistration>();
        // Tax is INTERNAL_ONLY (zero HTTP routes, no Endpoints project): the module-owned error
        // catalog contributor and resource set are registered by the Infrastructure composition root,
        // exactly as the certified Inventory/Pricing precedent. Both concrete types stay Contracts-owned.
        services.AddSingleton<IErrorCatalogContributor, TaxErrorCatalogContributor>();
        services.AddSingleton<IErrorResourceSet, TaxErrorResourceSet>();
        services.AddScoped<ITaxUseCaseGuard, OpenTaxUseCaseGuard>();
        services.AddScoped<ITaxDirectory, TaxDirectory>();
        services.AddScoped<ITaxCalculator>(sp => sp.GetRequiredService<ITaxDirectory>());
        services.AddScoped<ITaxQueryGateway>(sp => (TaxDirectory)sp.GetRequiredService<ITaxDirectory>());
        services.AddScoped<ITaxDevelopmentSeedGateway, TaxDevelopmentSeedGateway>();
        services.AddScoped<ITaxSchemaMigrator, TaxSchemaMigrator>();
        services.AddModuleSchemaMigrator("Tax", ModuleSchemaMigrationOrder.Tax, (sp, ct) => sp.GetRequiredService<ITaxSchemaMigrator>().MigrateAsync(ct));
        services.AddDbContext<TaxDbContext>((sp, options) =>
        {
            var connectionString = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connectionString,
                TaxDbContext.Schema,
                typeof(TaxDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}
