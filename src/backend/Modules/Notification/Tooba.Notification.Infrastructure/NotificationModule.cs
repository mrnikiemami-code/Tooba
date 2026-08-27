using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application;
using Tooba.ModuleContracts;
using Tooba.Notification.Application;
using Tooba.Notification.Infrastructure.Persistence;
using Tooba.Payment.Application;
using Tooba.Persistence;
using Tooba.Returns.Application;

namespace Tooba.Notification.Infrastructure;

/// <summary>
/// ماژول Notification: inbox پایدار مشتری/فروشنده از رویدادهای تجاری. سفارش و پرداخت را نگه نمی‌دارد.
/// </summary>
public sealed class NotificationModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Notification";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddSingleton<NotificationInstrumentation>();
        services.AddSingleton<IOutboxModuleRegistration, NotificationOutboxRegistration>();
        services.AddScoped<NotificationDirectory>();
        services.AddScoped<INotificationDirectory>(sp => sp.GetRequiredService<NotificationDirectory>());
        services.AddScoped<NotificationProjector>();

        services.AddScoped<IIntegrationEventHandler<PaymentSucceededIntegrationEvent>, NotificationPaymentSucceededHandler>();
        services.AddScoped<IIntegrationEventHandler<PaymentFailedIntegrationEvent>, NotificationPaymentFailedHandler>();
        services.AddScoped<IIntegrationEventHandler<FulfillmentCreatedIntegrationEvent>, NotificationFulfillmentCreatedHandler>();
        services.AddScoped<IIntegrationEventHandler<ShipmentDispatchedIntegrationEvent>, NotificationShipmentDispatchedHandler>();
        services.AddScoped<IIntegrationEventHandler<ReturnRequestedIntegrationEvent>, NotificationReturnRequestedHandler>();
        services.AddScoped<IIntegrationEventHandler<ReturnApprovedIntegrationEvent>, NotificationReturnApprovedHandler>();
        services.AddScoped<IIntegrationEventHandler<RefundSucceededIntegrationEvent>, NotificationRefundSucceededHandler>();

        services.AddDbContext<NotificationDbContext>((sp, options) =>
        {
            var connectionString = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connectionString,
                NotificationDbContext.Schema,
                typeof(NotificationDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}
