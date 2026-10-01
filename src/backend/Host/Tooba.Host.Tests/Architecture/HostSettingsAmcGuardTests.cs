using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-SETTINGS-AMC-001 — CLOSED_HOST_ZERO for Host/Settings.
/// </summary>
public sealed class HostSettingsAmcGuardTests
{
    [Fact]
    public void Host_settings_folder_is_absent()
    {
        var folder = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Settings");
        Assert.False(Directory.Exists(folder), "Host/Settings must be HOST_ZERO / ABSENT");
    }

    [Fact]
    public void Host_has_no_settings_namespace_or_settings_foundation_seed_type()
    {
        var hostRoot = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host");
        foreach (var path in Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("namespace Tooba.Host.Settings", text, StringComparison.Ordinal);
            Assert.False(
                Regex.IsMatch(text, @"\bclass\s+SettingsFoundationDevelopmentSeed\b(?!Host)"),
                $"Host must not retain SettingsFoundationDevelopmentSeed type: {path}");
        }
    }

    [Fact]
    public void Composition_binder_delegates_to_module_owned_development_seeds()
    {
        var root = FindRepoRoot();
        var composition = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host", "Composition", "SettingsFoundationDevelopmentSeedHost.cs"));

        Assert.Contains("namespace Tooba.Host.Composition", composition, StringComparison.Ordinal);
        Assert.Contains("PartyOrganizationProfileDevelopmentSeed", composition, StringComparison.Ordinal);
        Assert.Contains("UserPreferenceDevelopmentSeed", composition, StringComparison.Ordinal);
        Assert.Contains("OperatorProfileDevelopmentSeed", composition, StringComparison.Ordinal);
        Assert.Contains("StorefrontGuestActor.ActorId", composition, StringComparison.Ordinal);
        Assert.Contains("WorkspaceDemoMarketplaceSeed.SellerADisplayName", composition, StringComparison.Ordinal);
        Assert.DoesNotContain("PartyDbContext", composition, StringComparison.Ordinal);
        Assert.DoesNotContain("Order.Application", composition, StringComparison.Ordinal);
        Assert.DoesNotContain("OperatorProfile.Application", composition, StringComparison.Ordinal);
        Assert.DoesNotContain("UserPreference.Application", composition, StringComparison.Ordinal);
        Assert.DoesNotContain("Party.Application", composition, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(
            root, "src", "backend", "Modules", "Party", "Tooba.Party.Infrastructure", "Development",
            "PartyOrganizationProfileDevelopmentSeed.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src", "backend", "Modules", "UserPreference", "Tooba.UserPreference.Infrastructure", "Development",
            "UserPreferenceDevelopmentSeed.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src", "backend", "Modules", "OperatorProfile", "Tooba.OperatorProfile.Infrastructure", "Development",
            "OperatorProfileDevelopmentSeed.cs")));
    }

    [Fact]
    public void Development_schema_migrator_uses_composition_binder_not_host_settings_folder()
    {
        var migrator = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Development", "DevelopmentSchemaMigrator.cs"));
        Assert.Contains("SettingsFoundationDevelopmentSeedHost", migrator, StringComparison.Ordinal);
        Assert.DoesNotContain("using Tooba.Host.Settings", migrator, StringComparison.Ordinal);
        Assert.Equal(
            2,
            Regex.Matches(migrator, @"SettingsFoundationDevelopmentSeedHost\.ApplyAsync").Count);
    }

    [Fact]
    public void Sot_records_settings_host_zero()
    {
        var sot = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs", "architecture", "tmar-current-state.json"));
        Assert.Contains("\"hostSettingsAmc\"", sot, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-HOST-SETTINGS-AMC-001", sot, StringComparison.Ordinal);
        Assert.Contains("CLOSED_HOST_ZERO", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_SETTINGS_AMC_001_CLOSED_HOST_ZERO", sot, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
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
