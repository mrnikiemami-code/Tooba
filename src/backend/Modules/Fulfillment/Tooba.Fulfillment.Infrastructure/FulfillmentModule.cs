using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Inventory.Application;
using Tooba.ModuleContracts;
using Tooba.Order.Application;
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
        services.AddScoped<IFulfillmentInventoryGateway, FulfillmentInventoryGateway>();
        services.AddScoped<IFulfillmentReturnReader, FulfillmentReturnBridge>();
        services.AddScoped<ISellerOrderCancelFulfillmentGate, FulfillmentSellerOrderCancelGate>();
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
