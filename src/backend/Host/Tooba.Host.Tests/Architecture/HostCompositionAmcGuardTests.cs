using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-COMPOSITION-AMC-001 / R1 — KEEP_AS_GENERIC_HOST_COMPOSITION_ROOT + exact path↔namespace.
/// </summary>
public sealed class HostCompositionAmcGuardTests
{
    private static readonly string[] Allowlist =
    [
        "ContentDevelopmentSeedHost.cs",
        "SettingsFoundationDevelopmentSeedHost.cs",
        "SupportDevelopmentSeedHost.cs",
        "ToobaModuleComposition.cs",
        "WalletDevelopmentSeedHost.cs",
    ];

    [Fact]
    public void Composition_folder_matches_exact_retained_allowlist()
    {
        var folder = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Composition");
        Assert.True(Directory.Exists(folder));

        var files = Directory.GetFiles(folder, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(Allowlist.OrderBy(x => x, StringComparer.Ordinal).ToArray(), files);
    }

    [Fact]
    public void Every_composition_file_has_exact_path_namespace()
    {
        var composition = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Composition");
        foreach (var file in Allowlist)
        {
            var text = File.ReadAllText(Path.Combine(composition, file));
            Assert.Contains("namespace Tooba.Host.Composition", text, StringComparison.Ordinal);
            Assert.DoesNotContain("namespace Tooba.Host;", text, StringComparison.Ordinal);
            Assert.DoesNotContain("namespace Tooba.Host\n", text, StringComparison.Ordinal);
            Assert.DoesNotContain("namespace Tooba.Host\r", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Seed_hosts_are_thin_and_module_bootstraps_own_migrate()
    {
        var root = FindRepoRoot();
        var composition = Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Composition");

        foreach (var file in new[]
                 {
                     "ContentDevelopmentSeedHost.cs",
                     "SupportDevelopmentSeedHost.cs",
                     "WalletDevelopmentSeedHost.cs",
                 })
        {
            var text = File.ReadAllText(Path.Combine(composition, file));
            Assert.Contains("namespace Tooba.Host.Composition", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Database.MigrateAsync", text, StringComparison.Ordinal);
        }

        var content = File.ReadAllText(Path.Combine(composition, "ContentDevelopmentSeedHost.cs"));
        Assert.Contains("ContentDevelopmentSeedBootstrap.ApplyAsync", content, StringComparison.Ordinal);

        var support = File.ReadAllText(Path.Combine(composition, "SupportDevelopmentSeedHost.cs"));
        Assert.Contains("SupportDevelopmentSeedBootstrap.ApplyAsync", support, StringComparison.Ordinal);

        var wallet = File.ReadAllText(Path.Combine(composition, "WalletDevelopmentSeedHost.cs"));
        Assert.Contains("WalletDevelopmentSeedBootstrap.ApplyAsync", wallet, StringComparison.Ordinal);

        var settings = File.ReadAllText(Path.Combine(composition, "SettingsFoundationDevelopmentSeedHost.cs"));
        Assert.Contains("namespace Tooba.Host.Composition", settings, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", settings, StringComparison.Ordinal);
        Assert.Contains("PartyOrganizationProfileDevelopmentSeed", settings, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Content/Tooba.Content.Infrastructure/Development/ContentDevelopmentSeedBootstrap.cs")));

        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Localization/Tooba.Localization.Infrastructure/LocalizationModule.cs"));
        Assert.Contains("AddModuleSchemaMigrator<LocalizationDbContext>", module, StringComparison.Ordinal);
    }

    [Fact]
    public void Module_composition_root_registers_modules_without_http_business()
    {
        var text = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Composition", "ToobaModuleComposition.cs"));
        Assert.Contains("AddToobaModules", text, StringComparison.Ordinal);
        Assert.Contains("IToobaModule", text, StringComparison.Ordinal);
        Assert.Contains("namespace Tooba.Host.Composition", text, StringComparison.Ordinal);
        Assert.DoesNotContain("namespace Tooba.Host;", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Database.MigrateAsync", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Program_wires_composition_seed_hosts()
    {
        var program = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs"));
        Assert.Contains("AddToobaModules", program, StringComparison.Ordinal);
        Assert.Contains("using Tooba.Host.Composition", program, StringComparison.Ordinal);
        Assert.Contains("ContentDevelopmentSeedHost.ApplyAsync", program, StringComparison.Ordinal);
        Assert.Contains("SupportDevelopmentSeedHost.ApplyAsync", program, StringComparison.Ordinal);
        Assert.Contains("WalletDevelopmentSeedHost.ApplyAsync", program, StringComparison.Ordinal);
    }

    [Fact]
    public void SoT_records_composition_keep_r1()
    {
        var sot = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs", "architecture", "tmar-current-state.json"));
        Assert.Contains("\"hostCompositionAmc\"", sot, StringComparison.Ordinal);
        Assert.Contains("\"hostCompositionAmcR1\"", sot, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-HOST-COMPOSITION-AMC-001-R1", sot, StringComparison.Ordinal);
        Assert.Contains("KEEP_AS_GENERIC_HOST_COMPOSITION_ROOT", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_COMPOSITION_AMC_001_R1_KEEP_GENERIC_HOST_COMPOSITION_ROOT", sot, StringComparison.Ordinal);
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
