using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Persistence;
using Tooba.Support.Application.Ports;
using Tooba.Support.Infrastructure.Adapters;
using Tooba.Support.Infrastructure.Directories;
using Tooba.Support.Infrastructure.Messaging;
using Tooba.Support.Infrastructure.Persistence;

namespace Tooba.Support.Infrastructure.DependencyInjection;

/// <summary>ماژول مستقل Support و schema اختصاصی آن.</summary>
public sealed class SupportModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Support";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton<IOutboxModuleRegistration, SupportOutboxRegistration>();
        services.AddScoped<ISupportDirectory, SupportDirectory>();
        services.AddSingleton<ISupportDemoPreviewPort, SupportDemoPreviewAdapter>();
        services.AddDbContext<SupportDbContext>((sp, options) =>
        {
            var connection = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(options, connection, SupportDbContext.Schema, typeof(SupportDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}
