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

        services.AddSingleton<IOutboxModuleRegistration, PaymentOutboxRegistration>();
        services.AddScoped<IPaymentUseCaseGuard, OpenPaymentUseCaseGuard>();
        services.AddScoped<IPaymentGateway, FakePaymentGateway>();
        services.AddScoped<IPaymentGateway, FakeFailingPaymentGateway>();
        services.AddScoped<IPaymentGatewayRegistry, PaymentGatewayRegistry>();
        services.AddScoped<IPaymentDirectory, PaymentDirectory>();
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
