using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-OPERATORPROFILE-AMSC-001-W2 — durable structure gate under the current
/// <c>tooba-architecture-structure</c> skill: capability-first shallow tree, zero per-use-case
/// request leaf folders, exact path↔namespace, root allowlists, canonical solution grouping,
/// Contracts-only Endpoints imports and Host final-closure preservation.
/// </summary>
public sealed class OperatorProfileModuleAmsc001W2StructureGuardTests
{
    private const string Module = "OperatorProfile";

    [Fact]
    public void Application_tree_is_capability_first_with_zero_per_use_case_leaf_folders()
    {
        var app = Project("Application");

        // Capability-first: Admin capability carries the request axes; shared Composition/Models/Ports.
        Assert.True(Directory.Exists(Path.Combine(app, "Admin", "Commands")));
        Assert.True(Directory.Exists(Path.Combine(app, "Admin", "Queries")));
        Assert.True(Directory.Exists(Path.Combine(app, "Admin", "Validators")));
        Assert.True(Directory.Exists(Path.Combine(app, "Composition")));
        Assert.True(Directory.Exists(Path.Combine(app, "Models")));
        Assert.True(Directory.Exists(Path.Combine(app, "Ports")));

        // No technical-axis-first roots and no per-use-case child leaves under any request axis.
        Assert.False(Directory.Exists(Path.Combine(app, "Commands")));
        Assert.False(Directory.Exists(Path.Combine(app, "Queries")));
        Assert.False(Directory.Exists(Path.Combine(app, "Validators")));
        foreach (var axis in new[] { "Commands", "Queries", "Validators" })
        {
            foreach (var child in Directory.EnumerateDirectories(Path.Combine(app, "Admin", axis)))
            {
                Assert.Fail($"per-use-case leaf folder is OVER_FOLDERED: {child}");
            }
        }

        // Request axes carry exactly the certified request/validator files directly (no subfolders).
        Assert.Equal(
            new[] { "UpsertOperatorProfileCommand.cs" },
            Directory.EnumerateFiles(Path.Combine(app, "Admin", "Commands"), "*.cs").Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.Equal(
            new[] { "GetOperatorProfileQuery.cs" },
            Directory.EnumerateFiles(Path.Combine(app, "Admin", "Queries"), "*.cs").Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.Equal(
            new[] { "GetOperatorProfileQueryValidator.cs", "UpsertOperatorProfileCommandValidator.cs" },
            Directory.EnumerateFiles(Path.Combine(app, "Admin", "Validators"), "*.cs").Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void Project_roots_match_manifest_allowlists_and_have_no_stale_copies()
    {
        var root = RepoRoot();
        var manifestPath = Path.Combine(root, "docs", "architecture", "tmar-module-structure-manifests.json");
        var raw = File.ReadAllText(manifestPath);
        using var doc = System.Text.Json.JsonDocument.Parse(raw.Replace("\uFEFF", string.Empty));
        var module = doc.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => m.GetProperty("module").GetString() == Module);

        foreach (var project in module.GetProperty("projects").EnumerateArray())
        {
            var projectName = project.GetProperty("projectName").GetString()!;
            var projectPath = Project(projectName.Split('.')[2]);
            Assert.True(Directory.Exists(projectPath), $"missing {projectName}");

            var allowlist = project.GetProperty("rootAllowlist").EnumerateArray()
                .Select(x => x.GetString()!).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var actualRoot = Directory.GetFiles(projectPath, "*.cs", System.IO.SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName!).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            Assert.Equal(allowlist, actualRoot);

            foreach (var forbidden in project.GetProperty("forbiddenRootFiles").EnumerateArray())
            {
                Assert.False(File.Exists(Path.Combine(projectPath, forbidden.GetString()!)), $"{projectName} resurrected forbidden root file {forbidden}");
            }

            foreach (var forbiddenFolder in project.GetProperty("forbiddenTopLevelFolders").EnumerateArray())
            {
                Assert.False(Directory.Exists(Path.Combine(projectPath, forbiddenFolder.GetString()!)), $"{projectName} resurrected forbidden folder {forbiddenFolder}");
            }
        }
    }

    [Fact]
    public void Path_derived_namespaces_are_exact_across_all_production_projects()
    {
        foreach (var project in new[]
                 {
                     "Contracts", "Domain",
                     "Application", "Infrastructure",
                     "Endpoints",
                 })
        {
            AssertExactNamespaces(Project(project), $"Tooba.{Module}.{project}");
        }
    }

    [Fact]
    public void Endpoints_import_only_self_contracts_application_and_building_blocks()
    {
        foreach (var file in Directory.EnumerateFiles(Project("Endpoints"), "*.cs", System.IO.SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Tooba.OperatorProfile.Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.OperatorProfile.Domain", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Host.", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        }

        var csproj = File.ReadAllText(Path.Combine(
            Project("Endpoints"), "Tooba.OperatorProfile.Endpoints.csproj"));
        Assert.DoesNotContain("Tooba.OperatorProfile.Infrastructure", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.OperatorProfile.Domain", csproj, StringComparison.Ordinal);
    }

    [Fact]
    public void Solution_grouping_is_canonical_and_host_closure_preserved()
    {
        var root = RepoRoot();
        var slnx = File.ReadAllText(Path.Combine(root, "src", "backend", "Tooba.slnx"));
        Assert.Contains("<Folder Name=\"/Modules/OperatorProfile/\">", slnx, StringComparison.Ordinal);
        foreach (var project in new[]
                 {
                     "Domain", "Contracts", "Application", "Infrastructure", "Endpoints",
                 })
        {
            Assert.Contains(
                $"Modules/OperatorProfile/Tooba.OperatorProfile.{project}/Tooba.OperatorProfile.{project}.csproj",
                slnx,
                StringComparison.Ordinal);
        }

        // Host OperatorProfile business folder stays absent (HOST_ZERO preserved).
        Assert.False(Directory.Exists(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "OperatorProfile")));

        // No TypeForwardedTo / alias workarounds in any module source.
        foreach (var file in Directory.EnumerateFiles(
                     Path.Combine(root, "src", "backend", "Modules", Module),
                     "*.cs",
                     System.IO.SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            Assert.DoesNotContain("TypeForwardedTo", File.ReadAllText(file), StringComparison.Ordinal);
        }
    }

    private static void AssertExactNamespaces(string projectPath, string projectName)
    {
        var rootFull = Path.GetFullPath(projectPath);
        foreach (var file in Directory.GetFiles(projectPath, "*.cs", System.IO.SearchOption.AllDirectories))
        {
            var relative = file[rootFull.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (relative.StartsWith($"obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relative.StartsWith($"bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var dir = Path.GetDirectoryName(relative);
            var expected = string.IsNullOrEmpty(dir)
                ? projectName
                : projectName + "." + dir.Replace(Path.DirectorySeparatorChar, '.').Replace(Path.AltDirectorySeparatorChar, '.');

            // EF generated migration files keep their own locked namespace; generated snapshot files
            // under Persistence/Migrations follow the repository-wide EF exemption.
            if (relative.Contains($"Persistence{Path.DirectorySeparatorChar}Migrations", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            var match = Regex.Match(text, @"^namespace\s+([A-Za-z0-9_.]+)", RegexOptions.Multiline);
            Assert.True(match.Success, $"no namespace in {relative}");
            Assert.Equal(expected, match.Groups[1].Value);
        }
    }

    private static string Project(string projectName) => Path.Combine(
        RepoRoot(), "src", "backend", "Modules", Module, $"Tooba.{Module}.{projectName}");

    private static string RepoRoot()
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
