using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FluentValidation;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.ModuleContracts;
using Tooba.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Tooba.Catalog.Infrastructure;

/// <summary>
/// ماژول Catalog: حقیقت توصیفی محصول. قیمت، موجودی، Offer و UI تجاری اینجا نیست.
/// </summary>
public sealed class CatalogModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Catalog";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddSingleton<IQuantityNormalizer, QuantityNormalizer>();
        services.AddSingleton<IOutboxModuleRegistration, CatalogOutboxRegistration>();
        services.AddScoped<ICatalogUseCaseGuard, OpenCatalogUseCaseGuard>();
        services.AddScoped<ICatalogActorContext, CatalogActorContext>();
        services.AddScoped<ICatalogDirectory, CatalogDirectory>();
        services.AddScoped<ICatalogLookupGateway>(sp => (CatalogDirectory)sp.GetRequiredService<ICatalogDirectory>());
        services.AddScoped<ICatalogVariantLookup>(sp => (CatalogDirectory)sp.GetRequiredService<ICatalogDirectory>());
        services.AddScoped<ICatalogOfferReadGateway, CatalogOfferReadGateway>();
        services.AddScoped<IStoreLandingPageDirectory, StoreLandingPageDirectory>();
        services.AddScoped<IStoreMenuDirectory, StoreMenuDirectory>();
        services.AddScoped<IStoreAppearanceSettingsDirectory, StoreAppearanceSettingsDirectory>();
        services.AddScoped<IStoreQuantitySettingsDirectory, StoreQuantitySettingsDirectory>();
        services.AddScoped<IUnitOfMeasureDirectory, UnitOfMeasureDirectory>();
        services.AddSingleton<IQuantityNormalizer, QuantityNormalizer>();
        services.AddValidatorsFromAssembly(typeof(CreateStoreLandingPageCommand).Assembly);
        services.AddDbContext<CatalogDbContext>((sp, options) =>
        {
            var connectionString = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connectionString,
                CatalogDbContext.Schema,
                typeof(CatalogDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}
