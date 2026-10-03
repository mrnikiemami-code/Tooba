using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PLATFORMPROBE-AMC-001-W1 — migrated test consumers are decoupled from production PlatformProbe types.
/// </summary>
public sealed class PlatformProbeAmcW1FixtureGuardTests
{
    private static readonly string[] MigratedConsumers =
    [
        "OutboxFoundationTests.cs",
        "OutboxPostgresTests.cs",
        "OutboxTestSupport.cs",
        "MassTransitPostgresTests.cs",
        "PostgresIntegrationTests.cs",
        "PersistenceFoundationTests.cs",
    ];

    [Fact]
    public void Fixture_path_and_namespace_are_test_owned()
    {
        var root = Repo();
        var fixture = Path.Combine(root, "src/backend/Host/Tooba.Host.Tests/Fixtures/PlatformProbe");
        Assert.True(Directory.Exists(fixture));
        Assert.True(File.Exists(Path.Combine(fixture, "ProbeEvents.cs")));
        Assert.True(File.Exists(Path.Combine(fixture, "TestPlatformProbeDbContext.cs")));
        Assert.True(File.Exists(Path.Combine(fixture, "TestPlatformProbeOutboxRegistration.cs")));

        foreach (var file in Directory.EnumerateFiles(fixture, "*.cs"))
        {
            var text = File.ReadAllText(file);
            Assert.Contains("namespace Tooba.Host.Tests.Fixtures.PlatformProbe", text, StringComparison.Ordinal);
            Assert.DoesNotContain("namespace Tooba.PlatformProbe", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Tooba.PlatformProbe.Infrastructure", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Migrated_test_consumers_have_zero_production_platform_probe_references()
    {
        var tests = Path.Combine(Repo(), "src/backend/Host/Tooba.Host.Tests");
        foreach (var name in MigratedConsumers)
        {
            var path = Path.Combine(tests, name);
            Assert.True(File.Exists(path), name);
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("using Tooba.PlatformProbe.Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.PlatformProbe.Infrastructure.", text, StringComparison.Ordinal);
            // bare production type names (exclude Test* fixture prefixes via word-boundary style checks)
            Assert.DoesNotContain("new PlatformProbeOutboxRegistration", text, StringComparison.Ordinal);
            Assert.DoesNotContain("new PlatformProbeDbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("typeof(PlatformProbeDbContext)", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DbContextOptionsBuilder<PlatformProbeDbContext>", text, StringComparison.Ordinal);
            Assert.False(
                System.Text.RegularExpressions.Regex.IsMatch(
                    text,
                    @"(?<![A-Za-z])PlatformProbePersistence\.",
                    System.Text.RegularExpressions.RegexOptions.CultureInvariant),
                $"{name} still references production PlatformProbePersistence");
            Assert.False(
                System.Text.RegularExpressions.Regex.IsMatch(
                    text,
                    @"(?<![A-Za-z])PlatformProbeDbContext(?![A-Za-z])",
                    System.Text.RegularExpressions.RegexOptions.CultureInvariant),
                $"{name} still references production PlatformProbeDbContext");
            Assert.False(
                System.Text.RegularExpressions.Regex.IsMatch(
                    text,
                    @"(?<![A-Za-z])PlatformProbeOutboxRegistration(?![A-Za-z])",
                    System.Text.RegularExpressions.RegexOptions.CultureInvariant),
                $"{name} still references production PlatformProbeOutboxRegistration");
        }
    }

    [Fact]
    public void Production_platform_probe_source_tree_remains_for_deferred_w3_cleanup()
    {
        var root = Repo();
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/PlatformProbe/Tooba.PlatformProbe.Infrastructure/PlatformProbeModule.cs")));
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
