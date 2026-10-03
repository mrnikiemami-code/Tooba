using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Order.Tests.Architecture;

/// <summary>
/// TB-TMAR-ORDER-AMC-001-W5-R1 — durable manifest↔disk reconciliation guard.
///
/// The Order entry in <c>tmar-module-structure-manifests.json</c> must describe the real certified
/// production surface: exactly the five Order production projects, each with a <c>rootAllowlist</c>
/// equal to the actual top-level production <c>.cs</c> files on disk.
///
/// This guard reads the real manifest and the real project directories. It never hard-codes a PASS
/// that is independent of disk state, so a manifest that silently drops a project, invents a project,
/// or drifts from the on-disk root allowlist fails here.
/// </summary>
public sealed class OrderManifestDiskReconciliationGuardTests
{
    private const string ManifestRelativePath = "docs/architecture/tmar-module-structure-manifests.json";
    private const string ModuleName = "Order";

    /// <summary>The certified Order production surface (tests project excluded by design).</summary>
    private static readonly string[] ExpectedProductionProjects =
    [
        "Tooba.Order.Contracts",
        "Tooba.Order.Domain",
        "Tooba.Order.Application",
        "Tooba.Order.Endpoints",
        "Tooba.Order.Infrastructure",
    ];

    [Fact]
    public void Order_manifest_has_exactly_one_entry_certified_under_arch_complete_002()
    {
        using var doc = ReadManifest();
        var entries = doc.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => string.Equals(m.GetProperty("module").GetString(), ModuleName, StringComparison.Ordinal))
            .ToArray();

        Assert.Single(entries);
        Assert.True(entries[0].GetProperty("structureCertified").GetBoolean(), "Order structureCertified must be true");
        Assert.Equal("ARCH-COMPLETE-002", entries[0].GetProperty("lockVersion").GetString());
    }

    [Fact]
    public void Order_manifest_represents_exactly_the_five_production_projects()
    {
        var declared = ReadOrderProjectNames();
        var onDisk = ProductionProjectDirectories()
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        // Missing project in the manifest is a defect; an extra project directory is a defect too.
        Assert.Equal(onDisk, declared);
        Assert.Equal(
            ExpectedProductionProjects.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            declared);
    }

    [Fact]
    public void Order_manifest_root_allowlists_equal_real_disk_root_cs_files()
    {
        using var doc = ReadManifest();
        var order = SingleOrderEntry(doc);

        foreach (var project in order.GetProperty("projects").EnumerateArray())
        {
            var projectName = project.GetProperty("projectName").GetString()!;
            var projectDir = Path.Combine(ModulesOrderRoot(), projectName);
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
    public void Order_production_surface_has_no_message_text_failure_classification()
    {
        var violations = new List<string>();
        foreach (var file in Directory.GetFiles(ModulesOrderRoot(), "*.cs", SearchOption.AllDirectories))
        {
            var relative = file[ModulesOrderRoot().Length..];
            if (relative.StartsWith($"{Path.DirectorySeparatorChar}Tooba.Order.Tests", StringComparison.Ordinal)
                || relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relative.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            if (Regex.IsMatch(text, @"catch\s*\([^)]*\)\s*when\s*\([^)]*\.Message")
                || text.Contains(".Message.StartsWith", StringComparison.Ordinal)
                || text.Contains(".Message.Contains", StringComparison.Ordinal))
            {
                violations.Add(relative);
            }
        }

        Assert.Empty(violations);
    }

    private static string[] ReadOrderProjectNames()
    {
        using var doc = ReadManifest();
        return SingleOrderEntry(doc).GetProperty("projects").EnumerateArray()
            .Select(p => p.GetProperty("projectName").GetString()!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
    }

    private static JsonElement SingleOrderEntry(JsonDocument doc)
    {
        var entries = doc.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => string.Equals(m.GetProperty("module").GetString(), ModuleName, StringComparison.Ordinal))
            .ToArray();
        Assert.Single(entries);
        return entries[0];
    }

    private static IEnumerable<string> ProductionProjectDirectories() =>
        Directory.GetDirectories(ModulesOrderRoot())
            .Where(dir =>
            {
                var name = Path.GetFileName(dir);
                return name.StartsWith("Tooba.Order.", StringComparison.Ordinal)
                    && !name.EndsWith(".Tests", StringComparison.Ordinal);
            });

    private static string ModulesOrderRoot() =>
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
