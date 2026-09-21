using Tooba.Inventory.Infrastructure.Messaging;
using Tooba.Inventory.Infrastructure.Adapters;
using Tooba.Inventory.Infrastructure.Directories;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Inventory.Contracts.Checkout;
using Tooba.Inventory.Contracts.Availability;
using Tooba.Inventory.Contracts.Returns;
using Tooba.Inventory.Contracts.Fulfillment;
using Tooba.Inventory.Application.Orders;
using Tooba.Inventory.Application.Checkout;
using Tooba.Inventory.Application.Ports;
﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Inventory.Contracts.Errors;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.Inventory.Infrastructure.DependencyInjection;

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
        services.AddScoped<ISellerOfferInventoryGateway>(sp => (InventoryDirectory)sp.GetRequiredService<IInventoryDirectory>());
        services.AddScoped<IFulfillmentInventoryLifecyclePort>(sp => (InventoryDirectory)sp.GetRequiredService<IInventoryDirectory>());
        services.AddScoped<IInventoryQueryGateway, Adapters.InventoryQueryGateway>();
        services.AddScoped<IInventorySchemaMigrator, Adapters.InventorySchemaMigrator>();
        services.AddScoped<ICheckoutInventoryReservationPort, CheckoutInventoryReservationAdapter>();
        services.AddScoped<IOrderInventoryLifecyclePort, OrderInventoryLifecycleAdapter>();
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
