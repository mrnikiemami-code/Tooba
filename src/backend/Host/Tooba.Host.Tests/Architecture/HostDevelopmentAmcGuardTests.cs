using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-DEVELOPMENT-AMC-001 — Host/Development retained allowlist + Development-only composition guards.
/// Folder may retain files; new unclassified production files fail.
/// </summary>
public sealed class HostDevelopmentAmcGuardTests
{
    private static readonly string[] Allowlist =
    [
        "MarketplaceDevelopmentBootstrap.cs",
        "MarketplaceAdminDevBootstrap.cs",
        "MarketplaceSellerDevBootstrap.cs",
    ];

    [Fact]
    public void Development_folder_matches_exact_retained_allowlist()
    {
        var folder = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Development");
        Assert.True(Directory.Exists(folder));
        var files = Directory.GetFiles(folder, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(Allowlist.OrderBy(x => x, StringComparer.Ordinal).ToArray(), files);
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
