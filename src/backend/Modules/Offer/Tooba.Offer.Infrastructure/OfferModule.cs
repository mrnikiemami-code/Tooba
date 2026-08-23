using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Offer.Application;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.Offer.Infrastructure;

/// <summary>
/// ماژول Offer: listing تجاری فروشنده روی Variant. قیمت، موجودی، Catalog persistence و UI فروشنده اینجا نیست.
/// Single-Store هم از همین Offer استفاده می‌کند و Price را روی Product نمی‌گذارد.
/// </summary>
public sealed class OfferModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Offer";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddSingleton<IOutboxModuleRegistration, OfferOutboxRegistration>();
        services.AddScoped<IOfferUseCaseGuard, OpenOfferUseCaseGuard>();
        services.AddScoped<IOfferDirectory, OfferDirectory>();
        services.AddScoped<IOfferLookupGateway>(sp => (OfferDirectory)sp.GetRequiredService<IOfferDirectory>());
        services.AddDbContext<OfferDbContext>((sp, options) =>
        {
            var connectionString = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connectionString,
                OfferDbContext.Schema,
                typeof(OfferDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}
