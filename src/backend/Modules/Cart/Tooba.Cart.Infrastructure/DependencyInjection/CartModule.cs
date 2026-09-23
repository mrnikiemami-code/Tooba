using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Cart.Contracts;
using Tooba.Cart.Application;
using Tooba.Cart.Application.Conversion;
using Tooba.Cart.Application.Lifetime;
using Tooba.Cart.Application.Ports;
using Tooba.Cart.Application.Presentation;
using Tooba.Cart.Infrastructure.Lifetime;
using Tooba.Cart.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.Cart.Infrastructure.DependencyInjection;

/// <summary>
/// ماژول Cart: سبد Offerمحور. سفارش، پرداخت، موجودی و منبع حقیقت قیمت اینجا نیستند.
/// عمر/انقضای سبد، سیاست ماندگاری و کارگر پس‌زمینه مالکیت Cart است، نه Host.
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

        services.Configure<CartLifetimeOptions>(configuration.GetSection(CartLifetimeOptions.SectionName));
        services.Configure<CartExpiryOptions>(configuration.GetSection(CartExpiryOptions.SectionName));
        services.AddSingleton<IOutboxModuleRegistration, CartOutboxRegistration>();
        services.AddScoped<ICartUseCaseGuard, OpenCartUseCaseGuard>();
        services.AddScoped<ICartDirectory, CartDirectory>();
        services.AddScoped<ICartConversionPort, CartConversionAdapter>();
        services.AddScoped<ICartQueryGateway>(sp => (CartDirectory)sp.GetRequiredService<ICartDirectory>());
        services.AddScoped<ICartExpiryReconciler, CartExpiryReconciler>();
        services.AddScoped<ICartPersistenceHoursResolver, CatalogCartPersistenceHoursResolver>();
        services.AddScoped<ICartPersistenceHoursSource, CartPersistenceHoursSource>();
        services.AddScoped<ICartCommerceContextResolver, CartCommerceContextResolver>();
        services.AddScoped<CartPresentationComposer>();
        services.AddScoped<Tooba.Cart.Contracts.ICartPresentationGateway>(sp => sp.GetRequiredService<CartPresentationComposer>());
        services.AddHostedService<CartExpiryWorker>();
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
