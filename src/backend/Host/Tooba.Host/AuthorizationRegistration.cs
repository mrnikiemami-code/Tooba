using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;

namespace Tooba.Host;

/// <summary>
/// ثبت مجوز Host. SDK SpiceDB به Domain/Application نشت نمی‌کند.
/// </summary>
internal static class AuthorizationRegistration
{
    /// <summary>
    /// قراردادهای Tooba و adapter مطابق Mode را ثبت می‌کند.
    /// </summary>
    public static IServiceCollection AddToobaAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<AuthorizationInstrumentation>();
        services.AddSingleton<IAuthorizationSchemaProvider, FoundationAuthorizationSchemaProvider>();
        services.AddSingleton<IAuthorizationSchemaBootstrapper, ConfiguredAuthorizationSchemaBootstrapper>();
        services.AddSingleton<IAuthorizationSecurityEventSink, InMemoryAuthorizationSecurityEventSink>();
        services.AddSingleton<InMemoryAuthorizationAdapter>();
        services.AddSingleton(sp =>
            new FailClosedAuthorizationAdapter("authorization.disabled", sp.GetRequiredService<AuthorizationInstrumentation>()));
        services.AddSingleton<SpiceDbAuthorizationAdapter>();
        services.AddSingleton<IAuthorizationService>(ResolveEngine);
        services.AddSingleton<IAuthorizationTupleWriter>(sp => (IAuthorizationTupleWriter)sp.GetRequiredService<IAuthorizationService>());
        services.AddSingleton<IAuthorizationGuard, AuthorizationGuard>();
        return services;
    }

    private static IAuthorizationService ResolveEngine(IServiceProvider sp)
    {
        var mode = sp.GetRequiredService<IOptions<AuthorizationHostOptions>>().Value.Mode;
        return mode switch
        {
            "InMemory" => sp.GetRequiredService<InMemoryAuthorizationAdapter>(),
            "SpiceDb" => sp.GetRequiredService<SpiceDbAuthorizationAdapter>(),
            _ => sp.GetRequiredService<FailClosedAuthorizationAdapter>(),
        };
    }
}
