using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-STOREFRONT-AMC-001-R3 — composer + models + browse routes evacuated to Catalog;
/// geography → Order.Endpoints; Contracts-only foreign enrichment.
/// </summary>
public sealed class HostStorefrontAmcR3GuardTests
{
    [Fact]
    public void Host_storefront_no_longer_owns_composer_models_or_browse_routes()
    {
        var hostRoot = HostRoot();
        Assert.False(Directory.Exists(Path.Combine(hostRoot, "Storefront")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Storefront", "StorefrontComposer.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Storefront", "StorefrontModels.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Storefront", "StorefrontEndpoints.cs")));

        var program = File.ReadAllText(Path.Combine(hostRoot, "Program.cs"));
        Assert.DoesNotContain("Host.Storefront.StorefrontComposer", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapStorefrontEndpoints()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("using Tooba.Host.Storefront;", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_owns_browse_composer_cqrs_and_contracts_only_enrichment()
    {
        var root = FindRepoRoot();
        var catalogRoot = Path.Combine(root, "src", "backend", "Modules", "Catalog");

        Assert.True(File.Exists(Path.Combine(
            catalogRoot, "Tooba.Catalog.Application", "Storefront", "Ports", "IStorefrontComposer.cs")));
        Assert.True(File.Exists(Path.Combine(
            catalogRoot, "Tooba.Catalog.Application", "Storefront", "Models", "StorefrontModels.cs")));
        Assert.True(File.Exists(Path.Combine(
            catalogRoot, "Tooba.Catalog.Application", "Storefront", "Queries", "StorefrontBrowseQueries.cs")));
        Assert.True(File.Exists(Path.Combine(
            catalogRoot, "Tooba.Catalog.Application", "Storefront", "Queries", "StorefrontBrowseHandlers.cs")));
        Assert.True(File.Exists(Path.Combine(
            catalogRoot, "Tooba.Catalog.Infrastructure", "Storefront", "StorefrontComposer.cs")));
        Assert.True(File.Exists(Path.Combine(
            catalogRoot, "Tooba.Catalog.Endpoints", "Storefront", "Browse",
            "CatalogStorefrontBrowseEndpoints.cs")));

        var composer = File.ReadAllText(Path.Combine(
            catalogRoot, "Tooba.Catalog.Infrastructure", "Storefront", "StorefrontComposer.cs"));
        Assert.DoesNotContain("Tooba.Party.Application", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Reviews.Application", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Content.Application", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Promotion.Application", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Content.Domain", composer, StringComparison.Ordinal);
        Assert.Contains("IPartyLookup", composer, StringComparison.Ordinal);
        Assert.Contains("ICheckoutPromotionPort", composer, StringComparison.Ordinal);
        Assert.Contains("IReviewsStorefrontLookup", composer, StringComparison.Ordinal);
        Assert.Contains("IContentStorefrontArticlesPort", composer, StringComparison.Ordinal);

        var endpoints = File.ReadAllText(Path.Combine(
            catalogRoot, "Tooba.Catalog.Endpoints", "Storefront", "Browse",
            "CatalogStorefrontBrowseEndpoints.cs"));
        Assert.Contains("MapGet(\"/home\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/category-plp/{slug}\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("ISender", endpoints, StringComparison.Ordinal);
        Assert.Contains("GetStorefrontHomeQuery", endpoints, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", endpoints, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            catalogRoot, "Tooba.Catalog.Endpoints", "CatalogEndpointModule.cs"));
        Assert.Contains("MapCatalogStorefrontBrowseEndpoints()", module, StringComparison.Ordinal);

        var catalogModule = File.ReadAllText(Path.Combine(
            catalogRoot, "Tooba.Catalog.Infrastructure", "CatalogModule.cs"));
        Assert.Contains("IStorefrontComposer, StorefrontComposer", catalogModule, StringComparison.Ordinal);
    }

    [Fact]
    public void Order_owns_geography_provinces_route()
    {
        var root = FindRepoRoot();
        var orderEndpoints = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Order/Tooba.Order.Endpoints/Storefront/StorefrontOrderEndpoints.cs"));
        Assert.Contains("MapGet(\"/geography/provinces\"", orderEndpoints, StringComparison.Ordinal);
        Assert.Contains("StorefrontIranGeography.Provinces", orderEndpoints, StringComparison.Ordinal);
    }

    [Fact]
    public void Cross_module_storefront_contracts_ports_exist()
    {
        var root = FindRepoRoot();
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Reviews/Tooba.Reviews.Contracts/Storefront/ReviewsStorefrontContracts.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Content/Tooba.Content.Contracts/Storefront/ContentStorefrontArticleContracts.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Reviews/Tooba.Reviews.Infrastructure/Adapters/ReviewsStorefrontLookupAdapter.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Content/Tooba.Content.Infrastructure/Adapters/ContentStorefrontArticlesAdapter.cs")));
    }

    [Fact]
    public void Downstream_host_adapters_use_catalog_composer_port()
    {
        var root = FindRepoRoot();
        var wishlistComposer = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/Wishlist/Tooba.Wishlist.Application/Presentation/WishlistPresentationComposer.cs"));
        Assert.Contains("ICatalogStorefrontProductCardLookup", wishlistComposer, StringComparison.Ordinal);
        Assert.DoesNotContain("IStorefrontComposer", wishlistComposer, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Application", wishlistComposer, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(HostRoot(), "Wishlist")));

        var landing = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/StoreLanding/StoreLandingShellAdapter.cs"));
        Assert.Contains("IStorefrontComposer", landing, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host.Storefront", landing, StringComparison.Ordinal);
        Assert.DoesNotContain("Content.Domain", landing, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(HostRoot(), "CatalogAdapters")));
    }

    [Fact]
    public void Host_storefront_residual_after_r3_was_demo_and_account_identity_only_then_r4_cleared()
    {
        Assert.False(Directory.Exists(Path.Combine(HostRoot(), "Storefront")));
        Assert.False(File.Exists(Path.Combine(HostRoot(), "Storefront", "StorefrontDemoCatalogBootstrap.cs")));
        Assert.False(File.Exists(Path.Combine(HostRoot(), "Storefront", "StorefrontDemoCatalogMatrix.cs")));
        Assert.False(File.Exists(Path.Combine(HostRoot(), "Storefront", "StorefrontAccountIdentity.cs")));
        Assert.False(File.Exists(Path.Combine(HostRoot(), "Storefront", "StorefrontEndpoints.cs")));
    }

    [Fact]
    public void SoT_and_evidence_hostStorefrontAmcR3_present()
    {
        var root = FindRepoRoot();
        var sot = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"hostStorefrontAmcR3\"", sot, StringComparison.Ordinal);
        Assert.True(Directory.Exists(Path.Combine(
            root, "docs/evidence/TB-TMAR-HOST-STOREFRONT-AMC-001-R3")));
    }

    private static string HostRoot()
        => Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host");

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Tooba.sln"))
                || File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
