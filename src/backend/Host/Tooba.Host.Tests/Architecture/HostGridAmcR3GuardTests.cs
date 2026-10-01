using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-GRID-AMC-001-R3 — Reviews grid + Catalog Contracts product-title ID lookup;
/// Host Reviews path has no CatalogDbContext.
/// </summary>
public sealed class HostGridAmcR3GuardTests
{
    [Fact]
    public void Host_has_no_AdminReviewGridQueryEngine_or_Reviews_policy()
    {
        var hostRoot = HostRoot();
        Assert.False(Directory.Exists(Path.Combine(hostRoot, "Grid")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Grid", "AdminReviewGridQueryEngine.cs")));

        var program = File.ReadAllText(Path.Combine(hostRoot, "Program.cs"));
        Assert.DoesNotContain("AdminReviewGridQueryEngine", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_owns_product_title_id_lookup_contract()
    {
        var root = FindRepoRoot();
        Assert.True(File.Exists(Path.Combine(
            root, "src", "backend", "Modules", "Catalog",
            "Tooba.Catalog.Contracts", "CatalogAdminProductTitleIdLookupContracts.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src", "backend", "Modules", "Catalog",
            "Tooba.Catalog.Infrastructure", "Admin", "CatalogAdminProductTitleIdLookup.cs")));

        var module = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Catalog",
            "Tooba.Catalog.Infrastructure", "CatalogModule.cs"));
        Assert.Contains("ICatalogAdminProductTitleIdLookup", module, StringComparison.Ordinal);
    }

    [Fact]
    public void Reviews_owns_grid_policy_engine_port_and_row_model()
    {
        var root = FindRepoRoot();
        var reviewsInfra = Path.Combine(root, "src", "backend", "Modules", "Reviews", "Tooba.Reviews.Infrastructure");
        Assert.True(File.Exists(Path.Combine(reviewsInfra, "Grid", "ReviewsAdminGridPolicies.cs")));
        Assert.True(File.Exists(Path.Combine(reviewsInfra, "Grid", "AdminReviewGridQueryEngine.cs")));
        Assert.True(File.Exists(Path.Combine(reviewsInfra, "Adapters", "AdminReviewGridAdapter.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src", "backend", "Modules", "Reviews",
            "Tooba.Reviews.Application", "AdminReviewItem.cs")));

        var engine = File.ReadAllText(Path.Combine(reviewsInfra, "Grid", "AdminReviewGridQueryEngine.cs"));
        Assert.Contains("ICatalogAdminProductTitleIdLookup", engine, StringComparison.Ordinal);
        Assert.Contains("GetProductTitlesByIdsAsync", engine, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", engine, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogLookupGateway", engine, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Application", engine, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", engine, StringComparison.Ordinal);

        var csproj = File.ReadAllText(Path.Combine(reviewsInfra, "Tooba.Reviews.Infrastructure.csproj"));
        Assert.Contains("Tooba.Catalog.Contracts", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Application", csproj, StringComparison.Ordinal);
    }

    [Fact]
    public void ReviewPanelComposer_consumes_module_port_without_CatalogDbContext()
    {
        var composer = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src", "backend", "Modules", "Reviews",
            "Tooba.Reviews.Application", "Presentation", "ReviewsPresentationComposer.cs"));
        Assert.Contains("IAdminReviewGridPort", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host.Grid", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminListGridPolicies", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("ReviewsDbContext", composer, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_AdminReviewItem_definition_removed()
    {
        Assert.False(Directory.Exists(Path.Combine(HostRoot(), "Reviews")));
        var endpoints = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src", "backend", "Modules", "Reviews",
            "Tooba.Reviews.Endpoints", "Admin", "ReviewsAdminEndpoints.cs"));
        Assert.DoesNotContain("public sealed record AdminReviewItem(", endpoints, StringComparison.Ordinal);
        Assert.Contains("using Tooba.Reviews.Application", endpoints, StringComparison.Ordinal);
    }

    [Fact]
    public void R3_evidence_present()
    {
        Assert.True(File.Exists(Path.Combine(
            FindRepoRoot(), "docs", "evidence", "TB-TMAR-HOST-GRID-AMC-001-R3", "migrate.md")));
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
