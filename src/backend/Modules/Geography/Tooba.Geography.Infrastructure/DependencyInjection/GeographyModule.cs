using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.Geography.Contracts.Places;
using Tooba.Geography.Infrastructure.Persistence;
using Tooba.Geography.Infrastructure.Places;
using Tooba.ModuleContracts;
using Tooba.Persistence;

namespace Tooba.Geography.Infrastructure.DependencyInjection;

/// <summary>
/// ریشهٔ ترکیب ماژول Geography: فقط ثبت سرویس‌های خودِ همین ماژول.
/// </summary>
/// <remarks>
/// <para>
/// <b>What is ACTIVE here.</b> The module owns one <see cref="GeographyDbContext"/> (its own
/// <c>geography</c> schema, its own migration history table) and the two read-only Contracts seams
/// <see cref="IGeographyLookup"/> / <see cref="IGeographyLabelLookup"/>. The context takes its connection
/// from <see cref="ICurrentCommerceContext"/> and <see cref="IDatabaseConnectionResolver"/> exactly like every
/// other module context, and the two lookups are the only implementations registered for those interfaces.
/// No endpoint, no hosted service, no outbox registration, no seed, no consumer change: nothing in this
/// module writes, and nothing resolves its context until a caller asks for a lookup.
/// </para>
/// <para>
/// <b>What is deliberately NOT here (schema migration).</b> This composition root does <em>not</em> call
/// <c>AddModuleSchemaMigrator&lt;GeographyDbContext&gt;</c>. Two independent reasons, both verified in 044:
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// <b>Runtime auto-apply.</b> The running Development Host executes <em>every</em> registered
/// <c>IModuleSchemaMigrator</c> at startup (<c>Host/Development/DevelopmentSchemaMigrator</c> →
/// <c>MigrateOrderedAsync</c> → <c>Database.MigrateAsync()</c>). Registering the geography migrator in a module
/// the Host loads would therefore emit <c>CREATE SCHEMA/TABLE/TRIGGER</c> DDL against the Tooba development
/// database on the next start, which is exactly the implicit, unauthorized migration DDL this Task forbids.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>No expressible canonical slot.</b> The single authoritative order source is
/// <c>ModuleSchemaMigrationOrder</c> in <c>Tooba.Persistence</c>. Adding a <c>Geography</c> constant there is
/// outside this Task's allowed paths and is explicitly held for a separate approval; inventing a literal order
/// number here would create the second contradictory ordering model that this seam exists to prevent. Until
/// that constant exists, the canonical registration cannot be written, so the migration hook is
/// <b>prepared-only</b> and documented in
/// <c>docs/planning/shipping/SHIPPING-GEOGRAPHY-MODULE-COMPOSITION-044.md</c>.
/// </description>
/// </item>
/// </list>
/// </remarks>
public sealed class GeographyModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Geography";

    /// <inheritdoc />
    public void AddServices(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddScoped<IGeographyLookup, GeographyPlaceLookup>();
        services.AddScoped<IGeographyLabelLookup, GeographyLabelLookup>();
        services.AddDbContext<GeographyDbContext>((sp, options) =>
        {
            var connection = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connection,
                GeographyDbContext.Schema,
                typeof(GeographyDbContext));
        });
    }
}
