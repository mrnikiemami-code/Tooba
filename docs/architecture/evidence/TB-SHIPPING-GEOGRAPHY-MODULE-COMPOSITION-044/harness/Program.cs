using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.Geography.Contracts.Places;
using Tooba.Geography.Infrastructure.DependencyInjection;
using Tooba.Geography.Infrastructure.Persistence;
using Tooba.Geography.Infrastructure.Places;
using Tooba.Localization.Contracts.Ports;
using Tooba.Persistence;

var failures = new List<string>();
void Check(string name, bool ok, string? detail = null)
{
    Console.WriteLine((ok ? "PASS  " : "FAIL  ") + name + (detail is null ? "" : " :: " + detail));
    if (!ok) failures.Add(name);
}

// ---------------------------------------------------------------------------
// Composition-only DI construction proof. No service startup, no database contact:
// the connection string points at a closed port and nothing is ever executed.
// ---------------------------------------------------------------------------
const string UnreachableConnectionString = "Host=127.0.0.1;Port=1;Username=none;Password=none;Database=none";

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
services.AddSingleton<IHostEnvironment>(new EmptyHostEnvironment());
services.AddSingleton<ICurrentCommerceContext>(new FixedCommerceContext());
services.AddSingleton<IDatabaseConnectionResolver>(new FixedConnectionResolver(UnreachableConnectionString));
// Localization owns this seam in production; the stand-in keeps the proof inside one module's graph.
services.AddScoped<ILanguageLookup>(_ => new EmptyLanguageLookup());

var module = new GeographyModule();
Check("C1 module identity is exactly \"Geography\"", module.Name == "Geography", module.Name);

module.AddServices(services, new ConfigurationBuilder().Build(), new EmptyHostEnvironment());

var lookupDescriptors = services.Where(d => d.ServiceType == typeof(IGeographyLookup)).ToArray();
var labelDescriptors = services.Where(d => d.ServiceType == typeof(IGeographyLabelLookup)).ToArray();
Check("C2 exactly one IGeographyLookup registration, scoped, implemented by GeographyPlaceLookup",
    lookupDescriptors.Length == 1
    && lookupDescriptors[0].Lifetime == ServiceLifetime.Scoped
    && lookupDescriptors[0].ImplementationType == typeof(GeographyPlaceLookup),
    lookupDescriptors.Length == 1 ? lookupDescriptors[0].ImplementationType?.Name : "count=" + lookupDescriptors.Length);

Check("C3 exactly one IGeographyLabelLookup registration, scoped, implemented by GeographyLabelLookup",
    labelDescriptors.Length == 1
    && labelDescriptors[0].Lifetime == ServiceLifetime.Scoped
    && labelDescriptors[0].ImplementationType == typeof(GeographyLabelLookup),
    labelDescriptors.Length == 1 ? labelDescriptors[0].ImplementationType?.Name : "count=" + labelDescriptors.Length);

Check("C4 exactly one GeographyDbContext registration, scoped",
    services.Count(d => d.ServiceType == typeof(GeographyDbContext)) == 1
    && services.Single(d => d.ServiceType == typeof(GeographyDbContext)).Lifetime == ServiceLifetime.Scoped);

// Migration-safety proof at the composition level: this module contributes NO schema migrator,
// so the running Host's IModuleSchemaMigrator enumeration cannot reach geography DDL.
Check("C5 module registers zero IModuleSchemaMigrator (no implicit DDL path)",
    !services.Any(d => d.ServiceType == typeof(IModuleSchemaMigrator)));

Check("C6 module registers zero IOutboxModuleRegistration (no background worker can poll geography tables)",
    !services.Any(d => d.ServiceType.Name.Contains("IOutboxModuleRegistration", StringComparison.Ordinal)));

Check("C7 module registers no hosted service (nothing runs at startup)",
    !services.Any(d => d.ServiceType == typeof(IHostedService)));

Check("C8 module registers no IModuleSchemaMigrationStep",
    !services.Any(d => d.ServiceType == typeof(IModuleSchemaMigrationStep)));

using var provider = services.BuildServiceProvider(new ServiceProviderOptions
{
    ValidateScopes = true,
    ValidateOnBuild = true,
});

using (var scope = provider.CreateScope())
{
    var lookup = scope.ServiceProvider.GetRequiredService<IGeographyLookup>();
    var labels = scope.ServiceProvider.GetRequiredService<IGeographyLabelLookup>();
    var context = scope.ServiceProvider.GetRequiredService<GeographyDbContext>();

    Check("D1 IGeographyLookup resolves to GeographyPlaceLookup", lookup is GeographyPlaceLookup, lookup.GetType().Name);
    Check("D2 IGeographyLabelLookup resolves to GeographyLabelLookup", labels is GeographyLabelLookup, labels.GetType().Name);
    Check("D3 GeographyDbContext resolves without any database contact",
        context.Database.GetDbConnection().ConnectionString == UnreachableConnectionString,
        context.Database.GetDbConnection().ConnectionString);
    Check("D4 resolved schema is the module-owned \"geography\" schema",
        context.Model.GetDefaultSchema() == "geography", context.Model.GetDefaultSchema());
    Check("D5 model contains the nine geography tables the initial migration created",
        context.Model.GetEntityTypes().Count() == 9,
        string.Join(",", context.Model.GetEntityTypes().Select(e => e.GetTableName()).OrderBy(x => x)));
    Check("D6 two lookups resolved from the same scope share the same context instance",
        ReferenceEquals(
            context,
            typeof(GeographyPlaceLookup)
                .GetField("_db", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .GetValue(lookup)));
}

Check("D7 BuildServiceProvider with ValidateOnBuild+ValidateScopes succeeded (graph is complete)",
    true);

Console.WriteLine();
Console.WriteLine(failures.Count == 0 ? "ALL CHECKS PASSED" : "FAILURES: " + string.Join(", ", failures));
return failures.Count == 0 ? 0 : 1;

internal sealed class EmptyHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "geo044check";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
        new Microsoft.Extensions.FileProviders.NullFileProvider();
}

internal sealed class FixedCommerceContext : ICurrentCommerceContext
{
    public CommerceContext? Current { get; } = new CommerceContext(
        new EditionContext(ToobaEdition.SingleStore, "geo044-local"),
        null,
        new ConnectionReference("geo044-check"),
        "geo044-check");
}

internal sealed class FixedConnectionResolver(string connectionString) : IDatabaseConnectionResolver
{
    public string Resolve(ConnectionReference reference) => connectionString;
}

internal sealed class EmptyLanguageLookup : ILanguageLookup
{
    public Task<IReadOnlyList<LanguageLookupSnapshot>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<LanguageLookupSnapshot>>(Array.Empty<LanguageLookupSnapshot>());
}
