using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Payment.Application;
using Tooba.Returns.Application;
using Tooba.Settlement.Application;
using Tooba.Settlement.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.Settlement.Infrastructure;

/// <summary>
/// ماژول Settlement: accrual پس از Paid و payout marketplace. سفارش و پرداخت مستقیم اینجا نیستند.
/// </summary>
public sealed class SettlementModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Settlement";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddSingleton<SettlementInstrumentation>();
        services.AddSingleton<IOutboxModuleRegistration, SettlementOutboxRegistration>();
        services.AddScoped<ISettlementUseCaseGuard, OpenSettlementUseCaseGuard>();
        services.AddScoped<SettlementDirectory>();
        services.AddScoped<ISettlementDirectory>(sp => sp.GetRequiredService<SettlementDirectory>());
        services.AddScoped<ISettlementOrderReader, SettlementOrderBridge>();
        services.AddScoped<ISettlementPaymentReader, SettlementPaymentBridge>();
        services.AddScoped<ISettlementReturnsReader, SettlementReturnsBridge>();

        if (IsMarketplaceEdition(configuration))
        {
            services.AddScoped<IIntegrationEventHandler<PaymentSucceededIntegrationEvent>, SettlementPaymentSucceededHandler>();
            services.AddScoped<IIntegrationEventHandler<RefundSucceededIntegrationEvent>, SettlementRefundSucceededHandler>();
        }

        if (environment.IsProduction())
        {
            services.AddScoped<IPayoutGateway, FailClosedPayoutGateway>();
        }
        else
        {
            services.AddScoped<IPayoutGateway, FakePayoutGateway>();
        }

        services.AddDbContext<SettlementDbContext>((sp, options) =>
        {
            var connectionString = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connectionString,
                SettlementDbContext.Schema,
                typeof(SettlementDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }

    private static bool IsMarketplaceEdition(IConfiguration configuration)
    {
        var edition = configuration.GetSection("Tooba")["Edition"];
        return string.Equals(edition, "Marketplace", StringComparison.OrdinalIgnoreCase);
    }
}
