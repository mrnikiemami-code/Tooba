using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-ACCESSCONTROL-AMSC-001-W2 (Structure) — durable manifest↔disk + structure reconciliation.
///
/// The AccessControl entry in <c>tmar-module-structure-manifests.json</c> must describe the real
/// production surface: exactly the five AccessControl production projects, each with a
/// <c>rootAllowlist</c> equal to the actual top-level production <c>.cs</c> files on disk, and the
/// canonical <c>/Modules/AccessControl/</c> solution folder must reference exactly those projects.
///
/// This guard reads the real manifest, the real project directories and the real solution file. It
/// never hard-codes a PASS that is independent of disk state, so a manifest that silently drops a
/// project, invents a project, drifts from the on-disk root allowlist, or a solution entry that
/// points at a deleted path fails here.
///
/// It also locks the W2 structure verdict: capability-first Application (no technical-axis-first
/// request root), no unjustified single-file request/use-case leaf folder, no forbidden
/// <c>Exceptions/</c> or <c>Validators/</c> resurrection, and exact path↔namespace alignment.
/// </summary>
public sealed class AccessControlManifestDiskReconciliationGuardTests
{
    private const string ManifestRelativePath = "docs/architecture/tmar-module-structure-manifests.json";
    private const string SolutionRelativePath = "src/backend/Tooba.slnx";
    private const string ModuleName = "AccessControl";

    /// <summary>The certified AccessControl production surface (no tests project exists by design).</summary>
    private static readonly string[] ExpectedProductionProjects =
    [
        "Tooba.AccessControl.Application",
        "Tooba.AccessControl.Contracts",
        "Tooba.AccessControl.Domain",
        "Tooba.AccessControl.Endpoints",
        "Tooba.AccessControl.Infrastructure",
    ];

    [Fact]
    public void AccessControl_manifest_has_exactly_one_entry_certified_under_arch_complete_002()
    {
        using var doc = ReadManifest();
        var entries = doc.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => string.Equals(m.GetProperty("module").GetString(), ModuleName, StringComparison.Ordinal))
            .ToArray();

