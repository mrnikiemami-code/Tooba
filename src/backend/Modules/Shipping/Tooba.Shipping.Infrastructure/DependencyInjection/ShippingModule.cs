using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.Persistence;
using Tooba.ModuleContracts;
using Tooba.Shipping.Application.Rates.Ports;
using Tooba.Shipping.Application.Scheduling.Ports;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Infrastructure.Persistence;
using Tooba.Shipping.Infrastructure.Rates;
using Tooba.Shipping.Infrastructure.Scheduling;

namespace Tooba.Shipping.Infrastructure.DependencyInjection;

/// <summary>
/// ریشهٔ ترکیب ماژول Shipping. در این Task عمداً <b>به Host وصل نشده است</b>.
/// </summary>
/// <remarks>
/// <para>
/// <b>What exists.</b> The module owns one <see cref="ShippingDbContext"/> (its own <c>shipping</c> schema)
/// and one read-only Contracts seam, <see cref="IShippingRateQuoteLookup"/>, backed by
/// <see cref="ShippingRateQuoteLookup"/>. Nothing in this module writes.
/// </para>
/// <para>
/// <b>Why it is not composed yet.</b> This slice is a non-runtime foundation: <c>Tooba.Host</c> has no
/// project reference to this assembly, and the Host's <c>ToobaModuleComposition</c> registers an explicit,
/// hand-maintained module list, so adding this class changes no running process. Registering the type
/// would require a Host edit (forbidden here) plus a schema-migration decision.
/// </para>
/// <para>
/// <b>No schema migrator is registered.</b> Consistent with
/// <c>GeographyModule</c>/<c>SHIPPING-GEOGRAPHY-MODULE-COMPOSITION-044</c>: the running Development Host
/// executes <em>every</em> registered <c>IModuleSchemaMigrator</c> at startup, so registering one here
/// would emit <c>CREATE SCHEMA/TABLE</c> DDL against the Tooba development database. The canonical order
/// source (<c>ModuleSchemaMigrationOrder</c>) is outside this Task's allowed paths, so the hook is
/// <b>prepared-only</b>: no migration exists and none is applied.
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
        // Admin authoring seam for Shipping-owned rate policies and coverage grants. Registered here
        // (module-owned DI) so no Host edit is needed for the data path; the HTTP layer stays in
        // Tooba.Shipping.Endpoints and is wired by the Host at 055.
        services.AddScoped<IShippingRateCoverageDirectory, ShippingRateCoverageDirectory>();
        // Admin authoring seam for the Shipping-owned weekly operating schedule (carrier pickup/dispatch
        // availability). Registered here (module-owned DI) so no Host edit is needed for the data path;
        // the HTTP layer stays in Tooba.Shipping.Endpoints.
        services.AddScoped<IShippingOperatingScheduleDirectory, ShippingOperatingScheduleDirectory>();
        // Destination identity stays Geography-owned: this adapter validates submitted scope ids through
        // the Geography Contracts read seam only (never a foreign DbContext or cross-schema FK).
        services.AddScoped<IShippingDestinationScopeValidator, GeographyDestinationScopeValidator>();
        // Method identity stays Fulfillment-owned: an option code is never hardcoded here, it is looked up
        // in the real shipping catalog through the Fulfillment Contracts read seam.
        services.AddScoped<IShippingMethodOptionValidator, FulfillmentShippingMethodOptionValidator>();
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
