using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>Durable guards for TB-TMAR-HOST-REVIEWS-AMC-001 — Host Reviews HOST_ZERO.</summary>
public sealed class HostReviewsAmcGuardTests
{
    [Fact]
    public void Host_Reviews_folder_is_absent_and_module_owns_http()
    {
        var root = FindRepoRoot();
        Assert.False(Directory.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Reviews")));

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("MapReviewsModuleEndpoints", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapReviewEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("AddReviewsEndpointPresentation", program, StringComparison.Ordinal);
        Assert.Contains("HostReviewsSellerAuthorizer", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host.Reviews", program, StringComparison.Ordinal);
        Assert.DoesNotContain("ReviewPanelComposer", program, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Reviews/Tooba.Reviews.Endpoints/ReviewsEndpointModule.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Reviews/Tooba.Reviews.Application/Presentation/ReviewsPresentationComposer.cs")));
    }

    [Fact]
    public void Reviews_endpoints_do_not_reference_host_domain_or_foreign_application()
    {
        var root = FindRepoRoot();
        var csproj = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Reviews/Tooba.Reviews.Endpoints/Tooba.Reviews.Endpoints.csproj"));
        Assert.DoesNotContain("Tooba.Reviews.Domain", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Application", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Offer.Application", csproj, StringComparison.Ordinal);

        foreach (var file in EnumerateEndpointSources(root))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Tooba.Host.", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Reviews.Domain", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Catalog.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Offer.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ListSellerOffersQuery", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ICatalogLookupGateway", text, StringComparison.Ordinal);
            Assert.DoesNotContain("AdminPanelAccess.RequireAuthorizedAsync", text, StringComparison.Ordinal);
            Assert.DoesNotContain("CurrentAuthenticatedSession", text, StringComparison.Ordinal);
            Assert.DoesNotContain("message.Contains", text, StringComparison.Ordinal);
            Assert.DoesNotContain("قبلاً", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
        }

        var httpErrors = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Reviews/Tooba.Reviews.Endpoints/ReviewsHttpErrors.cs"));
        Assert.Contains("ApiResponseFactory", httpErrors, StringComparison.Ordinal);
        Assert.Contains("FromSemanticException", httpErrors, StringComparison.Ordinal);
    }

    [Fact]
    public void Seller_composition_uses_offer_contracts_port()
    {
        var root = FindRepoRoot();
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Offer/Tooba.Offer.Contracts/Ports/IOfferSellerProductIdLookup.cs")));
        var composer = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/Reviews/Tooba.Reviews.Application/Presentation/ReviewsPresentationComposer.cs"));
        Assert.Contains("IOfferSellerProductIdLookup", composer, StringComparison.Ordinal);
        Assert.Contains("ICatalogAdminProductTitleIdLookup", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("ListSellerOffersQuery", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogLookupGateway", composer, StringComparison.Ordinal);
    }

    [Fact]
    public void SoT_hostReviewsAmc_present()
    {
        var sot = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"hostReviewsAmc\"", sot, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-HOST-REVIEWS-AMC-001", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_REVIEWS_AMC_001_CLOSED_HOST_ZERO", sot, StringComparison.Ordinal);
    }

    private static IEnumerable<string> EnumerateEndpointSources(string root) =>
        Directory.EnumerateFiles(
                Path.Combine(root, "src/backend/Modules/Reviews/Tooba.Reviews.Endpoints"),
                "*.cs",
                SearchOption.AllDirectories)
            .Where(file =>
                !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
