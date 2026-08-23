using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Party.Application;
using Tooba.Party.Infrastructure.Events;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.Party.Infrastructure;

/// <summary>
/// ماژول Party: شخص/سازمان/عضویت. احراز هویت Identity و ماتریس مجوز محصول اینجا نیست.
/// </summary>
public sealed class PartyModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Party";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddSingleton<IOutboxModuleRegistration, PartyOutboxRegistration>();
        services.AddScoped<IPartyDirectory, PartyDirectory>();
        services.AddScoped<IPartyLookupGateway>(sp => (PartyDirectory)sp.GetRequiredService<IPartyDirectory>());
        services.AddScoped<IIntegrationEventHandler<PartyMembershipEstablishedIntegrationEvent>, PartyMembershipProjectionHandler>();
        services.AddDbContext<PartyDbContext>((sp, options) =>
        {
            var connectionString = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connectionString,
                PartyDbContext.Schema,
                typeof(PartyDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}
