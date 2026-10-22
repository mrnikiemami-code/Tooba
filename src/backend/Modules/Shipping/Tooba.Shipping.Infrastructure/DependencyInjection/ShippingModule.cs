using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.Persistence;
using Tooba.ModuleContracts;
using Tooba.Shipping.Application.Packaging.Ports;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Application.Rates.Ports;using Tooba.Shipping.Application.Scheduling.Ports;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Contracts.Operations;
using Tooba.Shipping.Infrastructure.Operations;
using Tooba.Shipping.Infrastructure.Authoring;
using Tooba.Shipping.Infrastructure.Packaging;
using Tooba.Shipping.Infrastructure.Persistence;
using Tooba.Shipping.Infrastructure.Rates;
using Tooba.Shipping.Infrastructure.Scheduling;

namespace Tooba.Shipping.Infrastructure.DependencyInjection;

/// <summary>
/// ریشهٔ ترکیب ماژول Shipping. در این Task عمداً <b>به Host وصل نشده است</b>.
/// </summary>
/// <remarks>
/// <para>
/// <b>What exists.</b> The module owns one <see cref="ShippingDbContext"/> (its own <c>shipping</c> schema),
/// its own authoring write paths (admin authoring directories plus the D3 draft/region-group aggregate)
/// and read-only Contracts seams such as <see cref="IShippingRateQuoteLookup"/>. No quote, order or
/// financial row is written here: the writes are limited to Shipping-owned authoring configuration.
/// </para>
/// <para>
/// <b>How it is composed.</b> The Host's <c>ToobaModuleComposition</c> registers an explicit,
/// hand-maintained module list and instantiates this class once there; the Shipping HTTP surface is
/// mapped separately by <c>ShippingEndpointModule</c> in <c>Tooba.Shipping.Endpoints</c>. This class
/// therefore only registers module-owned data seams and never route composition.
/// </para>
/// <para>
/// <b>No schema migrator is registered.</b> Consistent with
/// <c>GeographyModule</c>/<c>SHIPPING-GEOGRAPHY-MODULE-COMPOSITION-044</c>: the running Development Host
/// executes <em>every</em> registered <c>IModuleSchemaMigrator</c> at startup, so registering one here
/// would emit <c>CREATE SCHEMA/TABLE</c> DDL against the Tooba development database. Shipping migrations
/// under <c>Persistence/Migrations</c> are applied only by the CLI migration runner
/// (<c>Tooba.MigrationRunner</c>), never at Host startup; the Host hook stays <b>off</b>.
/// </para>
/// </remarks>
public sealed class ShippingModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Shipping";

    /// <inheritdoc />
    public void AddServices(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddScoped<IShippingRateQuoteLookup, ShippingRateQuoteLookup>();
        // Packaging fee is a Shipping-owned commercial fact kept SEPARATE from the freight charge:
        // this read-only seam answers "what is the packaging component" without ever mutating a quote.
        services.AddScoped<IShippingPackagingFeeLookup, ShippingPackagingFeeLookup>();
        // Forward-looking read-only composition of the two existing components (carrier freight +
        // packaging). It adds no new business rule and has no live Checkout consumer yet.
        services.AddScoped<IShippingShipmentCostLookup, ShippingShipmentCostLookup>();
        // Admin authoring seam for Shipping-owned rate policies and coverage grants. Registered here
        // (module-owned DI) so no Host edit is needed for the data path; the HTTP layer stays in
        // Tooba.Shipping.Endpoints and is wired by the Host at 055.
        services.AddScoped<IShippingRateCoverageDirectory, ShippingRateCoverageDirectory>();
        // TB-161/TB-163 seam: the method-scoped coverage restriction directory is satisfied by the SAME
        // module-owned adapter (it implements both read ports). Without this registration the Shipping
        // command handler cannot be activated and the whole Host fails DI validation.
        services.AddScoped<IShippingMethodCoverageDirectory, ShippingRateCoverageDirectory>();
        // Admin authoring seam for Shipping-owned packaging fee policies, separate from rate policies.
        services.AddScoped<IShippingPackagingPolicyDirectory, ShippingPackagingPolicyDirectory>();
        // Admin authoring seam for the Shipping-owned weekly operating schedule (carrier pickup/dispatch
        // availability). Registered here (module-owned DI) so no Host edit is needed for the data path;
        // the HTTP layer stays in Tooba.Shipping.Endpoints.
        services.AddScoped<IShippingOperatingScheduleDirectory, ShippingOperatingScheduleDirectory>();
        // Read-only operational evaluation seam: composes the EXISTING rate engine, packaging resolver and
        // schedule evaluator into one auditable answer. It adds no new pricing rule, writes nothing and has
        // no live Checkout consumer yet.
        services.AddScoped<IShippingOperationalQuoteLookup, ShippingOperationalQuoteLookup>();
        // Destination identity stays Geography-owned: this adapter validates submitted scope ids through
        // the Geography Contracts read seam only (never a foreign DbContext or cross-schema FK).
        services.AddScoped<IShippingDestinationScopeValidator, GeographyDestinationScopeValidator>();
        // Method identity stays Fulfillment-owned: an option code is never hardcoded here, it is looked up
        // in the real shipping catalog through the Fulfillment Contracts read seam.
        services.AddScoped<IShippingMethodOptionValidator, FulfillmentShippingMethodOptionValidator>();
        // TB-188 authoring context: the admin panel must read the effective edition, configured default
        // currency and ACTIVE content languages from their real owners (platform edition context,
        // StoreContext.Contracts, Localization.Contracts) instead of treating preview fixtures as store
        // configuration. Read-only, Contracts-only, and no cache/fallback of its own.
        services.AddScoped<IShippingAuthoringContextSource, ShippingAuthoringContextSource>();
        // D3 authoring drafts: Shipping-owned draft/group/area persistence and the Geography Contracts
        // ancestry proof for submitted selections. Both are module-owned; the owner scope is mapped to
        // the persisted owner representation only inside the adapter.
        services.AddScoped<IShippingAuthoringDraftDirectory, ShippingAuthoringDraftDirectory>();
        services.AddScoped<IShippingAuthoringDestinationResolver, ShippingAuthoringDestinationResolver>();
        // TB-197 draft type assignment: the shared type catalog stays Fulfillment-owned and is read ONLY
        // through its Contracts seam (no foreign DbContext, no local taxonomy, no cache). Registered
        // scoped once, like every other module-owned adapter.
        services.AddScoped<IShippingTypeCatalogReader, FulfillmentShippingTypeCatalogReader>();
        services.AddDbContext<ShippingDbContext>((sp, options) =>
        {
            var connection = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connection,
                ShippingDbContext.Schema,
                typeof(ShippingDbContext));
        });
    }
}
