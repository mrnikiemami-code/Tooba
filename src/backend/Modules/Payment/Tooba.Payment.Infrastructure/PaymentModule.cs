using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Payment.Application;
using Tooba.Payment.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.Payment.Infrastructure;

/// <summary>
/// ماژول Payment: درگاه انتزاعی و تلاش تأییدشده. سفارش و ذخیره کارت اینجا نیستند.
/// </summary>
public sealed class PaymentModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Payment";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.Configure<PaymentGatewayOptions>(configuration.GetSection(PaymentGatewayOptions.SectionName));
        services.AddSingleton<PaymentGatewayInstrumentation>();
        services.AddSingleton<IOutboxModuleRegistration, PaymentOutboxRegistration>();
        services.AddScoped<IPaymentUseCaseGuard, OpenPaymentUseCaseGuard>();
        services.AddScoped<IPaymentGatewayRegistry, PaymentGatewayRegistry>();
        services.AddScoped<IPaymentDirectory, PaymentDirectory>();
        services.AddScoped<IPaymentReconciliationDirectory>(sp => (PaymentDirectory)sp.GetRequiredService<IPaymentDirectory>());
        services.AddScoped<IPaymentWebhookHandler, PaymentWebhookHandler>();

        if (environment.IsProduction())
        {
            var mode = configuration.GetSection(PaymentGatewayOptions.SectionName).GetValue<string>("Mode") ?? "Disabled";
            if (string.Equals(mode, "Webhook", StringComparison.OrdinalIgnoreCase))
            {
                services.AddHttpClient<WebhookPaymentGateway>();
                services.AddScoped<IPaymentGateway, WebhookPaymentGateway>();
            }
            else
            {
                services.AddScoped<IPaymentGateway, FailClosedPaymentGateway>();
            }
        }
        else
        {
            services.AddScoped<IPaymentGateway, FakePaymentGateway>();
            services.AddScoped<IPaymentGateway, FakeFailingPaymentGateway>();
        }

        services.AddDbContext<PaymentDbContext>((sp, options) =>
        {
            var connectionString = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connectionString,
                PaymentDbContext.Schema,
                typeof(PaymentDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}
