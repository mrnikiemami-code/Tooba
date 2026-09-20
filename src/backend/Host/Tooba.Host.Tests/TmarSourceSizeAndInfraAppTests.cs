using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests;

public sealed class TmarSourceSizeAndInfraAppTests
{
    [Fact]
    public void Hand_written_source_size_does_not_expand_beyond_baseline()
    {
        var root = FindRepoRoot();
        var baseline = LoadJsonBaseline("tmar-source-size-baseline.json");
        var actual = TmarSourceSizeGuard.ScanHandWrittenSources(root);
        var violations = TmarSourceSizeGuard.Evaluate(actual, baseline);
        Assert.True(
            violations.Count == 0,
            FormatViolations(violations));
    }

    [Fact]
    public void Source_size_inventory_evidence_exists_and_matches_scan_count()
    {
        var root = FindRepoRoot();
        var inventoryPath = Path.Combine(root, "docs", "evidence", "TB-TMAR-BOUNDARY-V1-R1", "source-size-inventory.json");
        Assert.True(File.Exists(inventoryPath), inventoryPath);
        using var doc = JsonDocument.Parse(File.ReadAllText(inventoryPath));
        var fileCount = doc.RootElement.GetProperty("fileCount").GetInt32();
        var scanned = TmarSourceSizeGuard.ScanHandWrittenSources(root);
        Assert.True(fileCount > 1000, $"expected repository-wide inventory, got {fileCount}");
        Assert.Equal(scanned.Count, fileCount);

        var baseline = LoadJsonBaseline("tmar-source-size-baseline.json");
        var baselinePaths = baseline.GetProperty("files").EnumerateArray()
            .Select(e => e.GetProperty("path").GetString()!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var inventoryPaths = doc.RootElement.GetProperty("files").EnumerateArray()
            .Select(e => e.GetProperty("path").GetString()!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.True(baselinePaths.IsSubsetOf(inventoryPaths), "baseline paths missing from inventory");
        Assert.Equal(
            baselinePaths.Count,
            scanned.Count(f => f.Classification is "OVERSIZED_LEGACY" or "CRITICAL_GOD_FILE"));
    }

    [Fact]
    public void Synthetic_new_hand_written_file_over_800_loc_is_rejected()
    {
        var threshold = TmarSourceSizeGuard.NewFileLocThreshold;
        var baseline = JsonDocument.Parse("""{"rule":"SourceSize","thresholdNewFileLoc":800,"files":[]}""").RootElement;
        var synthetic = new[]
        {
            new TmarSourceSizeGuard.SourceFileInfo("src/backend/Host/Tooba.Host/SyntheticGiant.cs", "cs", threshold + 1, "OVERSIZED_LEGACY"),
        };
        var violations = TmarSourceSizeGuard.Evaluate(synthetic, baseline);
        Assert.Contains(violations, v => v.ViolationType == "NEW_OVERSIZED_FILE" && v.Path.EndsWith("SyntheticGiant.cs", StringComparison.Ordinal));
        Assert.Equal(threshold + 1, violations[0].CurrentLoc);
        Assert.Null(violations[0].BaselineLoc);
        Assert.Equal(threshold, violations[0].Threshold);
    }

    [Fact]
    public void Synthetic_oversized_legacy_growth_is_rejected()
    {
        var baseline = JsonDocument.Parse(
            """{"rule":"SourceSize","thresholdNewFileLoc":800,"files":[{"path":"legacy/God.cs","language":"cs","loc":900,"classification":"OVERSIZED_LEGACY"}]}""")
            .RootElement;
        var synthetic = new[]
        {
            new TmarSourceSizeGuard.SourceFileInfo("legacy/God.cs", "cs", 950, "OVERSIZED_LEGACY"),
        };
        var violations = TmarSourceSizeGuard.Evaluate(synthetic, baseline);
        var v = Assert.Single(violations);
        Assert.Equal("OVERSIZED_GROWTH", v.ViolationType);
        Assert.Equal(950, v.CurrentLoc);
        Assert.Equal(900, v.BaselineLoc);
        Assert.Equal(800, v.Threshold);
    }

    [Fact]
    public void Synthetic_oversized_legacy_shrink_is_allowed()
    {
        var baseline = JsonDocument.Parse(
            """{"rule":"SourceSize","thresholdNewFileLoc":800,"files":[{"path":"legacy/God.cs","language":"cs","loc":900,"classification":"OVERSIZED_LEGACY"}]}""")
            .RootElement;
        var synthetic = new[]
        {
            new TmarSourceSizeGuard.SourceFileInfo("legacy/God.cs", "cs", 850, "OVERSIZED_LEGACY"),
        };
        var violations = TmarSourceSizeGuard.Evaluate(synthetic, baseline);
        Assert.Empty(violations);
    }

    [Fact]
    public void Infrastructure_to_foreign_Application_edges_do_not_expand_beyond_baseline()
    {
        var baseline = LoadJsonBaseline("tmar-infra-to-foreign-application.json");
        var allowed = baseline.GetProperty("edges").EnumerateArray().Select(e => e.GetString()!).ToHashSet(StringComparer.Ordinal);
        var actual = ScanProjectEdges(".Infrastructure", ".Application", foreignModulesOnly: true).ToHashSet(StringComparer.Ordinal);
        var extra = actual.Except(allowed).OrderBy(x => x).ToArray();
        Assert.True(extra.Length == 0, "NEW Infrastructure→foreign Application edges: " + string.Join("; ", extra));
        var missing = allowed.Except(actual).OrderBy(x => x).ToArray();
        Assert.True(missing.Length == 0, "Baseline Infrastructure→foreign Application edges missing (shrink via explicit baseline edit): " + string.Join("; ", missing));
    }

    private static string FormatViolations(IReadOnlyList<TmarSourceSizeGuard.Violation> violations)
    {
        var sb = new StringBuilder();
        sb.Append("Source-size violations:");
        foreach (var v in violations.OrderBy(x => x.Path, StringComparer.OrdinalIgnoreCase))
        {
            sb.Append(" [")
                .Append(v.ViolationType)
                .Append("] file=")
                .Append(v.Path)
                .Append(" currentLoc=")
                .Append(v.CurrentLoc)
                .Append(" baselineLoc=")
                .Append(v.BaselineLoc?.ToString() ?? "n/a")
                .Append(" threshold=")
                .Append(v.Threshold)
                .Append(';');
        }

        return sb.ToString();
    }

    private static IEnumerable<string> ScanProjectEdges(string fromSuffix, string toSuffix, bool foreignModulesOnly)
    {
        var backend = Path.Combine(FindRepoRoot(), "src", "backend");
        foreach (var path in Directory.GetFiles(backend, "*.csproj", SearchOption.AllDirectories))
        {
            if (path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var fromName = Path.GetFileNameWithoutExtension(path);
            if (!fromName.EndsWith(fromSuffix, StringComparison.Ordinal))
            {
                continue;
            }

            var fromModule = ModuleNameOf(fromName);
            var xml = XDocument.Load(path);
            foreach (var include in xml.Descendants().Where(e => e.Name.LocalName == "ProjectReference")
                         .Select(e => e.Attribute("Include")?.Value)
                         .Where(v => !string.IsNullOrWhiteSpace(v)))
            {
                var toName = Path.GetFileNameWithoutExtension(include!);
                if (!toName.EndsWith(toSuffix, StringComparison.Ordinal))
                {
                    continue;
                }

                if (foreignModulesOnly && ModuleNameOf(fromName) == ModuleNameOf(toName))
                {
                    continue;
                }

                if (foreignModulesOnly && fromModule is null)
                {
                    continue;
                }

                yield return $"{fromName} -> {toName}";
            }
        }
    }

    private static string? ModuleNameOf(string projectName)
    {
        var parts = projectName.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 && parts[0] == "Tooba" ? parts[1] : null;
    }

    private static JsonElement LoadJsonBaseline(string fileName)
    {
        var path = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host.Tests", "Baselines", fileName);
        return JsonDocument.Parse(File.ReadAllText(path)).RootElement;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "src", "backend")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo_root_not_found");
    }
}
