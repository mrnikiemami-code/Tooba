using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Pricing.Application;
using Tooba.Pricing.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.Pricing.Infrastructure;

/// <summary>
/// ماژول Pricing: حقیقت مبلغ نوشته‌شده برای Offer. Product و Offer مبلغ ندارند؛ مالیات و FX اینجا محاسبه نمی‌شوند.
/// </summary>
public sealed class PricingModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Pricing";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddSingleton<IOutboxModuleRegistration, PricingOutboxRegistration>();
        services.AddScoped<IPricingUseCaseGuard, OpenPricingUseCaseGuard>();
        services.AddScoped<IPriceDirectory, PriceDirectory>();
        services.AddScoped<IPriceLookupGateway>(sp => (PriceDirectory)sp.GetRequiredService<IPriceDirectory>());
        services.AddDbContext<PricingDbContext>((sp, options) =>
        {
            var connectionString = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connectionString,
                PricingDbContext.Schema,
                typeof(PricingDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}
