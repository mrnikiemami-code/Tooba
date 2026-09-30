using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-GRID-AMC-001-R5 (FINAL) — Host/Grid ABSENT (HOST_ZERO);
/// BuildingBlocks.Grid remains the only shared platform.
/// </summary>
public sealed class HostGridAmcR5GuardTests
{
    [Fact]
    public void Host_Grid_directory_is_absent()
    {
        var hostRoot = HostRoot();
        Assert.False(Directory.Exists(Path.Combine(hostRoot, "Grid")));
        foreach (var name in new[]
                 {
                     "AdminListGridPolicies.cs",
                     "AdminListGridQueryPolicy.cs",
                     "BoundedListGridQueryEngine.cs",
                     "InMemoryGridField.cs",
                     "InMemoryGridFieldKind.cs",
                     "AdminStoryGridQueryEngine.cs",
                     "AdminReviewGridQueryEngine.cs",
                     "AdminSellersGridQueryEngine.cs",
                 })
        {
            Assert.False(File.Exists(Path.Combine(hostRoot, "Grid", name)));
        }

        var program = File.ReadAllText(Path.Combine(hostRoot, "Program.cs"));
        Assert.DoesNotContain("Tooba.Host.Grid", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminStoryGridQueryEngine", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminReviewGridQueryEngine", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminSellersGridQueryEngine", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_Development_allowlist_unchanged_no_grid_sink()
    {
        var development = Path.Combine(HostRoot(), "Development");
        Assert.True(Directory.Exists(development));
        var files = Directory.GetFiles(development, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[]
            {
                "DevelopmentSchemaMigrator.cs",
                "DevelopmentTenantCommerceContext.cs",
                "MarketplaceAdminDevBootstrap.cs",
                "MarketplaceDevelopmentBootstrap.cs",
                "MarketplaceSellerDevBootstrap.cs",
            },
            files);
        Assert.DoesNotContain(files, name => name!.Contains("Grid", StringComparison.Ordinal));
    }

    [Fact]
    public void Module_owned_grid_policies_exist_without_Host_Grid()
    {
        var root = FindRepoRoot();
        Assert.True(File.Exists(Path.Combine(
            root, "src", "backend", "Modules", "Story",
            "Tooba.Story.Infrastructure", "Grid", "StoryAdminGridPolicies.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src", "backend", "Modules", "Reviews",
            "Tooba.Reviews.Infrastructure", "Grid", "ReviewsAdminGridPolicies.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src", "backend", "Modules", "Party",
            "Tooba.Party.Infrastructure", "Grid", "PartyAdminSellersGridPolicies.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src", "backend", "Modules", "Order",
            "Tooba.Order.Application", "Admin", "OrdersGrid", "AdminOrdersGridPolicy.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src", "backend", "BuildingBlocks", "Tooba.BuildingBlocks", "Grid", "GridQueryPolicyBase.cs")));
    }

    [Fact]
    public void Host_write_baseline_has_no_Grid_entries()
    {
        var baseline = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host.Tests", "Baselines", "tmar-host-write-files.json"));
        Assert.DoesNotContain("Grid/", baseline, StringComparison.Ordinal);
    }

    [Fact]
    public void SoT_and_evidence_hostGridAmc_CLOSED_HOST_ZERO()
    {
        var root = FindRepoRoot();
        var sot = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"hostGridAmc\"", sot, StringComparison.Ordinal);
        Assert.Contains("\"hostGridAmcR5\"", sot, StringComparison.Ordinal);
        Assert.Contains("CLOSED_HOST_ZERO", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_GRID_AMC_001_R5", sot, StringComparison.Ordinal);
        Assert.True(Directory.Exists(Path.Combine(root, "docs/evidence/TB-TMAR-HOST-GRID-AMC-001-R5")));
    }

    private static string HostRoot()
        => Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host");

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md"))
                || File.Exists(Path.Combine(dir.FullName, "Tooba.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
