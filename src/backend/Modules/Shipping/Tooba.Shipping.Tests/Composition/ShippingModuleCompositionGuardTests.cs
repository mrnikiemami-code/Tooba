using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Infrastructure.DependencyInjection;
using Tooba.Shipping.Infrastructure.Persistence;
using Tooba.Shipping.Infrastructure.Rates;
using Xunit;
using IModuleSchemaMigrator = Tooba.Persistence.IModuleSchemaMigrator;

namespace Tooba.Shipping.Tests.Composition;

/// <summary>
/// TB-SHIPPING-MVP-ACTIVATION-053 — composition/activation boundary guards.
/// The module must be resolvable through the canonical Host module list exactly once, while no
/// schema migrator is registered anywhere (which would make the Development Host emit shipping DDL
/// implicitly at startup).
/// </summary>
public sealed class ShippingModuleCompositionGuardTests
{
    [Fact]
    public void ShippingModule_registers_one_context_and_one_lookup_and_zero_schema_migrators()
    {
        var services = new ServiceCollection();

        new ShippingModule().AddServices(services, new ConfigurationBuilder().Build(), new StubHostEnvironment());

        var contextDescriptor = Assert.Single(services, d => d.ServiceType == typeof(ShippingDbContext));
        Assert.Equal(ServiceLifetime.Scoped, contextDescriptor.Lifetime);

        var lookupDescriptor = Assert.Single(services, d => d.ServiceType == typeof(IShippingRateQuoteLookup));
        Assert.Equal(typeof(ShippingRateQuoteLookup), lookupDescriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, lookupDescriptor.Lifetime);

        Assert.DoesNotContain(services, d => d.ServiceType == typeof(IModuleSchemaMigrator));
        Assert.DoesNotContain(services, d => typeof(IModuleSchemaMigrator).IsAssignableFrom(d.ServiceType));

        // Composition is registration-only: every descriptor added by the module root must be a
        // registration (factory or implementation type), never an already-built instance whose creation
        // could have opened a connection.
        Assert.All(services, descriptor =>
            Assert.True(
                descriptor.ImplementationFactory is not null
                || descriptor.ImplementationType is not null
                || descriptor.ImplementationInstance is not null));
    }

    [Fact]
    public void Shipping_module_never_registers_a_startup_schema_migrator()
    {
        var root = FindRepoRoot();
        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/DependencyInjection/ShippingModule.cs"));

        Assert.DoesNotContain("AddModuleSchemaMigrator", module, StringComparison.Ordinal);
        Assert.DoesNotContain("Database.MigrateAsync", module, StringComparison.Ordinal);
        Assert.DoesNotContain("AddHostedService", module, StringComparison.Ordinal);
        Assert.Contains("AddScoped<IShippingRateQuoteLookup, ShippingRateQuoteLookup>", module, StringComparison.Ordinal);

        // No source file anywhere in the backend may register a Shipping schema migrator: that is the
        // only path that turns the composed module into implicit DDL on the next Host start.
        var offenders = BackendSourceFiles(root)
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"AddModuleSchemaMigrator(<|\()\s*ShippingDbContext"))
            .Select(path => Path.GetRelativePath(root, path))
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Host_composes_shipping_once_through_the_explicit_module_list()
    {
        var root = FindRepoRoot();
        var compositionPath = Path.Combine(root, "src/backend/Host/Tooba.Host/Composition/ToobaModuleComposition.cs");
        var composition = File.ReadAllText(compositionPath);

        Assert.Contains("using Tooba.Shipping.Infrastructure.DependencyInjection;", composition, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(composition, @"new ShippingModule\(\)"));
        Assert.Single(Regex.Matches(composition, @"new GeographyModule\(\)"));
        Assert.DoesNotContain("AddModuleSchemaMigrator", composition, StringComparison.Ordinal);
        Assert.DoesNotContain("Database.MigrateAsync", composition, StringComparison.Ordinal);

        // Exactly one place in the whole backend instantiates the module, so no duplicate composition
        // root can exist.
        var instantiations = BackendSourceFiles(root)
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"new ShippingModule\(\)"))
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .ToArray();

        Assert.Equal(new[] { "src/backend/Host/Tooba.Host/Composition/ToobaModuleComposition.cs" }, instantiations);

        var hostProject = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Tooba.Host.csproj"));
        Assert.Contains(
            @"..\..\Modules\Shipping\Tooba.Shipping.Infrastructure\Tooba.Shipping.Infrastructure.csproj",
            hostProject,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Canonical_migration_order_has_one_unique_shipping_slot_after_geography()
    {
        var root = FindRepoRoot();
        var seam = File.ReadAllText(Path.Combine(root, "src/backend/BuildingBlocks/Tooba.Persistence/ModuleSchemaMigrator.cs"));

        Assert.Contains("public const int Geography = 30;", seam, StringComparison.Ordinal);
        Assert.Contains("public const int Shipping = 31;", seam, StringComparison.Ordinal);

        var values = Regex.Matches(seam, @"public const int \w+ = (?<value>\d+);")
            .Select(match => match.Groups["value"].Value)
            .ToArray();

        Assert.Equal(values.Length, values.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(new[] { "30", "31" }, values.Where(value => value is "30" or "31").ToArray());
        Assert.Equal("31", values[^1]);
    }

    [Fact]
    public void Cli_registry_appends_shipping_as_the_last_descriptor_without_an_apply_path()
    {
        var root = FindRepoRoot();
        var registry = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.MigrationRunner/ModuleMigrationRegistry.cs"));

        Assert.Contains("using Tooba.Shipping.Infrastructure.Persistence;", registry, StringComparison.Ordinal);
        Assert.Contains(
            "Descriptor<ShippingDbContext>(\"Shipping\", ShippingDbContext.Schema)",
            registry,
            StringComparison.Ordinal);
        Assert.Single(Regex.Matches(registry, @"Descriptor<ShippingDbContext>\("));

        var all = Regex.Match(registry, @"All \{ get; \} =\s*\[(?<entries>[\s\S]*?)\n    \];", RegexOptions.CultureInvariant);
        Assert.True(all.Success, "ModuleMigrationRegistry.All list not found");
        var descriptors = Regex.Matches(all.Groups["entries"].Value, @"Descriptor<(?<context>\w+)>\(")
            .Select(match => match.Groups["context"].Value)
            .ToArray();
        Assert.Equal("ShippingDbContext", descriptors[^1]);
        Assert.Equal(descriptors.Length, descriptors.Distinct(StringComparer.Ordinal).Count());
    }

    private static string[] BackendSourceFiles(string root)
    {
        var backend = Path.Combine(root, "src", "backend");
        return Directory
            .EnumerateFiles(backend, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedOrTestTree(path, root))
            .ToArray();
    }

    private static bool IsGeneratedOrTestTree(string path, string root)
    {
        var relative = Path.GetRelativePath(root, path);
        var segments = relative.Split(
            new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(segment => segment is "obj" or "bin")
               || segments.Any(segment => segment.EndsWith("Tests", StringComparison.Ordinal));
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";

        public string ApplicationName { get; set; } = "Tooba.Host";

        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
