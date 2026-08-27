using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Order.Application;
using Tooba.Payment.Application;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.Order.Infrastructure;

/// <summary>
/// ماژول Order: checkout و سفارش فروشنده‌محور. سبد، پرداخت و ارسال اینجا نیستند.
/// </summary>
public sealed class OrderModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Order";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddSingleton<IOutboxModuleRegistration, OrderOutboxRegistration>();
        services.AddScoped<IOrderUseCaseGuard, OpenOrderUseCaseGuard>();
        services.AddScoped<ICheckoutDirectory, CheckoutDirectory>();
        services.AddScoped<IOrderPurchaseVerificationGateway, OrderPurchaseVerificationGateway>();
        services.AddScoped<IPayableCheckoutReader, OrderPaymentBridge>();
        services.AddScoped<IOrderPaymentProjection, OrderPaymentBridge>();
        services.AddScoped<IOrderFulfillmentReader, OrderFulfillmentBridge>();
        services.AddScoped<IOrderReturnReader, OrderReturnBridge>();
        services.AddScoped<IOrderNotificationReader, OrderNotificationBridge>();
        services.AddScoped<IIntegrationEventHandler<PaymentSucceededIntegrationEvent>, OrderPaymentSucceededHandler>();
        services.AddDbContext<OrderDbContext>((sp, options) =>
        {
            var connectionString = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connectionString,
                OrderDbContext.Schema,
                typeof(OrderDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}
