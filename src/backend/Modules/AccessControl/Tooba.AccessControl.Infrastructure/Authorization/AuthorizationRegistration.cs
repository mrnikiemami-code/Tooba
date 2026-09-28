using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;

namespace Tooba.AccessControl.Infrastructure.Authorization;

/// <summary>
/// ثبت مجوز Access Control. SDK SpiceDB به Domain/Application نشت نمی‌کند.
/// </summary>
public static class AuthorizationRegistration
{
    /// <summary>
    /// قراردادهای Tooba و adapter مطابق Mode را ثبت می‌کند.
    /// </summary>
    public static IServiceCollection AddToobaAuthorization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<AuthorizationInstrumentation>();
        services.AddSingleton<IAuthorizationSchemaProvider, FoundationAuthorizationSchemaProvider>();
        services.AddSingleton<IAuthorizationSchemaBootstrapper>(sp =>
            new ConfiguredAuthorizationSchemaBootstrapper(
                sp.GetRequiredService<IOptions<SpiceDbAuthorizationOptions>>(),
                sp.GetRequiredService<IAuthorizationSchemaProvider>(),
                sp.GetRequiredService<ILogger<ConfiguredAuthorizationSchemaBootstrapper>>(),
                sp));
        services.AddSingleton<IAuthorizationSecurityEventSink, InMemoryAuthorizationSecurityEventSink>();
        services.AddSingleton<SpiceDbHealthProbe>();
        services.AddSingleton<InMemoryAuthorizationAdapter>();
        services.AddSingleton(sp =>
            new FailClosedAuthorizationAdapter("authorization.disabled", sp.GetRequiredService<AuthorizationInstrumentation>()));
        services.AddSingleton<SpiceDbAuthorizationAdapter>();
        services.AddSingleton<IAuthorizationService>(ResolveEngine);
        services.AddSingleton<IAuthorizationTupleWriter>(sp => (IAuthorizationTupleWriter)sp.GetRequiredService<IAuthorizationService>());
        services.AddSingleton<IAuthorizationGuard, AuthorizationGuard>();
        services.AddHostedService<AuthorizationSchemaHostedService>();
        return services;
    }

    private static IAuthorizationService ResolveEngine(IServiceProvider sp)
    {
        var mode = sp.GetRequiredService<IOptions<SpiceDbAuthorizationOptions>>().Value.Mode;
        return mode switch
        {
            "InMemory" => sp.GetRequiredService<InMemoryAuthorizationAdapter>(),
            "SpiceDb" => sp.GetRequiredService<SpiceDbAuthorizationAdapter>(),
            _ => sp.GetRequiredService<FailClosedAuthorizationAdapter>(),
        };
    }
}

/// <summary>
/// در استارت میزبان، schema را فقط وقتی ApplySchemaOnStartup روشن باشد اعمال می‌کند.
/// تولید با مقدار پیش‌فرض false هر بار schema را بازنویسی نمی‌کند.
/// </summary>
public sealed class AuthorizationSchemaHostedService : IHostedService
{
    private readonly IAuthorizationSchemaBootstrapper _bootstrapper;

    /// <summary>
    /// hosted service را روی bootstrapper می‌سازد.
    /// </summary>
    public AuthorizationSchemaHostedService(IAuthorizationSchemaBootstrapper bootstrapper) =>
        _bootstrapper = bootstrapper;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) =>
        _bootstrapper.BootstrapIfConfiguredAsync(cancellationToken);

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
