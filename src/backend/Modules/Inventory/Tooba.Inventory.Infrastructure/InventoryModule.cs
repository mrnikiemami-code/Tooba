using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Inventory.Application;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.Inventory.Infrastructure;

/// <summary>
/// ماژول Inventory: حقیقت موجودی Offer در محل. Product و Offer ستون موجودی ندارند؛ Cart اینجا نیست.
/// </summary>
public sealed class InventoryModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Inventory";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddSingleton<IOutboxModuleRegistration, InventoryOutboxRegistration>();
        services.AddScoped<IInventoryUseCaseGuard, OpenInventoryUseCaseGuard>();
        services.AddScoped<IInventoryDirectory, InventoryDirectory>();
        services.AddScoped<IInventoryReturnGateway, InventoryReturnGateway>();
        services.AddScoped<IInventoryAvailabilityGateway>(sp => (InventoryDirectory)sp.GetRequiredService<IInventoryDirectory>());
        services.AddDbContext<InventoryDbContext>((sp, options) =>
        {
            var connectionString = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connectionString,
                InventoryDbContext.Schema,
                typeof(InventoryDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}
