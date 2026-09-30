using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-GRID-AMC-001-R1 — Orders residue removed; Order AdminOrdersGridPolicy sole owner;
/// Host write-file baseline refreshed for remaining Grid engines only.
/// </summary>
public sealed class HostGridAmcR1GuardTests
{
    [Fact]
    public void Host_AdminListGridPolicies_Orders_residue_evacuated()
    {
        Assert.False(Directory.Exists(Path.Combine(HostRoot(), "Grid")));
        Assert.True(File.Exists(Path.Combine(
            FindRepoRoot(), "src", "backend", "Modules", "Order",
            "Tooba.Order.Application", "Admin", "OrdersGrid", "AdminOrdersGridPolicy.cs")));
    }

    [Fact]
    public void Host_write_baseline_lists_only_remaining_Grid_engines()
    {
        var baseline = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host.Tests", "Baselines", "tmar-host-write-files.json"));
        Assert.DoesNotContain("Grid/AdminContentGridQueryEngine.cs", baseline, StringComparison.Ordinal);
        Assert.DoesNotContain("Grid/AdminCustomersGridQueryEngine.cs", baseline, StringComparison.Ordinal);
        Assert.DoesNotContain("Grid/AdminFulfillmentWorkQueueQueryEngine.cs", baseline, StringComparison.Ordinal);
        Assert.DoesNotContain("Grid/AdminOrdersGridQueryEngine.cs", baseline, StringComparison.Ordinal);
        Assert.DoesNotContain("Grid/AdminPayoutGridQueryEngine.cs", baseline, StringComparison.Ordinal);
        Assert.DoesNotContain("Grid/AdminReturnGridQueryEngine.cs", baseline, StringComparison.Ordinal);
    }

    [Fact]
    public void R1_evidence_present()
    {
        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "docs", "evidence", "TB-TMAR-HOST-GRID-AMC-001-R1")));
        Assert.True(File.Exists(Path.Combine(
            FindRepoRoot(), "docs", "evidence", "TB-TMAR-HOST-GRID-AMC-001-R1", "migrate.md")));
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
