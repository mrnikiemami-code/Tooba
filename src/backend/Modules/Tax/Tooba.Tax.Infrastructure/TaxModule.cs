using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Persistence;
using Tooba.Tax.Application;
using Tooba.Tax.Infrastructure.Persistence;

namespace Tooba.Tax.Infrastructure;

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
        services.AddScoped<ITaxUseCaseGuard, OpenTaxUseCaseGuard>();
        services.AddScoped<ITaxDirectory, TaxDirectory>();
        services.AddScoped<ITaxCalculator>(sp => sp.GetRequiredService<ITaxDirectory>());
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
