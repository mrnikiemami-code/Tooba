using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-GRID-AMC-001-R2 — Story grid policy + engine owned by Story module;
/// Host Story composer consumes IAdminStoryGridPort only.
/// </summary>
public sealed class HostGridAmcR2GuardTests
{
    [Fact]
    public void Host_has_no_AdminStoryGridQueryEngine_or_Stories_policy()
    {
        var hostRoot = HostRoot();
        Assert.False(Directory.Exists(Path.Combine(hostRoot, "Grid")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Grid", "AdminStoryGridQueryEngine.cs")));

        var program = File.ReadAllText(Path.Combine(hostRoot, "Program.cs"));
        Assert.DoesNotContain("AdminStoryGridQueryEngine", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Story_owns_grid_policy_engine_and_port()
    {
        var root = FindRepoRoot();
        var storyInfra = Path.Combine(root, "src", "backend", "Modules", "Story", "Tooba.Story.Infrastructure");
        Assert.True(File.Exists(Path.Combine(storyInfra, "Grid", "StoryAdminGridPolicies.cs")));
        Assert.True(File.Exists(Path.Combine(storyInfra, "Grid", "AdminStoryGridQueryEngine.cs")));
        Assert.True(File.Exists(Path.Combine(storyInfra, "Adapters", "AdminStoryGridAdapter.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src", "backend", "Modules", "Story",
            "Tooba.Story.Application", "Ports", "IAdminStoryGridPort.cs")));

        var module = File.ReadAllText(Path.Combine(storyInfra, "StoryModule.cs"));
        Assert.Contains("IAdminStoryGridPort, AdminStoryGridAdapter", module, StringComparison.Ordinal);

        var policy = File.ReadAllText(Path.Combine(storyInfra, "Grid", "StoryAdminGridPolicies.cs"));
        Assert.Contains("GridQueryPolicyBase", policy, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", policy, StringComparison.Ordinal);
    }

    [Fact]
    public void StoryPanelComposer_consumes_module_port_only()
    {
        var composer = File.ReadAllText(Path.Combine(HostRoot(), "Story", "StoryPanelComposer.cs"));
        Assert.Contains("IAdminStoryGridPort", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host.Grid", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminListGridPolicies", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("StoryDbContext", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("new AdminStoryGridQueryEngine", composer, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_write_baseline_has_no_Story_grid_engine()
    {
        var baseline = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host.Tests", "Baselines", "tmar-host-write-files.json"));
        Assert.DoesNotContain("Grid/AdminStoryGridQueryEngine.cs", baseline, StringComparison.Ordinal);
    }

    [Fact]
    public void R2_evidence_present()
    {
        Assert.True(File.Exists(Path.Combine(
            FindRepoRoot(), "docs", "evidence", "TB-TMAR-HOST-GRID-AMC-001-R2", "migrate.md")));
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
