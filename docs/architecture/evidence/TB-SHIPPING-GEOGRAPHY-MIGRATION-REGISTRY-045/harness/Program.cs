using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.Geography.Contracts.Places;
using Tooba.Geography.Infrastructure.DependencyInjection;
using Tooba.Persistence;

var failures = new List<string>();
void Check(string name, bool ok, string? detail = null)
{
    Console.WriteLine((ok ? "PASS  " : "FAIL  ") + name + (detail is null ? "" : " :: " + detail));
    if (!ok) failures.Add(name);
}

// ===========================================================================
// 1. Canonical migration-order metadata (public API, compiled assembly).
// ===========================================================================
var orderFields = typeof(ModuleSchemaMigrationOrder)
    .GetFields(BindingFlags.Public | BindingFlags.Static)
    .Where(f => f.IsLiteral && f.FieldType == typeof(int))
    .Select(f => (Name: f.Name, Value: (int)f.GetRawConstantValue()!))
    .OrderBy(x => x.Value)
    .ToArray();

Console.WriteLine("Order constants: " + string.Join(", ", orderFields.Select(x => $"{x.Name}={x.Value}")));

var expectedOrder = new Dictionary<string, int>(StringComparer.Ordinal)
{
    ["Catalog"] = 1, ["Offer"] = 2, ["Pricing"] = 3, ["Inventory"] = 4, ["Tax"] = 5,
    ["Party"] = 6, ["Identity"] = 7, ["Cart"] = 8, ["Order"] = 9, ["Payment"] = 10,
    ["Fulfillment"] = 11, ["Promotion"] = 12, ["Reviews"] = 14, ["ProductQnA"] = 15,
    ["BulkInquiry"] = 16, ["Wishlist"] = 17, ["AddressBook"] = 18, ["CustomerProfile"] = 19,
    ["UserPreference"] = 20, ["OperatorProfile"] = 21, ["Localization"] = 22, ["Content"] = 23,
    ["Media"] = 24, ["PageComposition"] = 25, ["Story"] = 26, ["Notification"] = 27,
    ["AccessControl"] = 28, ["Support"] = 29, ["Geography"] = 30,
};

Check("M1 Geography constant exists on the single canonical order source and equals 30",
    orderFields.Any(x => x.Name == "Geography" && x.Value == 30),
    string.Join("|", orderFields.Where(x => x.Name == "Geography").Select(x => x.Value.ToString())));

Check("M2 every order value is unique (no duplicate/reordered slot)",
    orderFields.Select(x => x.Value).Distinct().Count() == orderFields.Length,
    orderFields.Length + " constants, " + orderFields.Select(x => x.Value).Distinct().Count() + " distinct");

var drifted = expectedOrder
    .Where(kv => !orderFields.Any(x => x.Name == kv.Key && x.Value == kv.Value))
    .Select(kv => kv.Key + "!=" + kv.Value)
    .ToArray();
Check("M3 all 28 pre-existing constants keep their exact previous values (append-only, nothing renumbered)",
    drifted.Length == 0, string.Join(", ", drifted));

Check("M4 exactly one new constant was added (29 total; 28 pre-existing + Geography)",
    orderFields.Length == 29, orderFields.Length.ToString());

// ===========================================================================
// 2. Production MigrationRunner registry metadata (internal type, via reflection
//    over the compiled assembly: proves the descriptor is really in the build).
// ===========================================================================
// Repo-relative-locatable build output of the production runner, produced by:
//   dotnet build src/backend/Host/Tooba.MigrationRunner/Tooba.MigrationRunner.csproj -p:OutDir=<repo>/src/backend/Host/Tooba.MigrationRunner/bin/chk045/
// (a separate OutDir is used because the running Development Host holds the default bin output).
// Passed as argv[0] (or GEO045_RUNNER_OUT) because this harness lives outside the repository.
var migrationRunnerOutDir = Path.GetFullPath(
    args.Length > 0
        ? args[0]
        : Environment.GetEnvironmentVariable("GEO045_RUNNER_OUT")
          ?? throw new InvalidOperationException("pass the MigrationRunner OutDir as argv[0] or GEO045_RUNNER_OUT"));
AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
{
    var simple = new AssemblyName(args.Name).Name + ".dll";
    var candidate = Path.Combine(migrationRunnerOutDir, simple);
    return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
};

var runnerAssembly = Assembly.LoadFrom(Path.Combine(migrationRunnerOutDir, "Tooba.MigrationRunner.dll"));
var registryType = runnerAssembly.GetType("Tooba.MigrationRunner.ModuleMigrationRegistry", throwOnError: true)!;
var allProperty = registryType.GetProperty("All", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;
var descriptors = ((System.Collections.IEnumerable)allProperty.GetValue(null)!).Cast<object>().ToArray();

string ModuleOf(object d) => (string)d.GetType().GetProperty("Module")!.GetValue(d)!;
string SchemaOf(object d) => (string)d.GetType().GetProperty("Schema")!.GetValue(d)!;

var moduleNames = descriptors.Select(ModuleOf).ToArray();
Console.WriteLine("Registry descriptors (" + descriptors.Length + "): " + string.Join(", ", moduleNames));

string[] preExistingRegistryOrder =
[
    "Catalog", "Offer", "Pricing", "Inventory", "Tax", "Party", "Identity", "Cart", "Order", "Payment",
    "Fulfillment", "Returns", "Settlement", "Support", "Wallet", "Notification", "AccessControl",
    "Promotion", "Reviews", "ProductQnA", "BulkInquiry", "Wishlist", "AddressBook", "CustomerProfile",
    "UserPreference", "OperatorProfile", "Localization", "Content", "Media", "PageComposition", "Story",
];

Check("R1 all 31 pre-existing descriptors are preserved in their exact previous order",
    moduleNames.Take(preExistingRegistryOrder.Length).SequenceEqual(preExistingRegistryOrder, StringComparer.Ordinal),
    moduleNames.Length >= 31 ? string.Join(",", moduleNames.Take(31)) : "too short");

Check("R2 exactly one descriptor was appended and it is Geography (nothing reordered)",
    moduleNames.Length == preExistingRegistryOrder.Length + 1
    && moduleNames[^1] == "Geography"
    && moduleNames.Distinct(StringComparer.Ordinal).Count() == moduleNames.Length,
    moduleNames.Length.ToString());

var geographyIndex = Array.IndexOf(moduleNames, "Geography");
Check("R3 Geography descriptor declares the module-owned schema \"geography\"",
    geographyIndex >= 0 && SchemaOf(descriptors[geographyIndex]) == "geography",
    geographyIndex >= 0 ? SchemaOf(descriptors[geographyIndex]) : "descriptor missing");

// The descriptor's context factory is the RUNNER's own code path. Invoking it must produce a
// GeographyDbContext whose migration metadata is complete - and it must not open a connection
// (the connection string below points at a closed port).
const string Unreachable = "Host=127.0.0.1;Port=1;Username=none;Password=none;Database=none";
var createContext = (Func<string, DbContext>)descriptors[geographyIndex]
    .GetType().GetProperty("CreateContext")!.GetValue(descriptors[geographyIndex])!;
await using var context = createContext(Unreachable);
Check("R4 descriptor factory builds a GeographyDbContext without any database contact",
    context is Tooba.Geography.Infrastructure.Persistence.GeographyDbContext
    && context.Database.GetDbConnection().ConnectionString == Unreachable,
    context.GetType().Name);

var migrations = context.Database.GetMigrations().ToArray();
Check("R5 descriptor sees exactly the one geography migration from task 042 (metadata complete, nothing pending-only)",
    migrations.Length == 1 && migrations[0].Contains("InitialGeography", StringComparison.Ordinal),
    string.Join(",", migrations));

Check("R6 descriptor resolves the same schema as the module's DbContext",
    context.Model.GetDefaultSchema() == "geography", context.Model.GetDefaultSchema());

// ===========================================================================
// 3. Non-activation proof: the Host-loaded module still cannot migrate.
// ===========================================================================
var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
services.AddSingleton<IHostEnvironment>(new EmptyHostEnvironment());
services.AddSingleton<ICurrentCommerceContext>(new FixedCommerceContext());
services.AddSingleton<IDatabaseConnectionResolver>(new FixedConnectionResolver(Unreachable));
services.AddScoped<Tooba.Localization.Contracts.Ports.ILanguageLookup>(_ => new EmptyLanguageLookup());

new GeographyModule().AddServices(services, new ConfigurationBuilder().Build(), new EmptyHostEnvironment());

Check("N1 Host-loaded GeographyModule still registers ZERO IModuleSchemaMigrator (Host startup cannot migrate geography)",
    !services.Any(d => d.ServiceType == typeof(IModuleSchemaMigrator)));

Check("N2 Host-loaded GeographyModule registers zero IModuleSchemaMigrationStep and zero IHostedService",
    !services.Any(d => d.ServiceType == typeof(IModuleSchemaMigrationStep))
    && !services.Any(d => d.ServiceType == typeof(IHostedService)));

Check("N3 Geography read seams are still the only module services (lookup + label + context)",
    services.Count(d => d.ServiceType == typeof(IGeographyLookup)) == 1
    && services.Count(d => d.ServiceType == typeof(IGeographyLabelLookup)) == 1);

using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
using (var scope = provider.CreateScope())
{
    Check("N4 the module graph still resolves (lookups + context) without database contact",
        scope.ServiceProvider.GetRequiredService<IGeographyLookup>() is Tooba.Geography.Infrastructure.Places.GeographyPlaceLookup
        && scope.ServiceProvider.GetRequiredService<IGeographyLabelLookup>() is Tooba.Geography.Infrastructure.Places.GeographyLabelLookup
        && scope.ServiceProvider.GetRequiredService<Tooba.Geography.Infrastructure.Persistence.GeographyDbContext>() is not null);
}

var hostAssembly = Assembly.LoadFrom(Path.Combine(migrationRunnerOutDir, "Tooba.Host.dll"));
var hostReferencesRunner = hostAssembly.GetReferencedAssemblies()
    .Any(a => string.Equals(a.Name, "Tooba.MigrationRunner", StringComparison.OrdinalIgnoreCase));
Check("N5 the runner registry is unreachable from Host: Tooba.Host references no Tooba.MigrationRunner",
    runnerAssembly.GetName().Name == "Tooba.MigrationRunner" && !hostReferencesRunner,
    "host->runner ref: " + hostReferencesRunner);

Console.WriteLine();
Console.WriteLine(failures.Count == 0 ? "ALL CHECKS PASSED" : "FAILURES: " + string.Join(", ", failures));
return failures.Count == 0 ? 0 : 1;

internal sealed class EmptyHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "geo045check";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
        new Microsoft.Extensions.FileProviders.NullFileProvider();
}

internal sealed class FixedCommerceContext : ICurrentCommerceContext
{
    public CommerceContext? Current { get; } = new CommerceContext(
        new EditionContext(ToobaEdition.SingleStore, "geo045-local"),
        null,
        new ConnectionReference("geo045-check"),
        "geo045-check");
}

internal sealed class FixedConnectionResolver(string connectionString) : IDatabaseConnectionResolver
{
    public string Resolve(ConnectionReference reference) => connectionString;
}

internal sealed class EmptyLanguageLookup : Tooba.Localization.Contracts.Ports.ILanguageLookup
{
    public Task<IReadOnlyList<Tooba.Localization.Contracts.Ports.LanguageLookupSnapshot>> ListAsync(
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Tooba.Localization.Contracts.Ports.LanguageLookupSnapshot>>(
            Array.Empty<Tooba.Localization.Contracts.Ports.LanguageLookupSnapshot>());
}

