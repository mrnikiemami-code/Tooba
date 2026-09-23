using Tooba.Payment.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Order.Application;
using Tooba.Order.Application.Admin.Completeness.Ports;
using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.PurchaseVerification;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Contracts.Notifications;
using Tooba.Order.Contracts.Payments;
using Tooba.Order.Contracts.Returns;
using Tooba.Order.Infrastructure.Admin;
using Tooba.Order.Infrastructure.Admin.Fulfillment;
using Tooba.Order.Infrastructure.Checkout.Abuse;
using Tooba.Order.Infrastructure.Checkout.Persistence;
using Tooba.Order.Infrastructure.Customer;
using Tooba.Order.Infrastructure.Events.Payment;
using Tooba.Order.Infrastructure.Guards;
using Tooba.Order.Infrastructure.Integrations.Fulfillment;
using Tooba.Order.Infrastructure.Integrations.GridEnrichment;
using Tooba.Order.Infrastructure.Integrations.Notifications;
using Tooba.Order.Infrastructure.Integrations.Payment;
using Tooba.Order.Infrastructure.Integrations.Returns;
using Tooba.Order.Infrastructure.Messaging;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Order.Infrastructure.PurchaseVerification;
using Tooba.Order.Infrastructure.ReservationCycle;
using Tooba.Order.Infrastructure.Seller;
using Tooba.Payment.Application.Models;
using Tooba.Payment.Application.Ports;
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
        services.AddScoped<IAdminOrderCompletenessStore, AdminOrderCompletenessStore>();
        services.AddScoped<Application.Admin.OrdersGrid.Ports.IAdminOrdersGridReader, Admin.OrdersGrid.AdminOrdersGridReader>();
        services.AddScoped<Application.Admin.LegacyList.Ports.IAdminOrderListStore, Admin.LegacyList.AdminOrderListStore>();
        services.AddScoped<Application.Admin.Customers.Ports.IAdminCustomersGridReader, Admin.Customers.AdminCustomersGridReader>();
        services.AddScoped<Application.Admin.Dashboard.Ports.IAdminOrderDashboardMetricsStore, Admin.Dashboard.AdminOrderDashboardMetricsStore>();
        services.AddScoped<Application.Admin.Sellers.Ports.ISellerOrderCountReader, Admin.Sellers.SellerOrderCountReader>();
        services.AddScoped<Application.Admin.Detail.Ports.IAdminOrderDetailCheckoutStore, Admin.Detail.AdminOrderDetailCheckoutStore>();
        services.AddScoped<Application.Admin.Detail.AdminOrderDetailComposer>();
        services.AddScoped<Application.Customer.Ports.ICustomerOrderCheckoutStore, CustomerOrderCheckoutStore>();
        services.AddScoped<Application.Customer.CustomerOrderComposer>();
        services.AddScoped<Application.Seller.Ports.ISellerOrderStore, SellerOrderStore>();
        services.AddScoped<Application.Seller.SellerOrderComposer>();
        services.AddScoped<Application.Admin.Operations.Ports.IAdminOrderOperationsCheckoutReader, Admin.Operations.AdminOrderOperationsCheckoutReader>();
        services.AddScoped<Application.Admin.Supply.Ports.IOrderSupplyCheckoutStore, Admin.Supply.OrderSupplyCheckoutStore>();
        services.AddScoped<Application.Admin.Supply.Services.OrderSupplyService>();
        services.AddScoped<Application.Admin.InventoryRecovery.Services.OrderInventoryRecoveryService>();
        services.AddScoped<Application.Admin.OrdersGrid.Ports.IAdminOrderSupplyStatusReader, Admin.OrdersGrid.AdminOrderSupplyStatusReader>();
        services.AddScoped<Application.Admin.Operations.Ports.IAdminOrderOperationsInventoryRecoveryPort, Admin.Operations.AdminOrderOperationsInventoryRecoveryAdapter>();
        services.AddScoped<Application.Admin.Operations.Ports.IAdminOrderOperationsSupplyPort, Admin.Operations.AdminOrderOperationsSupplyAdapter>();
        services.AddScoped<Application.Admin.Operations.Services.AdminOrderOperationsOrchestrator>();
        services.AddScoped<ICheckoutDirectory, CheckoutDirectory>();
        services.AddScoped<ICheckoutAbuseGate, CheckoutAbuseGate>();
        services.AddScoped<ICheckoutProcessTracker, CheckoutProcessTracker>();
        services.AddScoped<IReservationCycleDirectory, ReservationCycleDirectory>();
        services.AddScoped<IReservationCycleCheckoutLineSource, ReservationCycleCheckoutLineSource>();
        services.AddScoped<IReservationCyclePolicyResolver, ReservationCyclePolicyResolver>();
        services.AddScoped<IReservationCycleCoordinator, ReservationCycleCoordinator>();
        services.AddScoped<IUnpaidOrderExpiryReconciler, UnpaidOrderExpiryReconciler>();
        services.Configure<ReservationCycleOptions>(
            configuration.GetSection(ReservationCycleOptions.SectionName));
        services.AddScoped<IOrderPurchaseVerificationGateway, OrderPurchaseVerificationGateway>();
        services.AddScoped<IPayableCheckoutReader, OrderPaymentBridge>();
        services.AddScoped<IOrderPaymentProjection, OrderPaymentBridge>();
        services.AddScoped<IOrderPaymentProjectionPort, OrderPaymentBridge>();
        services.AddScoped<IOrderFulfillmentReader, OrderFulfillmentBridge>();
        services.AddScoped<ISellerOrderAuthReader, SellerOrderAuthBridge>();
        services.AddScoped<ICustomerCheckoutOwnershipReader, CustomerCheckoutOwnershipBridge>();
        services.AddScoped<IOrderGridEnrichmentReader, OrderGridEnrichmentBridge>();
        services.AddScoped<IOrderReturnReader, OrderReturnBridge>();
        services.AddScoped<IOrderNotificationReader, OrderNotificationBridge>();
        services.AddScoped<ICheckoutPaymentAccessReader, CheckoutPaymentAccessBridge>();
        services.AddScoped<IPaymentAdminOrderEnrichmentReader, PaymentAdminOrderEnrichmentBridge>();
        services.AddScoped<IOrderUnpaidRetrySupplyPort, OrderUnpaidRetrySupplyBridge>();
        services.AddScoped<IAdminOrderFulfillmentCheckoutReader, AdminOrderFulfillmentCheckoutReader>();
        services.AddScoped<IAdminOrderFulfillmentPermissionGate, AdminOrderFulfillmentPermissionGate>();
        services.AddScoped<IAdminOrderFulfillmentOperations, AdminOrderFulfillmentOperations>();
        services.AddScoped<Application.Storefront.Ports.IStorefrontShippingDraftStore, Storefront.StorefrontShippingDraftStore>();
        services.AddScoped<Application.Storefront.Ports.IStorefrontPendingCheckoutStore, Storefront.StorefrontPendingCheckoutStore>();
        services.AddScoped<Application.Storefront.Services.StorefrontCheckoutService>();
        services.AddScoped<Application.Storefront.Services.StorefrontShippingService>();
        services.AddScoped<Application.Storefront.Services.StorefrontPendingPaymentService>();
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
