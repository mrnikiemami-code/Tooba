using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W2 (Structure) — durable structure lock: capability-first shallow
/// tree, exact path↔namespace, enforced root allowlists, zero <c>.gitkeep</c> ceremony, canonical
/// solution grouping and a declared empty Domain boundary project justified by the manifest.
/// </summary>
public sealed class ProductWorkspaceModuleAmsc001W2StructureGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/ProductWorkspace";
    private const string App = ModuleRoot + "/Tooba.ProductWorkspace.Application";
    private const string Domain = ModuleRoot + "/Tooba.ProductWorkspace.Domain";
    private const string Infra = ModuleRoot + "/Tooba.ProductWorkspace.Infrastructure";
    private const string Endpoints = ModuleRoot + "/Tooba.ProductWorkspace.Endpoints";
    private const string Contracts = ModuleRoot + "/Tooba.ProductWorkspace.Contracts";
    private const string Capability = App + "/Composition/ProductManagement";

    private static readonly string[] Projects =
    [
        "Tooba.ProductWorkspace.Contracts",
        "Tooba.ProductWorkspace.Domain",
        "Tooba.ProductWorkspace.Application",
        "Tooba.ProductWorkspace.Infrastructure",
        "Tooba.ProductWorkspace.Endpoints",
    ];

    [Fact]
    public void ProductWorkspace_path_namespace_alignment_is_exact()
    {
        var root = Repo();
        foreach (var project in Projects)
        {
            var projectPath = Path.Combine(root, ModuleRoot, project);
            var projectFull = Path.GetFullPath(projectPath);
            foreach (var file in Directory.EnumerateFiles(projectPath, "*.cs", SearchOption.AllDirectories)
                         .Where(f => !IsBuildOutput(f)))
            {
                var relative = file[projectFull.Length..]
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var directory = Path.GetDirectoryName(relative);
                var expected = string.IsNullOrEmpty(directory)
                    ? project
                    : project + "." + directory
                        .Replace(Path.DirectorySeparatorChar, '.')
                        .Replace(Path.AltDirectorySeparatorChar, '.');

                Assert.Equal(expected, DeclaredNamespace(file));
            }
        }
    }

    [Fact]
    public void ProductWorkspace_is_capability_first_shallow_without_technical_axis_roots_or_use_case_leaf_folders()
    {
        var root = Repo();

        // No technical axis may be a top-level Application root (Commands/Queries/Models/Ports/Validators/Grid).
        foreach (var banned in new[] { "Commands", "Queries", "Models", "Ports", "Validators", "Grid" })
        {
            Assert.False(
                Directory.Exists(Path.Combine(root, App, banned)),
                $"Application/{banned} must not be a top-level technical axis for a capability-first module.");
        }

        // Application root holds a single shared Composition folder; Composition holds the one capability.
        Assert.Equal(
            ["Composition"],
            TopLevelFolders(Path.Combine(root, App)));
        Assert.Equal(
            ["ProductManagement"],
            TopLevelFolders(Path.Combine(root, App, "Composition")));

        // The capability carries the CQRS request axes; Composition keeps only the typed-fault seam.
        Assert.Equal(
            ["Commands", "Grid", "Models", "Queries"],
            TopLevelFolders(Path.Combine(root, App, "Composition", "ProductManagement")));
        Assert.Equal(
            ["ProductWorkspaceOperation.cs"],
            Directory.EnumerateFiles(Path.Combine(root, App, "Composition"), "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal).ToArray());

        // No use-case-named leaf folder wrapping a single production source file.
        var offenders = Directory.EnumerateDirectories(Path.Combine(root, App), "*", SearchOption.AllDirectories)
            .Where(dir => !IsBuildOutput(dir))
            .Where(dir => Directory.EnumerateFiles(dir, "*.cs", SearchOption.TopDirectoryOnly).Count() == 1)
            .Select(Path.GetFileName)
            .Where(name => name.StartsWith("Create", StringComparison.Ordinal)
                           || name.StartsWith("Update", StringComparison.Ordinal)
                           || name.StartsWith("Get", StringComparison.Ordinal)
                           || name.StartsWith("List", StringComparison.Ordinal)
                           || name.StartsWith("Query", StringComparison.Ordinal)
                           || name.StartsWith("Assign", StringComparison.Ordinal)
                           || name.StartsWith("Publish", StringComparison.Ordinal)
                           || name.StartsWith("Unpublish", StringComparison.Ordinal)
                           || name.StartsWith("Archive", StringComparison.Ordinal)
                           || name.StartsWith("Restore", StringComparison.Ordinal)
                           || name.StartsWith("Patch", StringComparison.Ordinal)
                           || name.StartsWith("Add", StringComparison.Ordinal)
                           || name.StartsWith("Remove", StringComparison.Ordinal))
            .ToArray();
        Assert.Empty(offenders);
    }

    [Fact]
    public void ProductWorkspace_root_allowlists_are_enforced_and_no_gitkeep_ceremony_remains()
    {
        var root = Repo();

        // F5 closed: the .gitkeep decoration and the empty ceremonial shells are gone.
        Assert.Empty(Directory.EnumerateFiles(Path.Combine(root, ModuleRoot), ".gitkeep", SearchOption.AllDirectories)
            .Where(f => !IsBuildOutput(f)).ToArray());
        foreach (var retired in new[]
                 {
                     "Commands", "Queries", "Models", "Ports", "Validators", "Grid",
                 })
        {
            Assert.False(
                Directory.Exists(Path.Combine(root, App, "Composition", retired)),
                $"retired Application/Composition/{retired} shell must not return");
        }

        Assert.Empty(Directory.EnumerateFiles(Path.Combine(root, App), "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(Path.Combine(root, Domain), "*.cs", SearchOption.TopDirectoryOnly));

        // Root allowlists match reality exactly for every project.
        Assert.Equal(
            ["ProductWorkspaceContractsMarker.cs"],
            RootCs(root, Contracts));
        Assert.Equal(
            ["ProductWorkspaceModule.cs"],
            RootCs(root, Infra));
        Assert.Equal(
            ["ProductWorkspaceEndpointModule.cs"],
            RootCs(root, Endpoints));

        // No stale/duplicate copy of the retired flat Application layout.
        foreach (var moved in new[]
                 {
                     "ProductWorkspaceModels.cs", "AdminProductGridQueryPolicy.cs", "GetProductWorkspaceHandler.cs",
                     "ListProductWorkspaceQuery.cs", "QueryProductWorkspaceGridQuery.cs",
                     "CreateWorkspaceProductCommand.cs",
                 })
        {
            Assert.False(
                File.Exists(Path.Combine(root, App, "Composition", moved)),
                $"{moved} must live under the ProductManagement capability, not on Composition");
        }

        // Single authoritative home per responsibility inside the capability.
        Assert.True(File.Exists(Path.Combine(root, Capability, "Models", "ProductWorkspaceModels.cs")));
        Assert.True(File.Exists(Path.Combine(root, Capability, "Grid", "AdminProductGridQueryPolicy.cs")));
        Assert.True(File.Exists(Path.Combine(root, Capability, "Queries", "GetProductWorkspaceHandler.cs")));
        Assert.True(File.Exists(Path.Combine(root, Capability, "Queries", "ProductWorkspaceListComposer.cs")));
        Assert.Equal(14, Directory.EnumerateFiles(Path.Combine(root, Capability, "Commands"), "*.cs").Count());
    }

    [Fact]
    public void ProductWorkspace_solution_explorer_grouping_is_canonical()
    {
        var root = Repo();
        var slnx = File.ReadAllText(Path.Combine(root, "src/backend/Tooba.slnx"));
        Assert.Contains("<Folder Name=\"/Modules/ProductWorkspace/\">", slnx, StringComparison.Ordinal);
        foreach (var project in Projects)
        {
            Assert.Contains(
                $"Modules/ProductWorkspace/{project}/{project}.csproj",
                slnx,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ProductWorkspace_domain_stays_a_declared_empty_boundary_project()
    {
        var root = Repo();

        // F4 closed by declaration: the composition module owns no domain types, so Domain stays an
        // explicitly justified empty boundary assembly instead of being silently filled or removed.
        var csprojPath = Path.Combine(root, Domain, "Tooba.ProductWorkspace.Domain.csproj");
        Assert.True(File.Exists(csprojPath));
        Assert.Empty(Directory.EnumerateFiles(Path.Combine(root, Domain), "*.cs", SearchOption.AllDirectories)
            .Where(f => !IsBuildOutput(f)).ToArray());
        Assert.Empty(XDocument.Load(csprojPath).Descendants("ProjectReference"));

        // The declared-empty justification is recorded in the manifest, not invented at guard time.
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));
        var entry = CertifiedEntry(manifest, "ProductWorkspace");
        var domain = entry.GetProperty("projects").EnumerateArray()
            .Single(p => p.GetProperty("projectName").GetString() == "Tooba.ProductWorkspace.Domain");
        Assert.Empty(domain.GetProperty("rootAllowlist").EnumerateArray());
        Assert.Contains(
            "explicitly justified empty boundary assembly",
            domain.GetProperty("rootAllowlistJustification").GetString()!,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductWorkspace_manifest_structure_allowlists_match_disk()
    {
        var root = Repo();
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));

        // ProductWorkspace was promoted by TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3 from preCertModules to the
        // certified modules array (structureCertified true). The structural allowlists below are unchanged.
        var entry = CertifiedEntry(manifest, "ProductWorkspace");
        Assert.True(entry.GetProperty("structureCertified").GetBoolean());
        Assert.DoesNotContain(
            manifest.RootElement.GetProperty("preCertModules").EnumerateArray(),
            m => m.GetProperty("module").GetString() == "ProductWorkspace");
        Assert.Equal("ARCH-COMPLETE-002", entry.GetProperty("lockVersion").GetString());
        Assert.Equal(
            Projects.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            entry.GetProperty("projects").EnumerateArray()
                .Select(p => p.GetProperty("projectName").GetString()!)
                .OrderBy(x => x, StringComparer.Ordinal).ToArray());

        foreach (var project in entry.GetProperty("projects").EnumerateArray())
        {
            var projectName = project.GetProperty("projectName").GetString()!;
            var projectPath = Path.Combine(root, ModuleRoot, projectName);

            Assert.Equal(
                project.GetProperty("rootAllowlist").EnumerateArray().Select(x => x.GetString()!)
                    .OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                RootCs(root, projectPath));

            foreach (var forbidden in project.GetProperty("forbiddenRootFiles").EnumerateArray())
            {
                Assert.False(
                    File.Exists(Path.Combine(projectPath, forbidden.GetString()!)),
                    $"{projectName} still has forbidden root file {forbidden.GetString()}");
            }

            foreach (var forbidden in project.GetProperty("forbiddenTopLevelFolders").EnumerateArray())
            {
                Assert.False(
                    Directory.Exists(Path.Combine(projectPath, forbidden.GetString()!)),
                    $"{projectName} still has forbidden top-level folder {forbidden.GetString()}");
            }
        }

        // The Application allowlist must forbid the retired technical-axis roots, not just omit them.
        var app = entry.GetProperty("projects").EnumerateArray()
            .Single(p => p.GetProperty("projectName").GetString() == "Tooba.ProductWorkspace.Application");
        Assert.Equal(
            ["Commands", "Grid", "Models", "Ports", "Queries", "Validators"],
            app.GetProperty("forbiddenTopLevelFolders").EnumerateArray().Select(x => x.GetString()!)
                .OrderBy(x => x, StringComparer.Ordinal).ToArray());
    }

    private static JsonElement CertifiedEntry(JsonDocument manifest, string module) =>
        manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => m.GetProperty("module").GetString() == module);

    private static string[] RootCs(string root, string projectRelativePath) =>
        Directory.EnumerateFiles(Path.Combine(root, projectRelativePath), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

    private static string[] TopLevelFolders(string absolutePath) =>
        Directory.EnumerateDirectories(absolutePath)
            .Where(dir => !IsBuildOutput(dir))
            .Select(Path.GetFileName!)
            .Where(name => !string.Equals(name, "bin", StringComparison.OrdinalIgnoreCase)
                           && !string.Equals(name, "obj", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

    private static string DeclaredNamespace(string file)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            File.ReadAllText(file).TrimStart('\uFEFF'),
            @"^namespace\s+([A-Za-z0-9_.]+)",
            System.Text.RegularExpressions.RegexOptions.Multiline);
        Assert.True(match.Success, $"no namespace declaration in {file}");
        return match.Groups[1].Value;
    }

    private static bool IsBuildOutput(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

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
