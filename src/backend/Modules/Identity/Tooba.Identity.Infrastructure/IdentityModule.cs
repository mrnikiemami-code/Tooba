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
        services.AddSingleton<IOutboxModuleRegistration, IdentityOutboxRegistration>();
        services.AddSingleton<IPasswordHashingService, AspNetPasswordHashingService>();
        services.AddSingleton<CapturingOtpSender>();
        services.AddSingleton<IOtpSender>(sp => sp.GetRequiredService<CapturingOtpSender>());
        services.AddSingleton<IOtpChallengeService, InMemoryOtpChallengeService>();
        services.AddSingleton<IIdentitySecurityEventSink, InMemoryIdentitySecurityEventSink>();
        services.AddScoped<IIdentityAuthenticationService, IdentityAuthenticationService>();
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
