using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PLATFORMPROBE-AMC-001-W3 — production source/solution/order-constant absence.
/// </summary>
public sealed class PlatformProbeAmcW3StructureGuardTests
{
    [Fact]
    public void Production_source_solution_and_order_constant_are_absent()
    {
        var root = Repo();
        Assert.False(Directory.Exists(Path.Combine(root, "src/backend/Modules/PlatformProbe")));

        var slnx = File.ReadAllText(Path.Combine(root, "src/backend/Tooba.slnx"));
        Assert.DoesNotContain("PlatformProbe", slnx, StringComparison.Ordinal);

        var order = File.ReadAllText(Path.Combine(
            root, "src/backend/BuildingBlocks/Tooba.Persistence/ModuleSchemaMigrator.cs"));
        Assert.DoesNotContain("PlatformProbe", order, StringComparison.Ordinal);
        Assert.DoesNotContain("public const int PlatformProbe", order, StringComparison.Ordinal);
        Assert.False(Regex.IsMatch(order, @"=\s*13\s*;", RegexOptions.CultureInvariant), "order 13 reused");

        Assert.Contains("public const int Promotion = 12;", order, StringComparison.Ordinal);
        Assert.Contains("public const int Reviews = 14;", order, StringComparison.Ordinal);
        Assert.Contains("public const int ProductQnA = 15;", order, StringComparison.Ordinal);
        Assert.Contains("public const int Support = 29;", order, StringComparison.Ordinal);
    }

    [Fact]
    public void Test_fixture_remains_and_host_migration_runner_stay_clean()
    {
        var root = Repo();
        var fixture = Path.Combine(root, "src/backend/Host/Tooba.Host.Tests/Fixtures/PlatformProbe");
        Assert.True(Directory.Exists(fixture));
        foreach (var file in Directory.EnumerateFiles(fixture, "*.cs"))
        {
            var text = File.ReadAllText(file);
            Assert.Contains("namespace Tooba.Host.Tests.Fixtures.PlatformProbe", text, StringComparison.Ordinal);
        }

        var hostCsproj = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Tooba.Host.csproj"));
        Assert.DoesNotContain("PlatformProbe", hostCsproj, StringComparison.Ordinal);
        var composition = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Composition/ToobaModuleComposition.cs"));
        Assert.DoesNotContain("PlatformProbe", composition, StringComparison.Ordinal);
        var registry = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.MigrationRunner/ModuleMigrationRegistry.cs"));
        Assert.DoesNotContain("PlatformProbe", registry, StringComparison.Ordinal);
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
