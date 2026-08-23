using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Cart.Application;
using Tooba.Cart.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.Cart.Infrastructure;

/// <summary>
/// ماژول Cart: سبد Offerمحور. سفارش، پرداخت، موجودی و منبع حقیقت قیمت اینجا نیستند.
/// </summary>
public sealed class CartModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Cart";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddSingleton<IOutboxModuleRegistration, CartOutboxRegistration>();
        services.AddScoped<ICartUseCaseGuard, OpenCartUseCaseGuard>();
        services.AddScoped<ICartDirectory, CartDirectory>();
        services.AddScoped<ICartQueryGateway>(sp => (CartDirectory)sp.GetRequiredService<ICartDirectory>());
        services.AddDbContext<CartDbContext>((sp, options) =>
        {
            var connectionString = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connectionString,
                CartDbContext.Schema,
                typeof(CartDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}
