using Tooba.Payment.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Security;
using Tooba.ModuleContracts;
using Tooba.Order.Application;
using Tooba.Order.Application.Admin.Completeness.Ports;
using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
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
using Tooba.Payment.Contracts.Checkout;
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
        services.AddScoped<Tooba.Order.Contracts.Admin.IAdminOrderDashboardMetricsPort, Admin.AdminOrderDashboardMetricsPort>();
        services.AddScoped<Tooba.Order.Contracts.Admin.IAdminSellerOrderCountPort, Admin.AdminSellerOrderCountPort>();
        services.AddScoped<Application.Admin.Detail.Ports.IAdminOrderDetailCheckoutStore, Admin.Detail.AdminOrderDetailCheckoutStore>();
        services.AddScoped<Application.Admin.Detail.AdminOrderDetailComposer>();
        services.AddScoped<Application.Customer.Ports.ICustomerOrderCheckoutStore, CustomerOrderCheckoutStore>();
        services.AddScoped<Application.Customer.CustomerOrderComposer>();
        services.AddScoped<Tooba.Order.Contracts.Customer.ICustomerOrderDashboardSummaryPort, Adapters.CustomerOrderDashboardSummaryAdapter>();
        services.AddScoped<Application.Seller.Ports.ISellerOrderStore, SellerOrderStore>();
        services.AddScoped<Application.Seller.Ports.ISellerOrderViewAccessReader, SellerOrderViewAccessReader>();
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

        // TB-SHIPPING-GROUP-GOODS-QUOTE-FACTS-217: همان نمونهٔ scoped دایرکتوری، درز فقط-خواندنی
        // فکت مالی گروه ارسال را هم می‌دهد؛ DbContext/نمونهٔ دوم ساخته نمی‌شود.
        services.AddScoped<ICheckoutShippingGroupGoodsFactsReader>(
            sp => (ICheckoutShippingGroupGoodsFactsReader)sp.GetRequiredService<ICheckoutDirectory>());
        services.AddScoped<IOrderPaymentHoldPreparationPort, Application.Checkout.Process.OrderPaymentHoldPreparationService>();
        services.AddScoped<IOrderCheckoutReservationSetReader, OrderCheckoutReservationSetReader>();
        services.AddScoped<IOrderPaymentFreshPayabilityPort, Application.Checkout.Process.OrderPaymentFreshPayabilityService>();
        services.AddScoped<ICheckoutAbuseGate, CheckoutAbuseGate>();
        services.AddScoped<ICheckoutProcessTracker, CheckoutProcessTracker>();
        services.AddScoped<IReservationCycleDirectory, ReservationCycleDirectory>();
        services.AddScoped<IReservationCycleCheckoutLineSource, ReservationCycleCheckoutLineSource>();
        services.AddScoped<IReservationCyclePolicyResolver, ReservationCyclePolicyResolver>();
        services.AddScoped<Tooba.Order.Contracts.Reservation.IReservationCyclePolicyPreviewPort, ReservationCyclePolicyPreviewPort>();
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
        services.AddScoped<Tooba.Order.Contracts.Settlement.IOrderShippingGroupSettlementReader, Integrations.Settlement.OrderShippingGroupSettlementBridge>();
        services.AddScoped<Tooba.Order.Contracts.Settlement.IOrderCheckoutSalesChannelSettlementReader, Integrations.Settlement.OrderCheckoutSalesChannelSettlementBridge>();
        services.AddScoped<IOrderNotificationReader, OrderNotificationBridge>();
        services.AddScoped<ICheckoutPaymentAccessReader, CheckoutPaymentAccessBridge>();
        services.AddScoped<Tooba.Order.Contracts.Payments.ICartPaymentQuoteReader, Integrations.Payment.CartPaymentQuoteBridge>();
        services.AddScoped<IPaymentAdminOrderEnrichmentReader, PaymentAdminOrderEnrichmentBridge>();
        services.AddScoped<IOrderUnpaidRetrySupplyPort, OrderUnpaidRetrySupplyBridge>();
        services.AddScoped<IAdminOrderFulfillmentCheckoutReader, AdminOrderFulfillmentCheckoutReader>();
        services.AddScoped<IAdminOrderFulfillmentPermissionGate, AdminOrderFulfillmentPermissionGate>();
        services.AddScoped<IAdminOrderFulfillmentOperations, AdminOrderFulfillmentOperations>();
        services.AddScoped<Application.Storefront.Ports.IStorefrontShippingDraftStore, Storefront.StorefrontShippingDraftStore>();
        services.AddScoped<Application.Storefront.Ports.IStorefrontPendingCheckoutStore, Storefront.StorefrontPendingCheckoutStore>();
        services.AddScoped<Application.Storefront.Services.StorefrontCheckoutService>();
        services.AddScoped<Application.Storefront.Services.StorefrontShippingService>();

        // TB-SHIPPING-CAPTURED-GROUP-PROJECTION-COMPOSER-218: یک سرویس فقط-خواندنی که فکت مالی
        // واقعی گروه (Order) را با قیمت بازبینی capture‌شده (Contracts مالک Shipping) و تقویم دریافت
        // (213) ترکیب می‌کند. هیچ مسیر عمومی/فعال‌سازی/انتخاب بازبینی نمی‌سازد.
        services.AddScoped<Application.Storefront.Shipping.CapturedGroupProjection.StorefrontCapturedGroupProjectionComposer>();

        // TB-SHIPPING-STOREFRONT-GROUP-CARDS-CONNECTION-229: مسیر خواندنی واقعی «سبد → انتساب Offer →
        // انتخاب ۲۱۹ → یک ترکیب ۲۱۸». کلید پیش‌فرض خاموش است؛ خاموش یعنی صفر فراخوانی و همان پاسخ قبلی،
        // و انتخاب/ثبت هم فقط در حالت روشن صریحاً رد می‌شود. هیچ مالک/پیش‌نویس/بازبینی/مبلغی از مرورگر
        // پذیرفته نمی‌شود.
        services.Configure<Application.Storefront.Shipping.CapturedPreview.StorefrontCapturedPreviewOptions>(
            configuration.GetSection(
                Application.Storefront.Shipping.CapturedPreview.StorefrontCapturedPreviewOptions.SectionName));
        services.AddScoped<Application.Storefront.Shipping.CapturedPreview.StorefrontCapturedPreviewPlanner>();

        // TB-SHIPPING-CAPTURED-SELECTION-ATOMIC-WRITER-232: نویسندهٔ اتمیک انتخاب capture‌شده. کلید
        // نوشتن جدا و پیش‌فرض خاموش است؛ خاموش یعنی قصد capture صریحاً رد می‌شود (بدون fallback قدیمی).
        services.AddScoped(
            sp => new Application.Storefront.Shipping.CapturedPreview.StorefrontCapturedSelectionWriter(
                sp.GetRequiredService<
                    Application.Storefront.Shipping.CapturedPreview.StorefrontCapturedPreviewPlanner>(),
                sp.GetRequiredService<Application.Storefront.Ports.IStorefrontShippingDraftStore>(),
                sp.GetService<Tooba.Geography.Contracts.Places.IGeographyLookup>(),
                sp.GetRequiredService<Tooba.BuildingBlocks.IClock>()));

        // TB-SHIPPING-STOREFRONT-STRICT-RATE-CUTOVER-163: درز «نرخ مشتریِ مالک‌شدهٔ Shipping» با کلید
        // مستقل و پیش‌فرض خاموش (نبود بخش تنظیمات = false). خاموش یعنی صفر فراخوانی به Shipping و همان
        // قیمت/خطای قدیمی؛ روشن یعنی فقط نرخ authoritative گزینهٔ دقیق قیمت می‌دهد.
        services.Configure<Application.Storefront.Shipping.OwnedRate.StorefrontShippingOwnedRateOptions>(
            configuration.GetSection(
                Application.Storefront.Shipping.OwnedRate.StorefrontShippingOwnedRateOptions.SectionName));
        services.AddScoped(
            sp => new Application.Storefront.Shipping.OwnedRate.StorefrontShippingOwnedRateResolver(
                sp.GetService<Tooba.Shipping.Contracts.Rates.IShippingRateQuoteLookup>(),
                sp.GetService<Tooba.Geography.Contracts.Places.IGeographyLookup>(),
                sp.GetService<Microsoft.Extensions.Options.IOptions<
                    Application.Storefront.Shipping.OwnedRate.StorefrontShippingOwnedRateOptions>>()));

        // مشاهدهٔ سایهٔ نرخ ارسال: دروازهٔ پیش‌فرض خاموش است (appsettings هیچ بخشی ندارد، پس مقادیر
        // امن پیش‌فرض اعمال می‌شود) و اتصال فقط برای مشاهده است؛ هیچ قیمت/انتخاب/draft/snapshot تغییر نمی‌کند.
        services.Configure<Application.Storefront.Shipping.RateAuthority.StorefrontShippingRateAuthorityOptions>(
            configuration.GetSection(
                Application.Storefront.Shipping.RateAuthority.StorefrontShippingRateAuthorityOptions.SectionName));
        services.AddScoped(
            sp => new Application.Storefront.Shipping.RateAuthority.StorefrontShippingRateShadowEvaluator(
                sp.GetService<Tooba.Shipping.Contracts.Rates.IShippingRateQuoteLookup>()));
        services.AddScoped(
            sp => new Application.Storefront.Shipping.RateAuthority.StorefrontShippingShadowObserver(
                sp.GetService<Application.Storefront.Shipping.RateAuthority.StorefrontShippingRateShadowEvaluator>(),
                sp.GetService<Tooba.Geography.Contracts.Places.IGeographyLookup>(),
                sp.GetService<Microsoft.Extensions.Options.IOptions<
                    Application.Storefront.Shipping.RateAuthority.StorefrontShippingRateAuthorityOptions>>(),
                sp.GetService<Microsoft.Extensions.Logging.ILogger<
                    Application.Storefront.Shipping.RateAuthority.StorefrontShippingShadowObserver>>()));

        // مشاهدهٔ سایهٔ عملیاتی: درز <b>جدا</b> با دروازهٔ مستقل و پیش‌فرض خاموش، چون این درز ارزیابی
        // کامل (بسته‌بندی/تفکیک پرداخت/بازهٔ حامل/ظرفیت) را می‌بیند و روشن‌کردن سایهٔ ساده نباید
        // ناخواسته این ادعای تازه را هم فعال کند. اتصال فقط برای مشاهده است.
        services.Configure<Application.Storefront.Shipping.OperationalShadow.StorefrontShippingOperationalShadowOptions>(
            configuration.GetSection(
                Application.Storefront.Shipping.OperationalShadow.StorefrontShippingOperationalShadowOptions.SectionName));
        services.AddScoped(
            sp => new Application.Storefront.Shipping.OperationalShadow.StorefrontShippingOperationalShadowObserver(
                sp.GetService<Tooba.Shipping.Contracts.Operations.IShippingOperationalQuoteLookup>(),
                sp.GetService<Tooba.Geography.Contracts.Places.IGeographyLookup>(),
                sp.GetService<Microsoft.Extensions.Options.IOptions<
                    Application.Storefront.Shipping.OperationalShadow.StorefrontShippingOperationalShadowOptions>>(),
                sp.GetService<Microsoft.Extensions.Logging.ILogger<
                    Application.Storefront.Shipping.OperationalShadow.StorefrontShippingOperationalShadowObserver>>()));

        services.AddScoped<Application.Storefront.Services.StorefrontPendingPaymentService>();
        services.AddScoped<Application.Storefront.Services.StorefrontStartPaymentComposer>();
        services.AddScoped<IIntegrationEventHandler<PaymentSucceededIntegrationEvent>, OrderPaymentSucceededHandler>();
        services.AddModuleSchemaMigrator<OrderDbContext>("Order", ModuleSchemaMigrationOrder.Order);
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
