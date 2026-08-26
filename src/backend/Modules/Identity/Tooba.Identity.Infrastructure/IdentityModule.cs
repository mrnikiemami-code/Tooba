using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.Identity.Application;
using Tooba.Identity.Infrastructure.Persistence;
using Tooba.ModuleContracts;
using Tooba.Persistence;

namespace Tooba.Identity.Infrastructure;

/// <summary>
/// ماژول Identity: احراز اصل، نه مجوز کسب‌وکار و نه Party.
/// </summary>
public sealed class IdentityModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Identity";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.Configure<IdentityPasswordPolicyOptions>(configuration.GetSection("Identity:PasswordPolicy"));
        services.Configure<IdentityLifecycleOptions>(configuration.GetSection("Identity:Lifecycle"));
        services.Configure<OtpDeliveryOptions>(configuration.GetSection("Identity:OtpDelivery"));
        services.AddSingleton<OtpDeliveryInstrumentation>();
        services.AddSingleton<IOutboxModuleRegistration, IdentityOutboxRegistration>();
        services.AddSingleton<IPasswordHashingService, AspNetPasswordHashingService>();

        if (environment.IsProduction())
        {
            var otpMode = configuration.GetSection("Identity:OtpDelivery").GetValue<string>("Mode") ?? "Disabled";
            if (string.Equals(otpMode, "Webhook", StringComparison.OrdinalIgnoreCase))
            {
                services.AddHttpClient<WebhookOtpDeliveryProvider>();
                services.AddSingleton<IOtpDeliveryProvider>(sp => sp.GetRequiredService<WebhookOtpDeliveryProvider>());
            }
            else
            {
                services.AddSingleton<IOtpDeliveryProvider, FailClosedOtpDeliveryProvider>();
            }
        }
        else
        {
            services.AddSingleton<CapturingOtpDeliveryProvider>();
            services.AddSingleton<IOtpDeliveryProvider>(sp => sp.GetRequiredService<CapturingOtpDeliveryProvider>());
            services.AddSingleton<CapturingOtpSender>();
        }

        services.AddSingleton<IOtpSender, OtpDeliveryProviderSender>();
        services.AddSingleton<IIdentitySecurityEventSink, InMemoryIdentitySecurityEventSink>();
        services.AddSingleton<IAccessCredentialBoundary, SessionAccessCredentialBoundary>();
        services.AddScoped<IdentityLifecycleService>();
        services.AddScoped<IOtpChallengeService>(sp => sp.GetRequiredService<IdentityLifecycleService>());
        services.AddScoped<IIdentityCredentialLifecycle>(sp => sp.GetRequiredService<IdentityLifecycleService>());
        services.AddScoped<IIdentitySessionResolver>(sp => sp.GetRequiredService<IdentityLifecycleService>());
        services.AddScoped<IIdentityAuthenticationService, IdentityAuthenticationService>();
        services.AddScoped<IIdentityContactLookup, EfIdentityContactLookup>();
        services.AddScoped<IExternalIdentityDirectory, EfExternalIdentityDirectory>();
        services.AddScoped<IMfaEnrollmentStore, EfMfaEnrollmentStore>();
        services.AddDbContext<IdentityDbContext>((sp, options) =>
        {
            var connectionString = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connectionString,
                IdentityDbContext.Schema,
                typeof(IdentityDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}
