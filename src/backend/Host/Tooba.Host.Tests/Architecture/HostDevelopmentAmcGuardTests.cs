using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-DEVELOPMENT-AMC-001 — Host/Development retained allowlist + Development-only composition guards.
/// Folder may retain files; new unclassified production files fail.
/// </summary>
public sealed class HostDevelopmentAmcGuardTests
{
    /// <summary>
    /// Every retained Development file with its classification. New files must be
    /// explicitly classified (TB-TMAR-HOST-DEVELOPMENT-AMC-002) rather than silently added.
    /// </summary>
    private static readonly Dictionary<string, string> ClassifiedAllowlist = new(StringComparer.Ordinal)
    {
        ["MarketplaceDevelopmentBootstrap.cs"] = "ALLOWED_DEVELOPMENT_COMPOSITION (Marketplace migrate + module seed orchestration)",
        ["MarketplaceAdminDevBootstrap.cs"] = "ALLOWED_DEVELOPMENT_RUNTIME_SEAM (authorization tuple)",
        ["MarketplaceSellerDevBootstrap.cs"] = "ALLOWED_DEVELOPMENT_RUNTIME_SEAM (tuple + seller snapshot)",
        ["DevelopmentTenantCommerceContext.cs"] = "ALLOWED_DEVELOPMENT_COMPOSITION (single tenant/commerce seam for module seeds)",
        ["ProductWorkspaceDevelopmentBootstrap.cs"] = "STRUCTURAL_DEBT_ONLY (bounded blocker: Development schema-migration list only; Wave 2 TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-MIGRATION-SEAM-001)",
    };

    [Fact]
    public void Development_folder_contains_only_classified_files()
    {
        var folder = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Development");
        Assert.True(Directory.Exists(folder));
        var files = Directory.GetFiles(folder, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ClassifiedAllowlist.Keys.OrderBy(x => x, StringComparer.Ordinal).ToArray(), files);
    }

    [Fact]
    public void Development_folder_seed_wrappers_are_evacuated()
    {
        var folder = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Development");
        foreach (var evacuated in new[]
        {
            "FashionTemplateCatalogSeedHost.cs",
            "IndustryBatchATemplateCatalogSeedHost.cs",
            "IndustryBatchBTemplateCatalogSeedHost.cs",
            "IndustryBatchCTemplateCatalogSeedHost.cs",
            "CatalogAttributeSchemaDevelopmentSeedHost.cs",
            "LandingPageDevelopmentSeedHost.cs",
            "StoreMenuDevelopmentSeedHost.cs",
        })
        {
            Assert.False(File.Exists(Path.Combine(folder, evacuated)), evacuated);
        }

        Assert.True(File.Exists(Path.Combine(folder, "DevelopmentTenantCommerceContext.cs")));
    }

    [Fact]
    public void MarketplaceDevelopmentBootstrap_is_migration_orchestration_only()
    {
        var text = Read("MarketplaceDevelopmentBootstrap.cs");
        Assert.Contains("Database.MigrateAsync", text, StringComparison.Ordinal);
        Assert.Contains("registry.Edition != ToobaEdition.Marketplace", text, StringComparison.Ordinal);
        Assert.DoesNotContain(".Add(", text, StringComparison.Ordinal); // no entity Add to DbSets
        Assert.DoesNotContain("DbSet<", text, StringComparison.Ordinal);
        Assert.DoesNotContain(".Profiles", text, StringComparison.Ordinal);
        Assert.DoesNotContain(".Addresses", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Domain.CustomerProfile.Create", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPut(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapDelete(", text, StringComparison.Ordinal);
        // Seeds invoked are module-owned ApplyAsync orchestration, not local seed data.
        Assert.Contains("ContentDevelopmentSeed.ApplyAsync", text, StringComparison.Ordinal);
        Assert.Contains("PageCompositionDevelopmentSeed.ApplyAsync", text, StringComparison.Ordinal);
        Assert.Contains("StoryDevelopmentSeed.ApplyAsync", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_and_Seller_bootstraps_have_no_foreign_persistence()
    {
        foreach (var name in new[] { "MarketplaceAdminDevBootstrap.cs", "MarketplaceSellerDevBootstrap.cs" })
        {
            var text = Read(name);
            Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DbSet<", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MigrateAsync", text, StringComparison.Ordinal);
            Assert.DoesNotContain("SaveChanges", text, StringComparison.Ordinal);
            Assert.Contains("IAuthorizationTupleWriter", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapGet(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapPost(", text, StringComparison.Ordinal);
        }

        var admin = Read("MarketplaceAdminDevBootstrap.cs");
        Assert.Contains("DefaultAdminActor", admin, StringComparison.Ordinal);
        Assert.Contains("MarketplacePlatformTenantId", admin, StringComparison.Ordinal);

        var seller = Read("MarketplaceSellerDevBootstrap.cs");
        Assert.Contains("DefaultSellerParty", seller, StringComparison.Ordinal);
        Assert.Contains("DefaultSellerActor", seller, StringComparison.Ordinal);
        Assert.Contains("SellerDevActorBootstrap.PublishSnapshot", seller, StringComparison.Ordinal);
    }

    [Fact]
    public void Program_invokes_marketplace_bootstrap_only_in_Development()
    {
        var program = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs"));
        Assert.True(
            Regex.IsMatch(
                program,
                @"if\s*\(\s*app\.Environment\.IsDevelopment\s*\(\s*\)\s*\)[\s\S]{0,800}?MarketplaceDevelopmentBootstrap\.ApplyAsync",
                RegexOptions.CultureInvariant),
            "MarketplaceDevelopmentBootstrap must remain behind IsDevelopment()");
    }

    private static string Read(string fileName) =>
        File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Development", fileName));

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
