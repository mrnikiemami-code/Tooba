using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-GRID-AMC-001-R5-R1 — Reviews Catalog.Application ZERO;
/// grid title enrich via Catalog.Contracts only; Host/Grid remains ABSENT.
/// </summary>
public sealed class HostGridAmcR5R1GuardTests
{
    [Fact]
    public void Reviews_infrastructure_has_zero_Catalog_Application_or_Domain()
    {
        var root = FindRepoRoot();
        var reviewsInfra = Path.Combine(root, "src", "backend", "Modules", "Reviews", "Tooba.Reviews.Infrastructure");
        var csproj = File.ReadAllText(Path.Combine(reviewsInfra, "Tooba.Reviews.Infrastructure.csproj"));
        Assert.Contains("Tooba.Catalog.Contracts", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Application", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Domain", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Infrastructure", csproj, StringComparison.Ordinal);

        foreach (var file in Directory.GetFiles(reviewsInfra, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Tooba.Catalog.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Catalog.Domain", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ICatalogLookupGateway", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Catalog_owns_review_product_and_title_batch_contracts()
    {
        var root = FindRepoRoot();
        Assert.True(File.Exists(Path.Combine(
            root, "src", "backend", "Modules", "Catalog",
            "Tooba.Catalog.Contracts", "CatalogReviewProductLookupContracts.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src", "backend", "Modules", "Catalog",
            "Tooba.Catalog.Infrastructure", "Adapters", "CatalogReviewProductLookup.cs")));

        var titleContracts = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Catalog",
            "Tooba.Catalog.Contracts", "CatalogAdminProductTitleIdLookupContracts.cs"));
        Assert.Contains("GetProductTitlesByIdsAsync", titleContracts, StringComparison.Ordinal);

        var engine = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Reviews",
            "Tooba.Reviews.Infrastructure", "Grid", "AdminReviewGridQueryEngine.cs"));
        Assert.Contains("ICatalogAdminProductTitleIdLookup", engine, StringComparison.Ordinal);
        Assert.Contains("GetProductTitlesByIdsAsync", engine, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Application", engine, StringComparison.Ordinal);

        var directory = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Reviews",
            "Tooba.Reviews.Infrastructure", "ReviewDirectory.cs"));
        Assert.Contains("ICatalogReviewProductLookup", directory, StringComparison.Ordinal);
    }

    [Fact]
    public void Story_and_Party_grid_destinations_have_no_Catalog_Application_leak()
    {
        var root = FindRepoRoot();
        var storyEngine = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Story",
            "Tooba.Story.Infrastructure", "Grid", "AdminStoryGridQueryEngine.cs"));
        Assert.DoesNotContain("Tooba.Catalog.Application", storyEngine, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Domain", storyEngine, StringComparison.Ordinal);

        var partyEngine = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Party",
            "Tooba.Party.Infrastructure", "Grid", "AdminSellersGridQueryEngine.cs"));
        Assert.DoesNotContain("Tooba.Catalog.Application", partyEngine, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Domain", partyEngine, StringComparison.Ordinal);
        Assert.Contains("Tooba.Offer.Contracts", partyEngine, StringComparison.Ordinal);
        Assert.Contains("Tooba.Order.Contracts", partyEngine, StringComparison.Ordinal);
        Assert.Contains("Tooba.Party.Contracts", partyEngine, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_Grid_remains_absent_and_R5_R1_evidence_present()
    {
        var root = FindRepoRoot();
        Assert.False(Directory.Exists(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Grid")));
        Assert.True(Directory.Exists(Path.Combine(root, "docs", "evidence", "TB-TMAR-HOST-GRID-AMC-001-R5-R1")));
        var sot = File.ReadAllText(Path.Combine(root, "docs", "architecture", "tmar-current-state.json"));
        Assert.Contains("\"hostGridAmcR5R1\"", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_GRID_AMC_001_R5_R1_CLOSED_HOST_ZERO", sot, StringComparison.Ordinal);
        Assert.Contains("\"lastAcceptedTask\": \"TB-TMAR-HOST-GRID-AMC-001-R5-R1\"", sot, StringComparison.Ordinal);
    }

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
