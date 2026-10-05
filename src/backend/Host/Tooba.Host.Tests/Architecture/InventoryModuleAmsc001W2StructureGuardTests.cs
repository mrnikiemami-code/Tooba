using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-INVENTORY-AMSC-001-W2 — durable capability-first structure lock for Inventory: an
/// internal-only module with no Endpoints project, shallow capability trees, cohesive directory
/// partials, exact path↔namespace, root allowlists and solution grouping.
/// </summary>
public sealed class InventoryModuleAmsc001W2StructureGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Inventory";

    private static readonly string[] Projects =
    [
        "Tooba.Inventory.Application",
        "Tooba.Inventory.Contracts",
        "Tooba.Inventory.Domain",
        "Tooba.Inventory.Infrastructure",
        "Tooba.Inventory.Tests",
    ];

    /// <summary>INTERNAL_ONLY: no Endpoints project may ever appear for Inventory.</summary>
    [Fact]
    public void Inventory_remains_internal_only_with_no_endpoints_project()
    {
        Assert.False(Directory.Exists(Path.Combine(Repo(), ModuleRoot, "Tooba.Inventory.Endpoints")));
        Assert.False(Directory.Exists(Path.Combine(Repo(), "src", "backend", "Host", "Tooba.Host", "Inventory")));

        var production = Directory.EnumerateFiles(Path.Combine(Repo(), "src", "backend"), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains(".Tests" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(p => (Path: p, Text: File.ReadAllText(p)))
            .ToList();
        Assert.DoesNotContain(production, x => x.Text.Contains("MapInventoryEndpoints", StringComparison.Ordinal));

        using var state = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(Repo(), "docs", "architecture", "tmar-current-state.json")));
        var inventory = state.RootElement.GetProperty("completeReferenceModules").EnumerateArray()
            .Single(x => string.Equals(x.GetProperty("module").GetString(), "Inventory", StringComparison.Ordinal));
        Assert.Equal("INTERNAL_ONLY", inventory.GetProperty("httpApplicability").GetString());
        Assert.Equal("NOT_APPLICABLE", inventory.GetProperty("endpointOwnership").GetString());
        Assert.Equal("INTERNAL_USE_CASE_BOUNDARIES", inventory.GetProperty("cqrs").GetString());
    }

    /// <summary>Capability axis first — technical request folders must not be a top-level Application axis.</summary>
    [Fact]
    public void Inventory_Application_is_capability_first_not_technical_axis_first()
    {
        var app = Path.Combine(Repo(), ModuleRoot, "Tooba.Inventory.Application");
        var topLevel = Directory.GetDirectories(app)
            .Select(Path.GetFileName!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        foreach (var forbidden in new[] { "Commands", "Queries", "Models", "Validators" })
        {
            Assert.DoesNotContain(forbidden, topLevel);
        }

        foreach (var capability in new[] { "Checkout", "Composition", "Orders", "Ports" })
        {
            Assert.Contains(capability, topLevel);
        }

        Assert.Empty(Directory.GetFiles(app, "*.cs", SearchOption.TopDirectoryOnly));
    }

    /// <summary>Contracts owns boundary semantics only — no Application CQRS/persistence dump.</summary>
    [Fact]
    public void Inventory_Contracts_holds_boundary_semantics_only()
    {
        var contracts = Path.Combine(Repo(), ModuleRoot, "Tooba.Inventory.Contracts");
        var joined = string.Join("\n", Directory.GetFiles(contracts, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText));

        Assert.DoesNotContain("IRequest<", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("IRequestHandler", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("DbSet<", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("using Tooba.Inventory.Infrastructure", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Inventory.Domain", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Inventory.Application", joined, StringComparison.Ordinal);

        // The Contracts assembly must not reference the module Infrastructure (no persistence leak).
        var csproj = File.ReadAllText(Path.Combine(contracts, "Tooba.Inventory.Contracts.csproj"));
        Assert.DoesNotContain("Tooba.Inventory.Infrastructure", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Inventory.Application", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Inventory.Domain", csproj, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(contracts, "Errors", "InventoryErrorCodes.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Errors", "InventoryErrorCatalogContributor.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Errors", "InventoryErrorResourceSet.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Resources", "InventoryErrors.resx")));
        Assert.True(File.Exists(Path.Combine(contracts, "Resources", "InventoryErrors.fa.resx")));
    }

    /// <summary>Exactly one canonical stable-code owner — no Domain-local code class.</summary>
    [Fact]
    public void Inventory_has_single_stable_error_code_owner()
    {
        var domain = Path.Combine(Repo(), ModuleRoot, "Tooba.Inventory.Domain");
        var joined = string.Join("\n", Directory.GetFiles(domain, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText));

        // Domain consumes the canonical Contracts-owned codes but must not declare any code constant.
        Assert.DoesNotContain("public const string", joined, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(
            Repo(), ModuleRoot, "Tooba.Inventory.Contracts", "Errors", "InventoryErrorCodes.cs")));
    }

    /// <summary>The cohesive directory split must not regress into one multi-responsibility god file.</summary>
    [Fact]
    public void Inventory_directory_stays_cohesive_partials_with_guard_in_own_file()
    {
        var directories = Path.Combine(Repo(), ModuleRoot, "Tooba.Inventory.Infrastructure", "Directories");

        foreach (var partial in new[]
                 {
                     "InventoryDirectory.cs",
                     "InventoryDirectory.Availability.cs",
                     "InventoryDirectory.Lookups.cs",
                     "InventoryDirectory.OrderSupply.cs",
                     "InventoryDirectory.Reclaimer.cs",
                     "InventoryDirectory.SellerWrite.cs",
                     "OpenInventoryUseCaseGuard.cs",
                 })
        {
            Assert.True(File.Exists(Path.Combine(directories, partial)), $"missing {partial}");
        }

        foreach (var file in Directory.GetFiles(directories, "*.cs", SearchOption.TopDirectoryOnly))
        {
            var loc = File.ReadLines(file).Count();
            Assert.True(loc <= 500, $"{Path.GetFileName(file)} regressed to {loc} LOC (cohesion ceiling 500)");
        }

        // The guard type has its own file; the directory file declares exactly one type.
        var directoryFile = File.ReadAllText(Path.Combine(directories, "InventoryDirectory.cs"));
        Assert.DoesNotContain("class OpenInventoryUseCaseGuard", directoryFile, StringComparison.Ordinal);
        Assert.Contains("partial class InventoryDirectory", directoryFile, StringComparison.Ordinal);
    }

    /// <summary>Application ports are one capability per file — no mixed result/port dump.</summary>
    [Fact]
    public void Inventory_application_ports_are_one_capability_per_file()
    {
        var ports = Path.Combine(Repo(), ModuleRoot, "Tooba.Inventory.Application", "Ports");
        Assert.False(File.Exists(Path.Combine(ports, "InventoryDirectoryPorts.cs")));

        foreach (var file in new[] { "IInventoryDirectory.cs", "IInventoryUseCaseGuard.cs", "ReservationReceipt.cs" })
        {
            Assert.True(File.Exists(Path.Combine(ports, file)), $"missing {file}");
            var text = File.ReadAllText(Path.Combine(ports, file));
            var declarations = Regex.Matches(text, @"^(?:public|internal)\s+(?:sealed\s+)?(?:interface|record|class|enum)\s", RegexOptions.Multiline).Count;
            Assert.True(declarations == 1, $"{file} declares {declarations} types (expected exactly 1)");
        }
    }

    /// <summary>Infrastructure uses the canonical capability folders; the root has no loose module files.</summary>
    [Fact]
    public void Inventory_Infrastructure_uses_canonical_capability_folders()
    {
        var infra = Path.Combine(Repo(), ModuleRoot, "Tooba.Inventory.Infrastructure");
        Assert.True(File.Exists(Path.Combine(infra, "DependencyInjection", "InventoryModule.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Directories", "InventoryDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Messaging", "InventoryOutboxRegistration.cs")));
        Assert.True(File.Exists(Path.Combine(
            infra, "Persistence", "Migrations", "InventoryDbContextModelSnapshot.cs")));
        Assert.False(Directory.Exists(Path.Combine(infra, "Migrations")));
        Assert.Empty(Directory.GetFiles(infra, "*.cs", SearchOption.TopDirectoryOnly));

        var allowed = new[] { "Adapters", "DependencyInjection", "Directories", "Events", "Messaging", "Persistence" };
        var actual = Directory.GetDirectories(infra)
            .Select(Path.GetFileName!)
            .Where(n => n is not ("bin" or "obj"))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(allowed, actual);
    }

    [Fact]
    public void Inventory_root_allowlists_match_disk_and_forbidden_roots_absent()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Repo(), "docs/architecture/tmar-module-structure-manifests.json")));
        // Inventory is not ARCH-COMPLETE-002 certified yet, so its structure record lives in the
        // preCertModules array (never in the certified modules array until W3).
        Assert.DoesNotContain(
            doc.RootElement.GetProperty("modules").EnumerateArray(),
            m => string.Equals(m.GetProperty("module").GetString(), "Inventory", StringComparison.Ordinal));
        var entry = doc.RootElement.GetProperty("preCertModules").EnumerateArray()
            .Single(m => string.Equals(m.GetProperty("module").GetString(), "Inventory", StringComparison.Ordinal));
        Assert.False(entry.GetProperty("structureCertified").GetBoolean());

        foreach (var project in entry.GetProperty("projects").EnumerateArray())
        {
            var projectName = project.GetProperty("projectName").GetString()!;
            var projectPath = Path.Combine(Repo(), ModuleRoot, projectName);
            Assert.True(Directory.Exists(projectPath), projectPath);

            var allowlist = project.GetProperty("rootAllowlist").EnumerateArray()
                .Select(x => x.GetString()!).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var actual = Directory.GetFiles(projectPath, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName!).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            Assert.Equal(allowlist, actual);

            foreach (var forbidden in project.GetProperty("forbiddenRootFiles").EnumerateArray())
            {
                Assert.False(File.Exists(Path.Combine(projectPath, forbidden.GetString()!)));
            }

            foreach (var forbidden in project.GetProperty("forbiddenTopLevelFolders").EnumerateArray())
            {
                Assert.False(Directory.Exists(Path.Combine(projectPath, forbidden.GetString()!)));
            }
        }
    }

    [Fact]
    public void Inventory_production_path_equals_namespace_exactly()
    {
        var violations = new List<string>();
        foreach (var project in Projects)
        {
            var projectPath = Path.Combine(Repo(), ModuleRoot, project);
            var rootFull = Path.GetFullPath(projectPath);
            foreach (var file in Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories))
            {
                var relative = file[rootFull.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (relative.StartsWith($"obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || relative.StartsWith($"bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || Path.GetFileName(relative).StartsWith("GlobalUsings", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var dir = Path.GetDirectoryName(relative);
                var expected = string.IsNullOrEmpty(dir)
                    ? project
                    : project + "." + dir.Replace(Path.DirectorySeparatorChar, '.').Replace(Path.AltDirectorySeparatorChar, '.');
                var match = Regex.Match(File.ReadAllText(file), @"^namespace\s+([A-Za-z0-9_.]+)", RegexOptions.Multiline);
                if (!match.Success || !string.Equals(expected, match.Groups[1].Value, StringComparison.Ordinal))
                {
                    violations.Add($"{relative}: expected {expected}, got {(match.Success ? match.Groups[1].Value : "<none>")}");
                }
            }
        }

        Assert.True(violations.Count == 0, string.Join("\n", violations));
    }

    [Fact]
    public void Inventory_projects_grouped_under_Modules_Inventory_solution_folder()
    {
        var slnx = Path.Combine(Repo(), "src", "backend", "Tooba.slnx");
        var doc = XDocument.Load(slnx);
        var folder = doc.Root!.Elements("Folder").SingleOrDefault(f =>
            string.Equals((string?)f.Attribute("Name"), "/Modules/Inventory/", StringComparison.Ordinal));
        Assert.True(folder is not null, "missing /Modules/Inventory/ solution folder");

        var nested = folder!.Elements("Project")
            .Select(p => Path.GetFileNameWithoutExtension((string)p.Attribute("Path")!))
            .OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(Projects.OrderBy(x => x, StringComparer.Ordinal).ToArray(), nested);
    }

    private static string Repo()
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
}
