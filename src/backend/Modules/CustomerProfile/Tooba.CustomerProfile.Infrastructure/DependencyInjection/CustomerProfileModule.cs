using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.CustomerProfile.Contracts.Ports;
using Tooba.CustomerProfile.Infrastructure.Directories;
using Tooba.CustomerProfile.Infrastructure.Messaging;
using Tooba.CustomerProfile.Infrastructure.Persistence;
using Tooba.ModuleContracts;
using Tooba.Persistence;

namespace Tooba.CustomerProfile.Infrastructure.DependencyInjection;

/// <summary>ماژول مستقل پروفایل توصیفی مشتری با schema، قرارداد و Outbox اختصاصی.</summary>
public sealed class CustomerProfileModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "CustomerProfile";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton<IOutboxModuleRegistration, CustomerProfileOutboxRegistration>();
        services.AddScoped<ICustomerProfileDirectory, CustomerProfileDirectory>();
        services.AddModuleSchemaMigrator<CustomerProfileDbContext>("CustomerProfile", ModuleSchemaMigrationOrder.CustomerProfile);
        services.AddDbContext<CustomerProfileDbContext>((sp, options) =>
        {
            var connection = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connection,
                CustomerProfileDbContext.Schema,
                typeof(CustomerProfileDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}
