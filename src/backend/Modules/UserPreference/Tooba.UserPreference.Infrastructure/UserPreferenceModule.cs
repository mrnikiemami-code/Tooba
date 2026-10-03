using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Persistence;
using Tooba.UserPreference.Application.Ports;
using Tooba.UserPreference.Infrastructure.Directories;
using Tooba.UserPreference.Infrastructure.Persistence;

namespace Tooba.UserPreference.Infrastructure;

/// <summary>ماژول مستقل ترجیح کاربر با schema، قرارداد و Outbox اختصاصی.</summary>
public sealed class UserPreferenceModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "UserPreference";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton<IOutboxModuleRegistration, UserPreferenceOutboxRegistration>();
        services.AddScoped<IUserPreferenceDirectory, UserPreferenceDirectory>();
        services.AddScoped<IUiPreferenceDirectory, UiPreferenceDirectory>();
        services.AddModuleSchemaMigrator<UserPreferenceDbContext>("UserPreference", ModuleSchemaMigrationOrder.UserPreference);
        services.AddDbContext<UserPreferenceDbContext>((sp, options) =>
        {
            var connection = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connection,
                UserPreferenceDbContext.Schema,
                typeof(UserPreferenceDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}
