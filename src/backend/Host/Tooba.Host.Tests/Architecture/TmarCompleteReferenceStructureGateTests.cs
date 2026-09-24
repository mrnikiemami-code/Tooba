using System.Text.Json;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-LOCK-001 — reusable TMAR COMPLETE_REFERENCE_PATTERN structure gate.
/// Module structure manifests certify root allowlists, path↔namespace alignment and capability-root constraints.
/// </summary>
public sealed class TmarCompleteReferenceStructureGateTests
{
    private const string ManifestRelativePath = "docs/architecture/tmar-module-structure-manifests.json";

    [Fact]
    public void Manifest_is_well_formed_and_only_declared_modules_are_certified()
    {
        using var doc = ReadManifest(out _);
        var root = doc.RootElement;

        Assert.Equal("ARCH-COMPLETE-002", root.GetProperty("version").GetString());
        Assert.Contains(
            "COMPLETE_REFERENCE_PATTERN_REQUIRES_ENDPOINTS_CQRS_RESULT_CONTRACTS_VALIDATION_CAPABILITY_STRUCTURE_GUARDS_SOT",
            root.GetProperty("definitionMarker").GetString(),
            StringComparison.Ordinal);

        var modules = root.GetProperty("modules").EnumerateArray().ToArray();
        Assert.Equal(
            new[] { "Cart", "Offer", "Order", "StoreContext" },
            modules.Select(m => m.GetProperty("module").GetString()!).OrderBy(x => x, StringComparer.Ordinal).ToArray());

        foreach (var module in modules)
        {
            Assert.True(module.GetProperty("structureCertified").GetBoolean());
            Assert.Equal("ARCH-COMPLETE-002", module.GetProperty("lockVersion").GetString());
        }

        foreach (var other in root.GetProperty("uncertifiedHttpOwningModules").EnumerateArray())
        {
            Assert.DoesNotContain(other.GetString(), new[] { "Order", "Cart", "StoreContext", "Offer" }, StringComparer.Ordinal);
        }
    }

    [Fact]
    public void Certified_modules_satisfy_root_allowlists_and_namespace_alignment()
    {
        var repoRoot = RepoRoot();
        using var doc = ReadManifest(out _);
        foreach (var module in doc.RootElement.GetProperty("modules").EnumerateArray())
        {
            var moduleName = module.GetProperty("module").GetString()!;

            foreach (var project in module.GetProperty("projects").EnumerateArray())
            {
                var projectName = project.GetProperty("projectName").GetString()!;
                var projectPath = Path.Combine(repoRoot, "src", "backend", "Modules", moduleName, projectName);
                Assert.True(Directory.Exists(projectPath), $"missing {projectName} for {moduleName}");

                var allowlist = project.GetProperty("rootAllowlist").EnumerateArray()
                    .Select(x => x.GetString()!)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToArray();
                var actualRoot = Directory.GetFiles(projectPath, "*.cs", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName!)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToArray();
                Assert.Equal(allowlist, actualRoot);

                AssertNamespaceAlignment(projectPath, projectName);

                foreach (var forbidden in project.GetProperty("forbiddenRootFiles").EnumerateArray())
                {
                    Assert.False(
                        File.Exists(Path.Combine(projectPath, forbidden.GetString()!)),
                        $"{projectName} still has forbidden root file {forbidden.GetString()}");
                }

                foreach (var forbiddenFolder in project.GetProperty("forbiddenTopLevelFolders").EnumerateArray())
                {
                    Assert.False(
                        Directory.Exists(Path.Combine(projectPath, forbiddenFolder.GetString()!)),
                        $"{projectName} still has forbidden top-level folder {forbiddenFolder.GetString()}");
                }
            }
        }
    }

    [Fact]
    public void Uncertified_modules_are_explicitly_not_claimed()
    {
        using var doc = ReadManifest(out _);
        var uncertified = doc.RootElement.GetProperty("uncertifiedHttpOwningModules").EnumerateArray()
            .Select(x => x.GetString()!)
            .ToArray();
        Assert.DoesNotContain("Order", uncertified, StringComparer.Ordinal);
        Assert.DoesNotContain("Cart", uncertified, StringComparer.Ordinal);
        Assert.DoesNotContain("Offer", uncertified, StringComparer.Ordinal);
        Assert.NotEmpty(uncertified);

        var statePath = Path.Combine(RepoRoot(), "docs", "architecture", "tmar-current-state.json");
        using var state = JsonDocument.Parse(File.ReadAllText(statePath));
        var certified = state.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()!).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(new[] { "Cart", "Offer", "Order", "StoreContext" }, certified);
    }

    private static void AssertNamespaceAlignment(string projectPath, string projectName)
    {
        var rootFull = Path.GetFullPath(projectPath);
        foreach (var file in Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories))
        {
            var relative = file[rootFull.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (relative.StartsWith($"obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relative.StartsWith($"bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            // Global using files are pure import aggregation and declare no namespace by design.
            if (Path.GetFileName(relative).StartsWith("GlobalUsings", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var dir = Path.GetDirectoryName(relative);
            var expected = string.IsNullOrEmpty(dir)
                ? projectName
                : projectName + "." + dir.Replace(Path.DirectorySeparatorChar, '.').Replace(Path.AltDirectorySeparatorChar, '.');
            var text = File.ReadAllText(file);
            var match = System.Text.RegularExpressions.Regex.Match(text, @"^namespace\s+([A-Za-z0-9_.]+)", System.Text.RegularExpressions.RegexOptions.Multiline);
            Assert.True(match.Success, $"no namespace in {relative}");
            Assert.Equal(expected, match.Groups[1].Value);
        }
    }

    private static JsonDocument ReadManifest(out string manifestPath)
    {
        manifestPath = Path.Combine(RepoRoot(), ManifestRelativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(manifestPath), manifestPath);
        return JsonDocument.Parse(File.ReadAllText(manifestPath));
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "PROJECT-STATE.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
