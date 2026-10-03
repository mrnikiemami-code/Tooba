using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PLATFORMPROBE-AMC-001-W2 — production runtime detach (source absence enforced after W3).
/// </summary>
public sealed class PlatformProbeAmcW2DetachGuardTests
{
    [Fact]
    public void Host_and_migration_runner_have_zero_platform_probe_runtime_registration()
    {
        var root = Repo();
        var hostCsproj = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Tooba.Host.csproj"));
        Assert.DoesNotContain("PlatformProbe", hostCsproj, StringComparison.Ordinal);

        var composition = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Composition/ToobaModuleComposition.cs"));
        Assert.DoesNotContain("PlatformProbeModule", composition, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.PlatformProbe", composition, StringComparison.Ordinal);

        var registry = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.MigrationRunner/ModuleMigrationRegistry.cs"));
        Assert.DoesNotContain("PlatformProbeDbContext", registry, StringComparison.Ordinal);
        Assert.DoesNotContain("\"PlatformProbe\"", registry, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.PlatformProbe", registry, StringComparison.Ordinal);

        foreach (var relative in new[]
                 {
                     "src/backend/Host/Tooba.Host/Composition/ToobaModuleComposition.cs",
                     "src/backend/Host/Tooba.MigrationRunner/ModuleMigrationRegistry.cs",
                 })
        {
            var text = File.ReadAllText(Path.Combine(root, relative));
            Assert.DoesNotContain("PlatformProbeOutboxRegistration", text, StringComparison.Ordinal);
            Assert.DoesNotContain("AddModuleSchemaMigrator<PlatformProbeDbContext>", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Fixture_preserved_and_production_source_absent_after_w3()
    {
        var root = Repo();
        Assert.True(Directory.Exists(Path.Combine(
            root, "src/backend/Host/Tooba.Host.Tests/Fixtures/PlatformProbe")));
        Assert.False(Directory.Exists(Path.Combine(root, "src/backend/Modules/PlatformProbe")));
        var order = File.ReadAllText(Path.Combine(
            root, "src/backend/BuildingBlocks/Tooba.Persistence/ModuleSchemaMigrator.cs"));
        Assert.DoesNotContain("PlatformProbe", order, StringComparison.Ordinal);
        Assert.DoesNotContain("= 13;", order, StringComparison.Ordinal);
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
