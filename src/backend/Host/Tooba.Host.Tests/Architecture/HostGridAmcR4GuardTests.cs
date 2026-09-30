using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-GRID-AMC-001-R4 — Sellers grid owned by Party; Host AdminPanelComposer uses Contracts port.
/// </summary>
public sealed class HostGridAmcR4GuardTests
{
    [Fact]
    public void Host_has_no_AdminSellersGridQueryEngine_or_Sellers_policy()
    {
        var hostRoot = HostRoot();
        Assert.False(Directory.Exists(Path.Combine(hostRoot, "Grid")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Grid", "AdminSellersGridQueryEngine.cs")));

        var program = File.ReadAllText(Path.Combine(hostRoot, "Program.cs"));
        Assert.DoesNotContain("AdminSellersGridQueryEngine", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Party_owns_sellers_grid_policy_engine_port_and_row_model()
    {
        var root = FindRepoRoot();
        var partyInfra = Path.Combine(root, "src", "backend", "Modules", "Party", "Tooba.Party.Infrastructure");
        Assert.True(File.Exists(Path.Combine(partyInfra, "Grid", "PartyAdminSellersGridPolicies.cs")));
        Assert.True(File.Exists(Path.Combine(partyInfra, "Grid", "AdminSellersGridQueryEngine.cs")));
        Assert.True(File.Exists(Path.Combine(partyInfra, "Adapters", "AdminSellersGridAdapter.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src", "backend", "Modules", "Party",
            "Tooba.Party.Contracts", "AdminSellersGridContracts.cs")));

        var engine = File.ReadAllText(Path.Combine(partyInfra, "Grid", "AdminSellersGridQueryEngine.cs"));
        Assert.Contains("IOfferQueryGateway", engine, StringComparison.Ordinal);
        Assert.Contains("IPartyAdminSellerReadGateway", engine, StringComparison.Ordinal);
        Assert.Contains("IAdminSellerOrderCountPort", engine, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", engine, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", engine, StringComparison.Ordinal);
    }

    [Fact]
    public void AdminPanelComposer_consumes_Party_contracts_port_only()
    {
        var composer = File.ReadAllText(Path.Combine(HostRoot(), "Admin", "Panel", "AdminPanelComposer.cs"));
        Assert.Contains("IAdminSellersGridPort", composer, StringComparison.Ordinal);
        Assert.Contains("using Tooba.Party.Contracts;", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host.Grid", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminListGridPolicies", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminSellersGridQueryEngine", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Party.Application", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Party.Infrastructure", composer, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_AdminSellerListItem_definition_removed()
    {
        var models = File.ReadAllText(Path.Combine(HostRoot(), "Admin", "Panel", "AdminPanelModels.cs"));
        Assert.DoesNotContain("public sealed record AdminSellerListItem(", models, StringComparison.Ordinal);
    }

    [Fact]
    public void R4_evidence_present()
    {
        Assert.True(File.Exists(Path.Combine(
            FindRepoRoot(), "docs", "evidence", "TB-TMAR-HOST-GRID-AMC-001-R4", "migrate.md")));
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
