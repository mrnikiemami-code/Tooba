using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.ModuleContracts;
using Tooba.Party.Application.Ports;
using Tooba.Party.Contracts.Errors;
using Tooba.Party.Contracts.Ports;
using Tooba.Party.Infrastructure.Adapters;
using Tooba.Party.Infrastructure.Admin;
using Tooba.Party.Infrastructure.Development;
using Tooba.Party.Infrastructure.Directories;
using Tooba.Party.Infrastructure.Events;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Party.Infrastructure.Projections;
using Tooba.Party.Infrastructure.Seller;
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
        services.AddSingleton<IErrorCatalogContributor, PartyErrorCatalogContributor>();
        services.AddSingleton<IErrorResourceSet, PartyErrorResourceSet>();
        services.AddScoped<IPartyDirectory, PartyDirectory>();
        services.AddScoped<IPartySellerSettings, Tooba.Party.Infrastructure.Seller.PartySellerSettingsAdapter>();
        services.AddScoped<IPartyDevelopmentSeedGateway, PartyDevelopmentSeedGateway>();
        services.AddScoped<IPartyDevelopmentDirectory, PartyDevelopmentDirectoryAdapter>();
        services.AddScoped<IPartyLookupGateway>(sp => (PartyDirectory)sp.GetRequiredService<IPartyDirectory>());
        services.AddScoped<IPartyLookup>(sp => (PartyDirectory)sp.GetRequiredService<IPartyDirectory>());
        services.AddScoped<IPartyAdminSellerReadGateway, PartyAdminSellerReadGateway>();
        services.AddScoped<IAdminSellersGridPort, AdminSellersGridAdapter>();
        services.AddScoped<IIntegrationEventHandler<PartyMembershipEstablishedIntegrationEvent>, PartyMembershipProjectionHandler>();
        services.AddModuleSchemaMigrator<PartyDbContext>("Party", ModuleSchemaMigrationOrder.Party);
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