        Assert.Single(entries);
        Assert.True(entries[0].GetProperty("structureCertified").GetBoolean(),
            "AccessControl structureCertified must be true");
        Assert.Equal("ARCH-COMPLETE-002", entries[0].GetProperty("lockVersion").GetString());
    }

    [Fact]
    public void AccessControl_manifest_represents_exactly_the_five_production_projects()
    {
        var declared = ReadDeclaredProjectNames();
        var onDisk = ProductionProjectDirectories()
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        // A project missing from the manifest is a defect; an undeclared project directory is too.
        Assert.Equal(onDisk, declared);
        Assert.Equal(ExpectedProductionProjects.OrderBy(x => x, StringComparer.Ordinal).ToArray(), declared);
    }

    [Fact]
    public void AccessControl_manifest_root_allowlists_equal_real_disk_root_cs_files()
    {
        using var doc = ReadManifest();
        var accessControl = SingleAccessControlEntry(doc);

        foreach (var project in accessControl.GetProperty("projects").EnumerateArray())
        {
            var projectName = project.GetProperty("projectName").GetString()!;
            var projectDir = Path.Combine(ModulesAccessControlRoot(), projectName);
            Assert.True(Directory.Exists(projectDir), $"missing production project directory {projectName}");

            var declared = project.GetProperty("rootAllowlist").EnumerateArray()
                .Select(x => x.GetString()!)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();
            var actual = Directory.GetFiles(projectDir, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName!)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(actual, declared);
        }
    }

    [Fact]
    public void AccessControl_solution_folder_references_exactly_the_declared_production_projects()
    {
        var slnx = File.ReadAllText(Path.Combine(RepoRoot(),
            SolutionRelativePath.Replace('/', Path.DirectorySeparatorChar)));

        // Canonical dedicated module folder; never a flat "/Modules/" catch-all.
        Assert.Contains("<Folder Name=\"/Modules/AccessControl/\">", slnx, StringComparison.Ordinal);
        Assert.DoesNotContain("<Folder Name=\"/Modules/\">", slnx, StringComparison.Ordinal);

        var entries = Regex.Matches(slnx, @"<Project Path=""Modules/AccessControl/(?<name>[^/""]+)/[^""]+\.csproj""")
            .Select(m => m.Groups["name"].Value)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ExpectedProductionProjects.OrderBy(x => x, StringComparer.Ordinal).ToArray(), entries);

        // Every declared path must resolve on disk — no stale entry pointing at a deleted project.
        foreach (Match match in Regex.Matches(slnx, @"<Project Path=""(?<path>Modules/AccessControl/[^""]+\.csproj)"""))
        {
            var projectPath = Path.Combine(RepoRoot(), "src", "backend",
                match.Groups["path"].Value.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(projectPath), $"stale solution entry {match.Groups["path"].Value}");
        }
    }

    [Fact]
    public void AccessControl_application_has_no_technical_axis_first_request_root()
    {
        var app = Path.Combine(ModulesAccessControlRoot(), "Tooba.AccessControl.Application");

        Assert.False(Directory.Exists(Path.Combine(app, "Commands")),
            "Application/Commands as a primary axis is technical-axis-first for a multi-capability module");
        Assert.False(Directory.Exists(Path.Combine(app, "Queries")),
            "Application/Queries as a primary axis is technical-axis-first for a multi-capability module");

        // The real business capabilities must stay present as the primary axis.
        foreach (var capability in new[] { "Access", "Assignments", "Bootstrap", "Ceiling", "Permissions", "Roles" })
        {
            Assert.True(Directory.Exists(Path.Combine(app, capability)), $"missing capability root {capability}");
        }

        // W1 consolidation must not be undone.
        Assert.False(Directory.Exists(Path.Combine(app, "Exceptions")));
        Assert.False(Directory.Exists(Path.Combine(app, "Validators")));
        Assert.True(Directory.Exists(Path.Combine(app, "Validation")));
    }

    [Fact]
    public void AccessControl_has_no_unjustified_single_file_request_or_use_case_leaf_folder()
    {
        var app = Path.Combine(ModulesAccessControlRoot(), "Tooba.AccessControl.Application");
        var violations = new List<string>();

        foreach (var axis in Directory.GetDirectories(app, "*", SearchOption.AllDirectories))
        {
            var axisName = Path.GetFileName(axis);
            if (!string.Equals(axisName, "Commands", StringComparison.Ordinal)
                && !string.Equals(axisName, "Queries", StringComparison.Ordinal))
            {
                continue;
            }

            // A child of a Commands/Queries folder is a per-use-case leaf; it is only acceptable when
            // it holds multiple cohesive production files with distinct responsibilities.
            foreach (var leaf in Directory.GetDirectories(axis))
            {
                var sourceCount = Directory.GetFiles(leaf, "*.cs", SearchOption.TopDirectoryOnly).Length;
                if (sourceCount <= 1)
                {
                    violations.Add($"{leaf[ModulesAccessControlRoot().Length..]} ({sourceCount} production file)");
                }
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void AccessControl_production_paths_match_namespaces_exactly()
    {
        var root = ModulesAccessControlRoot();
        var mismatches = new List<string>();

        foreach (var projectDir in ProductionProjectDirectories())
        {
            var projectName = Path.GetFileName(projectDir);
            foreach (var file in Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories))
            {
                var relative = file[projectDir.Length..].TrimStart(Path.DirectorySeparatorChar);
                if (relative.StartsWith($"obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || relative.StartsWith($"bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                var dir = Path.GetDirectoryName(relative);
                var expected = string.IsNullOrEmpty(dir)
                    ? projectName
                    : projectName + "." + dir.Replace(Path.DirectorySeparatorChar, '.');

                var match = Regex.Match(File.ReadAllText(file), @"^namespace\s+([A-Za-z0-9_.]+)", RegexOptions.Multiline);
                if (!match.Success || !string.Equals(expected, match.Groups[1].Value, StringComparison.Ordinal))
                {
                    mismatches.Add($"{file[root.Length..]} expected={expected} actual={(match.Success ? match.Groups[1].Value : "<none>")}");
                }
            }
        }

        Assert.Empty(mismatches);
    }

    private static string[] ReadDeclaredProjectNames()
    {
        using var doc = ReadManifest();
        return SingleAccessControlEntry(doc).GetProperty("projects").EnumerateArray()
            .Select(p => p.GetProperty("projectName").GetString()!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
    }

    private static JsonElement SingleAccessControlEntry(JsonDocument doc)
    {
        var entries = doc.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => string.Equals(m.GetProperty("module").GetString(), ModuleName, StringComparison.Ordinal))
            .ToArray();
        Assert.Single(entries);
        return entries[0];
    }

    private static IEnumerable<string> ProductionProjectDirectories() =>
        Directory.GetDirectories(ModulesAccessControlRoot())
            .Where(dir =>
            {
                var name = Path.GetFileName(dir);
                return name.StartsWith("Tooba.AccessControl.", StringComparison.Ordinal)
                    && !name.EndsWith(".Tests", StringComparison.Ordinal);
            });

    private static string ModulesAccessControlRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", ModuleName);

    private static JsonDocument ReadManifest() =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepoRoot(), ManifestRelativePath.Replace('/', Path.DirectorySeparatorChar))));

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "src", "backend", "Tooba.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
