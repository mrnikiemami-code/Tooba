using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.ModuleContracts;
using Tooba.StoreContext.Contracts.Current;
using Tooba.StoreContext.Infrastructure.Current;

namespace Tooba.StoreContext.Infrastructure;

/// <summary>
/// StoreContext module composition: registers the scoped effective store commerce context accessor
/// and exposes it through both Contracts seams (read + assign). No DB, schema, migration, endpoint,
/// or application use-case in this foundation phase.
/// </summary>
public sealed class StoreContextModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "StoreContext";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddScoped<StoreCommerceContextAccessor>();
        services.AddScoped<ICurrentStoreCommerceContext>(sp => sp.GetRequiredService<StoreCommerceContextAccessor>());
        services.AddScoped<IStoreCommerceContextAssigner>(sp => sp.GetRequiredService<StoreCommerceContextAccessor>());
    }
}
