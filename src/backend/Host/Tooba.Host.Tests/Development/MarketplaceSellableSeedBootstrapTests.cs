using Microsoft.Extensions.Hosting;
using Tooba.Catalog.Application.Development;
using Tooba.Host.Configuration;
using Xunit;

namespace Tooba.Host.Tests.Development;

/// <summary>
/// TB240 focused facts for the opt-in Marketplace sellable-fixture commissioning seam.
/// The live checkpoint (real organizations/offers/stock/seller access) stays the real HTTP evidence.
/// </summary>
public sealed class MarketplaceSellableSeedBootstrapTests
{
    [Fact]
    public async Task Enabled_Development_seeds_only_when_the_exact_live_fixture_is_absent()
    {
        var seed = new RecordingSeed(liveFixtureExists: false);
        await MarketplaceDevelopmentBootstrap.EnsureSellableFixturesAsync(
            new EnvironmentStub("Development"),
            new MarketplaceSellableSeedOptions { Enabled = true },
            seed,
            CancellationToken.None);

        Assert.Equal(1, seed.LiveChecks);
        Assert.Equal(1, seed.SeedCalls);
    }

    [Fact]
    public async Task Existing_live_fixture_is_never_reseeded_on_repeat_or_restart()
    {
        var seed = new RecordingSeed(liveFixtureExists: true);
        await MarketplaceDevelopmentBootstrap.EnsureSellableFixturesAsync(
            new EnvironmentStub("Development"),
            new MarketplaceSellableSeedOptions { Enabled = true },
            seed,
            CancellationToken.None);

        Assert.Equal(1, seed.LiveChecks);
        Assert.Equal(0, seed.SeedCalls);
    }

    [Fact]
    public async Task Disabled_or_non_development_configuration_seeds_nothing()
    {
        foreach (var (environmentName, enabled) in new[]
                 {
                     ("Development", false),
                     ("Production", true),
                     ("Staging", true),
                 })
        {
            var seed = new RecordingSeed(liveFixtureExists: false);
            await MarketplaceDevelopmentBootstrap.EnsureSellableFixturesAsync(
                new EnvironmentStub(environmentName),
                new MarketplaceSellableSeedOptions { Enabled = enabled },
                seed,
                CancellationToken.None);

            Assert.Equal(0, seed.LiveChecks);
            Assert.Equal(0, seed.SeedCalls);
        }
    }

    [Fact]
    public void Checked_in_configuration_never_enables_the_sellable_seed()
    {
        foreach (var file in Directory.EnumerateFiles(
                     Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host"),
                     "appsettings*.json",
                     SearchOption.AllDirectories))
        {
            Assert.DoesNotContain("MarketplaceSellableSeed", File.ReadAllText(file), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Host_bootstrap_delegates_the_seed_instead_of_owning_business_data()
    {
        var text = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Development", "MarketplaceDevelopmentBootstrap.cs"));

        Assert.Contains("IWorkspaceDemoSeed", text, StringComparison.Ordinal);
        Assert.Contains("IsLiveProductSeededAsync", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbSet<", text, StringComparison.Ordinal);
        Assert.DoesNotContain(".Add(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", text, StringComparison.Ordinal);
        Assert.DoesNotContain("EntityState", text, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private sealed class EnvironmentStub(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Tooba.Host";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private sealed class RecordingSeed(bool liveFixtureExists) : IWorkspaceDemoSeed
    {
        public int LiveChecks { get; private set; }
        public int SeedCalls { get; private set; }

        public Task<bool> IsLiveProductSeededAsync(CancellationToken cancellationToken = default)
        {
            LiveChecks++;
            return Task.FromResult(liveFixtureExists);
        }

        public Task SeedNewProductAsync(CancellationToken cancellationToken = default)
        {
            SeedCalls++;
            return Task.CompletedTask;
        }

        public Task RefreshExistingCopyAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task EnsureAdminR3PreviewAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
