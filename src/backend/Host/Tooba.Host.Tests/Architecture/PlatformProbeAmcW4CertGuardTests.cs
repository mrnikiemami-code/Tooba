using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PLATFORMPROBE-AMC-001-W4 — final production-absence certification lock.
/// </summary>
public sealed class PlatformProbeAmcW4CertGuardTests
{
    [Fact]
    public void Production_platform_probe_is_fully_absent_from_runtime_and_solution()
    {
        var root = Repo();
        Assert.False(Directory.Exists(Path.Combine(root, "src/backend/Modules/PlatformProbe")));

        var slnx = File.ReadAllText(Path.Combine(root, "src/backend/Tooba.slnx"));
        Assert.DoesNotContain("PlatformProbe", slnx, StringComparison.Ordinal);

        foreach (var relative in new[]
                 {
                     "src/backend/Host/Tooba.Host/Tooba.Host.csproj",
                     "src/backend/Host/Tooba.Host/Composition/ToobaModuleComposition.cs",
                     "src/backend/Host/Tooba.MigrationRunner/ModuleMigrationRegistry.cs",
                     "src/backend/BuildingBlocks/Tooba.Persistence/ModuleSchemaMigrator.cs",
                 })
        {
            var text = File.ReadAllText(Path.Combine(root, relative));
            Assert.DoesNotContain("PlatformProbe", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.PlatformProbe", text, StringComparison.Ordinal);
        }

        var order = File.ReadAllText(Path.Combine(
            root, "src/backend/BuildingBlocks/Tooba.Persistence/ModuleSchemaMigrator.cs"));
        Assert.False(Regex.IsMatch(order, @"=\s*13\s*;", RegexOptions.CultureInvariant));
        Assert.Contains("public const int Promotion = 12;", order, StringComparison.Ordinal);
        Assert.Contains("public const int Reviews = 14;", order, StringComparison.Ordinal);
        Assert.Contains("public const int Support = 29;", order, StringComparison.Ordinal);

        Assert.Empty(Directory.EnumerateFiles(
                Path.Combine(root, "src/backend/Modules"),
                "*PlatformProbe*.csproj",
                SearchOption.AllDirectories));
    }

    [Fact]
    public void Test_fixture_is_present_test_owned_and_not_referenced_by_production()
    {
        var root = Repo();
        var fixture = Path.Combine(root, "src/backend/Host/Tooba.Host.Tests/Fixtures/PlatformProbe");
        Assert.True(Directory.Exists(fixture));
        foreach (var file in Directory.EnumerateFiles(fixture, "*.cs"))
        {
            var text = File.ReadAllText(file);
            Assert.Contains("namespace Tooba.Host.Tests.Fixtures.PlatformProbe", text, StringComparison.Ordinal);
            Assert.DoesNotContain("namespace Tooba.PlatformProbe", text, StringComparison.Ordinal);
        }

        foreach (var csproj in Directory.EnumerateFiles(
                     Path.Combine(root, "src/backend"),
                     "*.csproj",
                     SearchOption.AllDirectories))
        {
            if (csproj.Contains($"{Path.DirectorySeparatorChar}Tooba.Host.Tests{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                continue;
            var text = File.ReadAllText(csproj);
            Assert.DoesNotContain("Fixtures\\PlatformProbe", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Fixtures/PlatformProbe", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Host.Tests.Fixtures.PlatformProbe", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Sot_and_structure_lock_mark_absence_certification_not_module_cert()
    {
        var root = Repo();
        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));
        var block = sot.RootElement.GetProperty("platformProbeAmc001");
        Assert.Equal("PRODUCTION_ABSENCE_CERTIFIED", block.GetProperty("w4State").GetString());
        Assert.Equal("PRODUCTION_ABSENCE_CERTIFIED", block.GetProperty("finalCertification").GetString());
        Assert.Equal("TEST_FIXTURE_ONLY", block.GetProperty("ownershipState").GetString());
        Assert.Equal("NONE", block.GetProperty("businessCapability").GetString());
        Assert.Equal("NOT_APPLICABLE", block.GetProperty("completeReferencePatternApplicability").GetString());
        Assert.Equal("NOT_APPLICABLE", block.GetProperty("archComplete002Applicability").GetString());
        Assert.Equal("ABSENT", block.GetProperty("productionModule").GetString());
        Assert.Equal("ABSENT", block.GetProperty("productionSourceTree").GetString());
        Assert.Equal("PRESERVED_NO_DROP", block.GetProperty("deployedSchema").GetString());
        Assert.False(block.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("USER_REVIEW_PLATFORMPROBE_AMC_001_W4", block.GetProperty("workflowStop").GetString());
        Assert.Equal("NONE", block.GetProperty("automaticNextImplementationTask").GetString());

        var certified = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.DoesNotContain("PlatformProbe", certified);

        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));
        var manifestModules = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Select(m => m.GetProperty("module").GetString())
            .ToArray();
        Assert.DoesNotContain("PlatformProbe", manifestModules);

        Assert.True(File.Exists(Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-PLATFORMPROBE-AMC-001-W4/final-certification.md")));
    }

    private static string Repo()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
