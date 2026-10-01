using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tooba.AccessControl.Infrastructure.Authorization;
using Tooba.AccessControl.Infrastructure.Persistence;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Persistence;

using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Application.Permissions;
using Tooba.AccessControl.Application.Development.Seller;
using Tooba.AccessControl.Contracts.Development;
using Tooba.AccessControl.Infrastructure.Development;
using Tooba.AccessControl.Infrastructure.Development.Seller;
using Tooba.AccessControl.Infrastructure.Directories;
using Tooba.AccessControl.Infrastructure.Observability;
using Tooba.AccessControl.Infrastructure.Messaging;
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
        services.AddScoped<SellerDevContextBootstrap>();
        services.AddScoped<ISellerDevContextStore>(sp => sp.GetRequiredService<SellerDevContextBootstrap>());
        services.AddScoped<IAccessControlDevelopmentSeedPrelude, AccessControlDevelopmentSeedPrelude>();
        services.AddScoped<
            Tooba.AccessControl.Contracts.Access.IAccessControlEffectiveAccessReader,
            Adapters.AccessControlEffectiveAccessReader>();
        services.AddScoped<
            Tooba.AccessControl.Contracts.Readiness.IAuthorizationReadinessProbe,
            Adapters.AuthorizationReadinessProbe>();

        services.AddOptions<SpiceDbAuthorizationOptions>()
            .Bind(configuration.GetSection(SpiceDbAuthorizationOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SpiceDbAuthorizationOptions>, SpiceDbAuthorizationOptionsValidator>();
        services.AddToobaAuthorization();

        services.AddModuleSchemaMigrator<AccessControlDbContext>("AccessControl", ModuleSchemaMigrationOrder.AccessControl);
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
