using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Inventory.Application;
using Tooba.ModuleContracts;
using Tooba.Payment.Application;
using Tooba.Persistence;

namespace Tooba.Fulfillment.Infrastructure;

/// <summary>
/// ماژول Fulfillment: ارکستراسیون ارسال پس از Paid. سفارش و موجودی مستقیم اینجا نیستند.
/// </summary>
public sealed class FulfillmentModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Fulfillment";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddSingleton<FulfillmentInstrumentation>();
        services.AddSingleton<IOutboxModuleRegistration, FulfillmentOutboxRegistration>();
        services.AddScoped<IFulfillmentUseCaseGuard, OpenFulfillmentUseCaseGuard>();
        services.AddScoped<FulfillmentDirectory>();
        services.AddScoped<IFulfillmentDirectory>(sp => sp.GetRequiredService<FulfillmentDirectory>());
        services.AddScoped<IFulfillmentInventoryGateway, FulfillmentInventoryGateway>();
        services.AddScoped<IIntegrationEventHandler<PaymentSucceededIntegrationEvent>, FulfillmentPaymentSucceededHandler>();
        services.AddDbContext<FulfillmentDbContext>((sp, options) =>
        {
            var connectionString = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connectionString,
                FulfillmentDbContext.Schema,
                typeof(FulfillmentDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}
