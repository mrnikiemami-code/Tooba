using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Order.Contracts.Payments;
using Tooba.Payment.Application.Models;
using Tooba.Payment.Contracts.Checkout;
using Tooba.Payment.Application.Ports;
using Tooba.Payment.Application.Storefront.Orchestration;
using Tooba.Payment.Contracts.Settlement;
using Tooba.Payment.Contracts.Storefront;
using Tooba.Payment.Contracts.Returns;
using Tooba.Payment.Infrastructure.Persistence;
using Tooba.Payment.Contracts.Admin;
using Tooba.Payment.Contracts.Customer;
using Tooba.Payment.Contracts.Hold;
using Tooba.Persistence;

using Tooba.Payment.Infrastructure.Adapters;
using Tooba.Payment.Infrastructure.Directories;
using Tooba.Payment.Infrastructure.Messaging;
using Tooba.Payment.Infrastructure.Providers;
using Tooba.Payment.Infrastructure.Workers;

namespace Tooba.Payment.Infrastructure.DependencyInjection;

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
        services.Configure<PaymentReconciliationOptions>(configuration.GetSection(PaymentReconciliationOptions.SectionName));
        services.AddSingleton<PaymentGatewayInstrumentation>();
        services.AddHostedService<PaymentReconciliationWorker>();
        services.AddSingleton<IOutboxModuleRegistration, PaymentOutboxRegistration>();
        services.AddScoped<IPaymentUseCaseGuard, OpenPaymentUseCaseGuard>();
        services.AddScoped<ICommerceHoldPolicySource, CommerceHoldPolicySource>();
        services.AddScoped<ICommerceHoldPolicy, CommerceHoldPolicyAdapter>();
        services.AddScoped<PaymentGatewayActorContext>();
        services.AddScoped<IPaymentGatewayRegistry, PaymentGatewayRegistry>();
        // Focused directory decomposition: each port resolves to its own focused implementation.
        // PaymentDirectory still needs the admin directory for RetryManualAfterRejectionAsync; the
        // deferred Func breaks the PaymentAdminDirectory -> IPaymentDirectory -> PaymentAdminDirectory cycle.
        services.AddScoped<PaymentDirectory>(sp => new PaymentDirectory(
            sp.GetRequiredService<PaymentDbContext>(),
            sp.GetRequiredService<IPaymentUseCaseGuard>(),
            sp.GetRequiredService<IPayableCheckoutReader>(),
            sp.GetRequiredService<IPaymentGatewayRegistry>(),
            sp.GetRequiredService<PaymentGatewayActorContext>(),
            sp.GetRequiredService<IClock>(),
            sp.GetRequiredService<IIdGenerator>(),
            () => sp.GetRequiredService<IPaymentAdminDirectory>(),
            sp.GetRequiredService<ICommerceHoldPolicy>()));
        // The single registered money-entry directory is the fail-closed fresh-payment gate wrapper:
        // the injected Order fresh-payability port is a REQUIRED dependency, so the composed application
        // cannot reach the fresh path without a provable reservation verdict.
        services.AddScoped<IPaymentDirectory>(sp => new FreshPaymentGateDirectory(
            sp.GetRequiredService<PaymentDirectory>(),
            sp.GetRequiredService<PaymentDbContext>(),
            sp.GetRequiredService<IPayableCheckoutReader>(),
            sp.GetRequiredService<IPaymentGatewayRegistry>(),
            sp.GetRequiredService<IOrderPaymentFreshPayabilityPort>()));
        services.AddScoped<IPaymentReconciliationDirectory>(sp => new PaymentReconciliationDirectory(
            sp.GetRequiredService<PaymentDbContext>(),
            sp.GetRequiredService<IPaymentDirectory>()));
        services.AddScoped<IPaymentAdminDirectory>(sp => new PaymentAdminDirectory(
            sp.GetRequiredService<PaymentDbContext>(),
            sp.GetRequiredService<IPaymentUseCaseGuard>(),
            sp.GetRequiredService<IPaymentDirectory>(),
            sp.GetRequiredService<IClock>(),
            sp.GetRequiredService<IIdGenerator>(),
            sp.GetService<IPaymentRefundGateway>()));
        services.AddScoped<IPaymentExpiryDirectory>(sp => new PaymentExpiryDirectory(
            sp.GetRequiredService<PaymentDbContext>(),
            sp.GetRequiredService<IPaymentUseCaseGuard>(),
            sp.GetRequiredService<IPayableCheckoutReader>(),
            sp.GetRequiredService<IPaymentGatewayRegistry>(),
            sp.GetRequiredService<PaymentGatewayActorContext>(),
            sp.GetRequiredService<IClock>(),
            sp.GetRequiredService<IIdGenerator>(),
            sp.GetRequiredService<ICommerceHoldPolicy>()));
        services.AddScoped<IPaymentSettlementReader, PaymentSettlementBridge>();
        services.AddScoped<IPaymentWebhookHandler, PaymentWebhookHandler>();
        services.AddScoped<IPaymentWebhookSignatureVerifier, PaymentWebhookSignatureVerifierAdapter>();
        services.AddScoped<IPaymentGatewayCatalogPort, PaymentGatewayCatalogAdapter>();
        // Pre-commit provider resolution lives with the catalog authority that owns the storefront
        // provider decision, so the caller's hold policy and the payment's concrete provider agree
        // without duplicating any provider/default/duration rule outside Payment.
        services.AddScoped<IPaymentProviderSelectionPort, PaymentProviderSelectionAdapter>();
        services.AddScoped<IPaymentHoldSettingsDirectory, PaymentHoldSettingsDirectory>();
        services.AddScoped<IPaymentQueryDirectory, PaymentQueryDirectory>();
        services.AddScoped<IPendingPaymentReader, PendingPaymentBridge>();
        services.AddScoped<PaymentContractBridge>();
        services.AddScoped<IPaymentAdminGateway>(sp => sp.GetRequiredService<PaymentContractBridge>());
        services.AddScoped<IPaymentCustomerGateway>(sp => sp.GetRequiredService<PaymentContractBridge>());
        services.AddScoped<IPaymentHoldSettingsGateway>(sp => sp.GetRequiredService<PaymentContractBridge>());
        services.AddScoped<IPaymentReturnReader, PaymentReturnBridge>();
        // Contracts-only initiation seam for the owning Order module. Delegates to the single canonical
        // StorefrontPaymentOrchestrator initiation path. Resolution is deferred through the module's
        // established Func<> pattern so module DI validation stays independent of the Host-owned
        // orchestrator registration.
        services.AddScoped<IPaymentInitiationPort>(sp => new PaymentInitiationAdapter(
            () => sp.GetRequiredService<StorefrontPaymentOrchestrator>()));

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

            // کیف پول در Production هم در دسترس است (ledger محلی؛ PSP نیست).
            services.AddScoped<IPaymentGateway, WalletPaymentGateway>();
            // کارت‌به‌کارت/دستی: ثبت درگاه؛ در دسترس بودن با ManualCardToCardEnabled کنترل می‌شود.
            services.AddScoped<IPaymentGateway, ManualPaymentGateway>();
        }
        else
        {
            services.AddScoped<IPaymentGateway, FakePaymentGateway>();
            services.AddScoped<IPaymentGateway, FakeFailingPaymentGateway>();
            services.AddScoped<IPaymentGateway, ManualPaymentGateway>();
            services.AddScoped<IPaymentGateway, WalletPaymentGateway>();
            services.AddScoped<IPaymentRefundGateway, FakePaymentRefundGateway>();
        }

        if (environment.IsProduction())
        {
            services.AddScoped<IPaymentRefundGateway, FailClosedPaymentRefundGateway>();
        }

        services.AddModuleSchemaMigrator<PaymentDbContext>("Payment", ModuleSchemaMigrationOrder.Payment);
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
