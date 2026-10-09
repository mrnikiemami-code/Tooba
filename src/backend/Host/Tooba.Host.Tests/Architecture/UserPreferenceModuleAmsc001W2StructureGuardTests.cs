using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-USERPREFERENCE-AMSC-001-W2 — scoped structure gate (tooba-architecture-structure).
/// Pins the capability-first shallow physical tree, the exact path↔namespace rule for all five
/// production projects, the manifest root allowlists/forbidden lists, the canonical
/// <c>/Modules/UserPreference/</c> solution grouping and the absence of stale/duplicate physical
/// copies of the Contracts localization surface. Behavior is unchanged by W2; this guard only locks
/// the physical shape and moves the module's structure authority to
/// <c>TB-TMAR-USERPREFERENCE-AMSC-001-W2</c>.
/// </summary>
public sealed class UserPreferenceModuleAmsc001W2StructureGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/UserPreference";
    private const string ManifestPath = "docs/architecture/tmar-module-structure-manifests.json";

    private static readonly string[] Projects =
    [
        "Tooba.UserPreference.Application",
        "Tooba.UserPreference.Contracts",
        "Tooba.UserPreference.Domain",
        "Tooba.UserPreference.Endpoints",
        "Tooba.UserPreference.Infrastructure",
    ];

    private static readonly string[] TopLevelNoise = ["bin", "obj", "artifacts"];

    [Fact]
    public void Contracts_is_capability_first_with_no_root_dump()
    {
        var contracts = Path.Combine(Repo(), ModuleRoot, "Tooba.UserPreference.Contracts");

        Assert.Empty(Directory.GetFiles(contracts, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Equal(
            new[] { "Errors", "Resources" },
            Directories(contracts));

        // The retired defect: a stable-code dump at the Contracts root.
        Assert.False(File.Exists(Path.Combine(contracts, "UserPreferenceErrorCodes.cs")),
            "Contracts root must not resurrect the stable-code dump");
        Assert.True(File.Exists(Path.Combine(contracts, "Errors", "UserPreferenceErrorCodes.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Errors", "UserPreferenceErrorCatalogContributor.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Errors", "UserPreferenceErrorResourceSet.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Resources", "UserPreferenceErrors.resx")));
        Assert.True(File.Exists(Path.Combine(contracts, "Resources", "UserPreferenceErrors.fa.resx")));
    }

    [Fact]
    public void Domain_is_capability_first_with_no_root_dump()
    {
        var domain = Path.Combine(Repo(), ModuleRoot, "Tooba.UserPreference.Domain");

        Assert.Empty(Directory.GetFiles(domain, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Equal(new[] { "Aggregates" }, Directories(domain));

        Assert.False(File.Exists(Path.Combine(domain, "UserPreference.cs")));
        Assert.False(File.Exists(Path.Combine(domain, "UiPreference.cs")));
        Assert.False(Directory.Exists(Path.Combine(domain, "Errors")),
            "Domain/Errors must stay retired; the canonical home is Contracts/Errors");
        Assert.True(File.Exists(Path.Combine(domain, "Aggregates", "UserPreference.cs")));
        Assert.True(File.Exists(Path.Combine(domain, "Aggregates", "UiPreference.cs")));
    }

    [Fact]
    public void Application_is_capability_first_and_never_technical_axis_first()
    {
        var app = Path.Combine(Repo(), ModuleRoot, "Tooba.UserPreference.Application");

        Assert.Empty(Directory.GetFiles(app, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Equal(
            new[] { "Composition", "LocalePreferences", "Models", "Ports", "UiPreferences" },
            Directories(app));

        // No technical-axis-first request tree at the Application root.
        foreach (var forbidden in new[] { "Commands", "Queries", "Validators", "Presentation" })
        {
            Assert.False(Directory.Exists(Path.Combine(app, forbidden)),
                $"Application/{forbidden} must not exist (technical-axis-first tree)");
        }

        // Capability-first request trees.
        Assert.Equal(new[] { "Commands", "Queries", "Validators" }, Directories(Path.Combine(app, "LocalePreferences")));
        Assert.Equal(new[] { "Commands", "Queries", "Validators" }, Directories(Path.Combine(app, "UiPreferences")));

        Assert.True(File.Exists(Path.Combine(app, "Composition", "UserPreferenceOperation.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Models", "UserPreferenceModels.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Ports", "IUserPreferenceDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Ports", "IUiPreferenceDirectory.cs")));
        Assert.False(File.Exists(Path.Combine(app, "UserPreferenceContracts.cs")));
        Assert.False(File.Exists(Path.Combine(app, "UserPreferenceShapes.cs")));
    }

    [Fact]
    public void Endpoints_root_holds_only_the_composition_entry_and_owns_no_localization_surface()
    {
        var endpoints = Path.Combine(Repo(), ModuleRoot, "Tooba.UserPreference.Endpoints");

        Assert.Equal(
            new[] { "UserPreferenceEndpointModule.cs" },
            Directory.GetFiles(endpoints, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName!)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(new[] { "Admin", "Customer" }, Directories(endpoints));

        // The stable-code home and its bilingual resources are Contracts-owned, never duplicated here.
        Assert.False(Directory.Exists(Path.Combine(endpoints, "Errors")));
        Assert.False(Directory.Exists(Path.Combine(endpoints, "Resources")));
        Assert.False(File.Exists(Path.Combine(endpoints, "UserPreferenceCustomerEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(endpoints, "UserPreferenceAdminEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(endpoints, "UiPreferenceAdminEndpoints.cs")));

        Assert.True(File.Exists(Path.Combine(endpoints, "Customer", "UserPreferenceCustomerEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(endpoints, "Customer", "UserPreferenceCustomerActorResolver.cs")));
        Assert.True(File.Exists(Path.Combine(endpoints, "Admin", "UserPreferenceAdminEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(endpoints, "Admin", "UiPreferenceAdminEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(endpoints, "Admin", "IUserPreferenceAdminAuthorizer.cs")));
    }

    [Fact]
    public void Infrastructure_root_holds_only_the_composition_entry_with_migrations_under_persistence()
    {
        var infra = Path.Combine(Repo(), ModuleRoot, "Tooba.UserPreference.Infrastructure");

        Assert.Equal(
            new[] { "UserPreferenceModule.cs" },
            Directory.GetFiles(infra, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName!)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(new[] { "Development", "Directories", "Persistence" }, Directories(infra));

        Assert.False(Directory.Exists(Path.Combine(infra, "Migrations")));
        Assert.True(Directory.Exists(Path.Combine(infra, "Persistence", "Migrations")));
        Assert.False(File.Exists(Path.Combine(infra, "UserPreferenceDirectory.cs")));
        Assert.False(File.Exists(Path.Combine(infra, "UiPreferenceDirectory.cs")));
        Assert.False(File.Exists(Path.Combine(infra, "UserPreferenceDbContext.cs")));
        Assert.False(File.Exists(Path.Combine(infra, "UserPreferenceOutboxRegistration.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Directories", "UserPreferenceDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Directories", "UiPreferenceDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Persistence", "UserPreferenceDbContext.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Persistence", "UserPreferenceOutboxRegistration.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Development", "UserPreferenceDevelopmentSeed.cs")));
    }

    [Fact]
    public void Path_and_namespace_are_exact_across_every_production_file()
    {
        var root = Path.Combine(Repo(), ModuleRoot.Replace('/', Path.DirectorySeparatorChar));
        var mismatches = new List<string>();

        foreach (var project in Projects)
        {
            var projectRoot = Path.Combine(root, project);
            foreach (var file in Directory.EnumerateFiles(projectRoot, "*.cs", SearchOption.AllDirectories)
                         .Where(p => !TopLevelNoise.Any(n =>
                             p.Contains($"{Path.DirectorySeparatorChar}{n}{Path.DirectorySeparatorChar}", StringComparison.Ordinal))))
            {
                var relative = Path.GetRelativePath(projectRoot, file).Replace('\\', '/');
                var directory = Path.GetDirectoryName(relative)!.Replace('\\', '/');
                var expected = directory.Length == 0 ? project : $"{project}.{directory.Replace('/', '.')}";

                var text = File.ReadAllText(file).TrimStart('\uFEFF');
                var match = Regex.Match(text, @"^namespace\s+([A-Za-z0-9_.]+)", RegexOptions.Multiline);
                if (!match.Success || match.Groups[1].Value != expected)
                {
                    mismatches.Add($"{project}/{relative} got={match.Groups[1].Value switch { "" => "(none)", var g => g }} expected={expected}");
                }
            }
        }

        Assert.True(mismatches.Count == 0, string.Join("\n", mismatches));
    }

    [Fact]
    public void Solution_has_no_extra_or_missing_userpreference_project()
    {
        var slnx = File.ReadAllText(Path.Combine(Repo(), "src/backend/Tooba.slnx"));

        Assert.Contains("<Folder Name=\"/Modules/UserPreference/\">", slnx, StringComparison.Ordinal);
        var group = Regex.Match(
            slnx,
            "<Folder Name=\"/Modules/UserPreference/\">(?<body>.*?)</Folder>",
            RegexOptions.Singleline).Groups["body"].Value;

        var slnxProjects = Regex.Matches(group, "Path=\"[^\"]*/([^/\"]+)\\.csproj\"")
            .Select(m => m.Groups[1].Value)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(Projects.OrderBy(x => x, StringComparer.Ordinal).ToArray(), slnxProjects);

        // Disk and solution agree exactly: five projects, no extra project directory.
        Assert.Equal(
            Projects.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            Directory.GetDirectories(Path.Combine(Repo(), ModuleRoot.Replace('/', Path.DirectorySeparatorChar)))
                .Select(Path.GetFileName!)
                .Where(n => n.StartsWith("Tooba.UserPreference.", StringComparison.Ordinal))
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
    }

    [Fact]
    public void Manifest_records_the_userpreference_structure_authority_and_allowlists()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Repo(), ManifestPath)));
        var entry = doc.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => m.GetProperty("module").GetString() == "UserPreference");

        Assert.True(entry.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entry.GetProperty("lockVersion").GetString());
        Assert.Equal("TB-TMAR-USERPREFERENCE-AMSC-001-W2", entry.GetProperty("structureAuthorityTask").GetString());
        Assert.Equal("READY_FOR_CERTIFY_CONSUMED_BY_W3", entry.GetProperty("structureHandoffState").GetString());

        var projects = entry.GetProperty("projects").EnumerateArray().ToArray();
        Assert.Equal(Projects.Length, projects.Length);
        Assert.Equal(
            Projects.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            projects.Select(p => p.GetProperty("projectName").GetString()!)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());

        foreach (var project in projects)
        {
            // Every project pins an explicit root allowlist (possibly empty) with a justification, plus
            // an explicit forbidden root-file and forbidden top-level-folder list.
            Assert.True(project.TryGetProperty("rootAllowlist", out _), project.GetProperty("projectName").GetString());
            Assert.False(string.IsNullOrWhiteSpace(project.GetProperty("rootAllowlistJustification").GetString()));
            Assert.True(project.TryGetProperty("forbiddenRootFiles", out _));
            Assert.True(project.TryGetProperty("forbiddenTopLevelFolders", out _));
        }

        // The Contracts allowlist must keep the retired stable-code root dump forbidden.
        var contracts = projects.Single(p => p.GetProperty("projectName").GetString() == "Tooba.UserPreference.Contracts");
        Assert.Empty(contracts.GetProperty("rootAllowlist").EnumerateArray());
        Assert.Contains(
            contracts.GetProperty("forbiddenRootFiles").EnumerateArray().Select(x => x.GetString()),
            x => x == "UserPreferenceErrorCodes.cs");

        // Endpoints/Infrastructure keep exactly one allowed root composition file.
        var endpoints = projects.Single(p => p.GetProperty("projectName").GetString() == "Tooba.UserPreference.Endpoints");
        Assert.Equal(
            new[] { "UserPreferenceEndpointModule.cs" },
            endpoints.GetProperty("rootAllowlist").EnumerateArray().Select(x => x.GetString()!).ToArray());
        var infra = projects.Single(p => p.GetProperty("projectName").GetString() == "Tooba.UserPreference.Infrastructure");
        Assert.Equal(
            new[] { "UserPreferenceModule.cs" },
            infra.GetProperty("rootAllowlist").EnumerateArray().Select(x => x.GetString()!).ToArray());
    }

    [Fact]
    public void Module_has_no_stale_or_duplicate_physical_copies_of_the_localization_surface()
    {
        var root = Path.Combine(Repo(), ModuleRoot.Replace('/', Path.DirectorySeparatorChar));
        var codeHomes = Directory.EnumerateFiles(root, "UserPreferenceErrorCodes.cs", SearchOption.AllDirectories)
            .Where(p => !TopLevelNoise.Any(n =>
                p.Contains($"{Path.DirectorySeparatorChar}{n}{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
            .ToArray();
        Assert.Single(codeHomes);

        var resx = Directory.EnumerateFiles(root, "UserPreferenceErrors*.resx", SearchOption.AllDirectories)
            .Where(p => !TopLevelNoise.Any(n =>
                p.Contains($"{Path.DirectorySeparatorChar}{n}{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
            .Select(Path.GetFileName!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "UserPreferenceErrors.fa.resx", "UserPreferenceErrors.resx" }, resx);

        var contributorHomes = Directory.EnumerateFiles(root, "UserPreferenceErrorCatalogContributor.cs", SearchOption.AllDirectories)
            .Where(p => !TopLevelNoise.Any(n =>
                p.Contains($"{Path.DirectorySeparatorChar}{n}{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
            .ToArray();
        Assert.Single(contributorHomes);
    }

    private static string[] Directories(string path) =>
        Directory.GetDirectories(path)
            .Select(Path.GetFileName!)
            .Where(n => !TopLevelNoise.Contains(n, StringComparer.Ordinal))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

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
