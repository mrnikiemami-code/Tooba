using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>Durable guards for TB-TMAR-HOST-WISHLIST-AMC-001 — Host Wishlist HOST_ZERO.</summary>
public sealed class HostWishlistAmcGuardTests
{
    [Fact]
    public void Host_Wishlist_folder_is_absent_and_module_owns_http()
    {
        var root = FindRepoRoot();
        Assert.False(Directory.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Wishlist")));

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("MapWishlistModuleEndpoints", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapWishlistEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("AddWishlistEndpointPresentation", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host.Wishlist", program, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Wishlist/Tooba.Wishlist.Endpoints/WishlistEndpointModule.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Wishlist/Tooba.Wishlist.Endpoints/Customer/WishlistCustomerEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Wishlist/Tooba.Wishlist.Infrastructure/Development/WishlistDevelopmentSeed.cs")));
    }

    [Fact]
    public void Wishlist_directory_and_composer_are_contracts_only_toward_catalog()
    {
        var root = FindRepoRoot();
        var directory = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Wishlist/Tooba.Wishlist.Infrastructure/Directories/WishlistDirectory.cs"));
        Assert.Contains("ICatalogReviewProductLookup", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogLookupGateway", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogPublicationStatus", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Application", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Domain", directory, StringComparison.Ordinal);

        var composer = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Wishlist/Tooba.Wishlist.Application/Composition/WishlistPresentationComposer.cs"));
        Assert.Contains("ICatalogStorefrontProductCardLookup", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("IStorefrontComposer", composer, StringComparison.Ordinal);

        var seed = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Wishlist/Tooba.Wishlist.Infrastructure/Development/WishlistDevelopmentSeed.cs"));
        Assert.Contains("ICatalogDevelopmentPublishedProductSampler", seed, StringComparison.Ordinal);
        Assert.Contains("StorefrontGuestActor", seed, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", seed, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Domain", seed, StringComparison.Ordinal);

        var infraCsproj = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Wishlist/Tooba.Wishlist.Infrastructure/Tooba.Wishlist.Infrastructure.csproj"));
        Assert.Contains("Tooba.Catalog.Contracts", infraCsproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Application", infraCsproj, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_contracts_product_card_and_sampler_adapters_exist()
    {
        var root = FindRepoRoot();
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Ports/CatalogStorefrontProductCardContracts.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Ports/CatalogDevelopmentPublishedProductSamplerContracts.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Adapters/CatalogStorefrontProductCardLookup.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Adapters/CatalogDevelopmentPublishedProductSampler.cs")));
    }

    [Fact]
    public void Development_schema_migrator_calls_module_owned_wishlist_seed()
    {
        var migrator = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src/backend/Host/Tooba.Host/Development/DevelopmentSchemaMigrator.cs"));
        Assert.Contains("Tooba.Wishlist.Infrastructure.Development", migrator, StringComparison.Ordinal);
        Assert.Contains("WishlistDevelopmentSeed.ApplyAsync", migrator, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host.Wishlist", migrator, StringComparison.Ordinal);
    }

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
