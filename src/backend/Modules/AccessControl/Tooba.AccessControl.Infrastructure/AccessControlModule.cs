using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.AccessControl.Application;
using Tooba.AccessControl.Infrastructure.Persistence;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Persistence;

namespace Tooba.AccessControl.Infrastructure;

/// <summary>
/// ماژول Access Control: نقش/مجوز/سقف در PG و enforcement در SpiceDB.
/// </summary>
public sealed class AccessControlModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "AccessControl";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddSingleton<AccessControlInstrumentation>();
        services.AddSingleton<IOutboxModuleRegistration, AccessControlOutboxRegistration>();
        services.AddScoped<AccessControlDirectory>();
        services.AddScoped<IAccessControlDirectory>(sp => sp.GetRequiredService<AccessControlDirectory>());

        services.AddDbContext<AccessControlDbContext>((sp, options) =>
        {
            var connectionString = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connectionString,
                AccessControlDbContext.Schema,
                typeof(AccessControlDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}
