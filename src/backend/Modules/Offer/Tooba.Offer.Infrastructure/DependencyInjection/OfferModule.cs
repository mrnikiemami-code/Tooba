using Tooba.Offer.Infrastructure.Outbox;
using Tooba.Offer.Infrastructure.Adapters;
using Tooba.Offer.Contracts.Ports;
using Tooba.Offer.Application.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Offer.Infrastructure.Persistence;
using Tooba.Offer.Application.ReadModels;
using Tooba.Persistence;

namespace Tooba.Offer.Infrastructure.DependencyInjection;

/// <summary>
/// Registers the Offer module, which owns seller listings but not price, inventory, or Catalog persistence.
/// </summary>
public sealed class OfferModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Offer";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddSingleton<IOutboxModuleRegistration, OfferOutboxRegistration>();
        services.AddSingleton<IReturnPolicyResolver>(_ =>
        {
            var section = configuration.GetSection(ReturnPolicyOptions.SectionName);
            static int ReadInt(IConfigurationSection s, string key, int fallback) =>
                int.TryParse(s[key], out var v) ? v : fallback;
            static bool ReadBool(IConfigurationSection s, string key, bool fallback) =>
                bool.TryParse(s[key], out var v) ? v : fallback;
            var opts = new ReturnPolicyOptions
            {
                DefaultReturnWindowDays = ReadInt(section, "DefaultReturnWindowDays", 7),
                SellerCanOverrideReturnPolicy = ReadBool(section, "SellerCanOverrideReturnPolicy", true),
                MinReturnWindowDays = ReadInt(section, "MinReturnWindowDays", 1),
                MaxReturnWindowDays = ReadInt(section, "MaxReturnWindowDays", 30),
                AllowNonReturnableOffers = ReadBool(section, "AllowNonReturnableOffers", true),
            };
            return new ReturnPolicyResolver(opts);
        });
        services.AddScoped<IOfferStore, OfferStore>();
        services.AddScoped<OfferReadModelComposer>();
        services.AddScoped<IOfferLookupGateway>(sp => (OfferStore)sp.GetRequiredService<IOfferStore>());
        services.AddDbContext<OfferDbContext>((sp, options) =>
        {
            var connectionString = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connectionString,
                OfferDbContext.Schema,
                typeof(OfferDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}
