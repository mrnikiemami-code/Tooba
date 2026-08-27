using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Returns.Application;
using Tooba.Returns.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.Returns.Infrastructure;

/// <summary>
/// ماژول Returns: ارکستراسیون مرجوعی پس از تحویل. سفارش و پرداخت مستقیم اینجا نیستند.
/// </summary>
public sealed class ReturnsModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Returns";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddSingleton<ReturnsInstrumentation>();
        services.AddSingleton<IOutboxModuleRegistration, ReturnsOutboxRegistration>();
        services.AddScoped<IReturnUseCaseGuard, OpenReturnUseCaseGuard>();
        services.AddScoped<ReturnDirectory>();
        services.AddScoped<IReturnDirectory>(sp => sp.GetRequiredService<ReturnDirectory>());
        services.AddScoped<IReturnInventoryGateway, ReturnInventoryGateway>();
        services.AddScoped<IReturnSettlementReader, ReturnSettlementBridge>();
        services.AddDbContext<ReturnsDbContext>((sp, options) =>
        {
            var connectionString = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connectionString,
                ReturnsDbContext.Schema,
                typeof(ReturnsDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}
