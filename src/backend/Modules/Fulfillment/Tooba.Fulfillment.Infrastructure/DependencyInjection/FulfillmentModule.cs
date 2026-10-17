using Tooba.Payment.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Fulfillment.Application.Fulfillments.Ports;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Application.Shipping.Ports;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Fulfillment.Infrastructure.Directories;
using Tooba.Fulfillment.Infrastructure.Gateways;
using Tooba.Fulfillment.Infrastructure.Bridges;
using Tooba.Fulfillment.Infrastructure.Handlers;
using Tooba.Fulfillment.Infrastructure.Shipping;
using Tooba.Fulfillment.Infrastructure.Messaging;
using Tooba.Fulfillment.Infrastructure.Observability;
using Tooba.ModuleContracts;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Fulfillment.Contracts.Returns;
using Tooba.Inventory.Contracts.Fulfillment;
using Tooba.Persistence;

namespace Tooba.Fulfillment.Infrastructure.DependencyInjection;

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
        services.AddSingleton(_ =>
        {
            var section = configuration.GetSection(ShippingMethodsOptions.SectionName);
            var codes = section.GetSection("EnabledCodes").GetChildren()
                .Select(x => x.Value)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .ToArray();
            var rates = section.GetSection("Rates").GetChildren()
                .Select(r => new ShippingMethodRateOptions
                {
                    Code = r["Code"] ?? string.Empty,
                    BasePrice = decimal.TryParse(r["BasePrice"], out var price) ? price : 0m,
                    LeadDays = int.TryParse(r["LeadDays"], out var lead) ? lead : 1,
                    FreeAboveSubtotal = decimal.TryParse(r["FreeAboveSubtotal"], out var free) ? free : null,
                    AllowedProvinces = r.GetSection("AllowedProvinces").GetChildren()
                        .Select(p => p.Value)
                        .Where(v => !string.IsNullOrWhiteSpace(v))
                        .Select(v => v!)
                        .ToArray(),
                })
                .Where(r => !string.IsNullOrWhiteSpace(r.Code))
                .ToArray();
            var sellerPrep = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var child in section.GetSection("SellerPreparationDaysByPartyId").GetChildren())
            {
                if (!string.IsNullOrWhiteSpace(child.Key) && int.TryParse(child.Value, out var days))
                {
                    sellerPrep[child.Key] = days;
                }
            }

            var options = new ShippingMethodsOptions
            {
                EnabledCodes = codes.Length > 0
                    ? codes
                    : ["post", "tipax", "snapp_courier", "store_courier", "in_person"],
                DefaultSellerPreparationDays = int.TryParse(section["DefaultSellerPreparationDays"], out var prep)
                    ? prep
                    : 1,
                DeliveryHorizonDays = int.TryParse(section["DeliveryHorizonDays"], out var horizon)
                    ? horizon
                    : 7,
                SellerPreparationDaysByPartyId = sellerPrep,
            };
            if (rates.Length > 0)
            {
                options.Rates = rates;
            }

            return options;
        });
        services.AddScoped<IFulfillmentUseCaseGuard, OpenFulfillmentUseCaseGuard>();
        services.AddScoped<FulfillmentDirectory>();
        services.AddScoped<IFulfillmentDirectory>(sp => sp.GetRequiredService<FulfillmentDirectory>());
        services.AddScoped<Contracts.History.IFulfillmentHistoryReader, Adapters.FulfillmentHistoryReader>();
        services.AddScoped<Contracts.Operations.IFulfillmentAdminOperations, Adapters.FulfillmentAdminOperationsAdapter>();
        services.AddScoped<ISellerFulfillmentAuthorizer, SellerFulfillmentAuthorizer>();
        services.AddScoped<IFulfillmentShippedQuantityReader, FulfillmentShippedQuantityReader>();
        services.AddScoped<IAdminFulfillmentWorkQueueQuery, Queries.AdminFulfillmentWorkQueueQueryEngine>();
        services.AddScoped<IShippingServiceDirectory, ShippingServiceDirectory>();
        services.AddScoped<IShippingServiceLanguageGate, ShippingServiceLanguageGate>();
        services.AddScoped<IShippingCatalogReader, ShippingCatalogReader>();
        services.AddScoped<IShippingOneLevelCatalogReader, ShippingOneLevelCatalogReader>();
        services.AddScoped<IShippingOneLevelCatalogAdminRead, ShippingOneLevelCatalogAdminRead>();
        // TB-SHIPPING-ONE-LEVEL-TABS-100: درز فقط-خواندنی برچسب فروشگاه؛ دروازهٔ خاموش ⇒ نگاشت خالی و برچسب قدیمی.
        services.AddScoped<IShippingOneLevelStorefrontLabelReader, ShippingOneLevelStorefrontLabelRead>();
        // TB-SHIPPING-NEW-METHOD-ACTIVATION-170: هویت فقط-خواندنی روش‌های canonical بدون ردیف گزینهٔ
        // قدیمی؛ دروازهٔ خاموش ⇒ فهرست خالی و هیچ پرس‌وجویی برای مصرف‌کننده‌های نرخ/فروشگاه.
        services.AddScoped<IShippingOneLevelCanonicalMethodReader, ShippingOneLevelCanonicalMethodRead>();
        // TB-SHIPPING-ONE-LEVEL-TABS-094: تنها درز نوشتن نام روش یک‌سطحی؛ دروازهٔ خاموش ⇒ هیچ نوشتنی.
        services.AddScoped<IShippingOneLevelCatalogAdminWrite, ShippingOneLevelCatalogAdminWrite>();
        // TB-SHIPPING-ADMIN-FLAT-METHOD-CREATE-167: درز ایجاد روش canonical مستقل؛ پیوند قدیمی فقط
        // از کد canonical مشتق می‌شود، روش تازه غیرفعال است و هیچ ردیف کاتالوگ قدیمی ساخته نمی‌شود.
        services.AddScoped<IShippingOneLevelCatalogAdminCreate, ShippingOneLevelCatalogAdminCreate>();
        // TB-SHIPPING-ONE-LEVEL-TABS-096: تنها مرجع تراکنشی فعال‌سازی روش گزینه‌دار؛ ردیف قدیمی گزینه و
        // فرافکنی یک‌سطحی را با هم هم‌راستا می‌کند و دروازهٔ خاموش هیچ نوشتنی نمی‌دهد.
        services.AddScoped<IShippingOneLevelCatalogAdminActivation, ShippingOneLevelCatalogActivation>();
        services.AddScoped<IShippingOneLevelBackfillStore, ShippingOneLevelBackfillStore>();
        services.AddScoped<IShippingCatalogSeedPort, ShippingCatalogSeedAdapter>();
        // دروازهٔ کاتالوگ یک‌سطحی: پیش‌فرض خاموش است و هیچ بخشی در appsettings ندارد. این دروازه فقط
        // مسیر خواندن کاتالوگ/UI آینده را کنترل می‌کند و به Tooba:ShippingRateAuthority یا
        // Tooba:ShippingOperationalShadow وصل نیست و آن‌ها را تغییر نمی‌دهد.
        services.Configure<ShippingOneLevelCatalogOptions>(
            configuration.GetSection(ShippingOneLevelCatalogOptions.SectionName));
        services.AddSingleton<IErrorCatalogContributor, Errors.FulfillmentErrorCatalogContributor>();
        services.AddScoped<IFulfillmentInventoryGateway, FulfillmentInventoryGateway>();
        services.AddScoped<IFulfillmentReturnReader, FulfillmentReturnBridge>();
        services.AddScoped<ISellerOrderCancelFulfillmentGate, FulfillmentSellerOrderCancelGate>();
        services.AddScoped<IIntegrationEventHandler<PaymentSucceededIntegrationEvent>, FulfillmentPaymentSucceededHandler>();
        services.AddModuleSchemaMigrator<FulfillmentDbContext>("Fulfillment", ModuleSchemaMigrationOrder.Fulfillment);
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
