using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-STOREFRONT-AMC-001-R4 (FINAL) — Host/Storefront ABSENT (HOST_ZERO);
/// demo seed in Catalog Development; AccountIdentity under Host/Authentication.
/// </summary>
public sealed class HostStorefrontAmcR4GuardTests
{
    [Fact]
    public void Host_storefront_directory_is_absent()
    {
        var hostRoot = HostRoot();
        Assert.False(Directory.Exists(Path.Combine(hostRoot, "Storefront")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Storefront", "StorefrontEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Storefront", "StorefrontDemoCatalogBootstrap.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Storefront", "StorefrontDemoCatalogMatrix.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Storefront", "StorefrontAccountIdentity.cs")));

        var program = File.ReadAllText(Path.Combine(hostRoot, "Program.cs"));
        Assert.DoesNotContain("MapStorefrontEndpoints()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("using Tooba.Host.Storefront;", program, StringComparison.Ordinal);
        Assert.DoesNotContain("StorefrontDemoCatalogBootstrap", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_owns_storefront_demo_seed_via_development_contracts_only()
    {
        var root = FindRepoRoot();
        var catalogRoot = Path.Combine(root, "src", "backend", "Modules", "Catalog");
        var bootstrap = Path.Combine(
            catalogRoot, "Tooba.Catalog.Infrastructure", "Development", "StorefrontDemo",
            "StorefrontDemoCatalogBootstrap.cs");
        var matrix = Path.Combine(
            catalogRoot, "Tooba.Catalog.Infrastructure", "Development", "StorefrontDemo",
            "StorefrontDemoCatalogMatrix.cs");
        Assert.True(File.Exists(bootstrap));
        Assert.True(File.Exists(matrix));

        var text = File.ReadAllText(bootstrap);
        Assert.Contains("namespace Tooba.Catalog.Infrastructure.Development.StorefrontDemo", text, StringComparison.Ordinal);
        Assert.Contains("SentinelProductSlug", text, StringComparison.Ordinal);
        Assert.Contains("demo-mobile-1", text, StringComparison.Ordinal);
        Assert.Contains("IPartyDevelopmentSeedGateway", text, StringComparison.Ordinal);
        Assert.Contains("IOfferDevelopmentSeedGateway", text, StringComparison.Ordinal);
        Assert.Contains("IPricingDevelopmentSeedGateway", text, StringComparison.Ordinal);
        Assert.Contains("IInventoryDevelopmentSeedGateway", text, StringComparison.Ordinal);
        Assert.Contains("ITaxDevelopmentSeedGateway", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Offer.Application", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Inventory.Application", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Party.Application", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Pricing.Application", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Tax.Application", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Tax.Domain", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Inventory.Domain", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductQnA.Infrastructure", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Content.Infrastructure", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Account_identity_lives_under_host_authentication_platform()
    {
        var hostRoot = HostRoot();
        Assert.True(File.Exists(Path.Combine(hostRoot, "Authentication", "StorefrontAccountIdentity.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Storefront", "StorefrontAccountIdentity.cs")));

        var identity = File.ReadAllText(Path.Combine(hostRoot, "Authentication", "StorefrontAccountIdentity.cs"));
        Assert.Contains("namespace Tooba.Host.Authentication", identity, StringComparison.Ordinal);

        var boundary = File.ReadAllText(Path.Combine(hostRoot, "Authentication", "AuthenticationHttpBoundary.cs"));
        Assert.Contains("using Tooba.Host.Authentication;", boundary, StringComparison.Ordinal);
        Assert.DoesNotContain("using Tooba.Host.Storefront;", boundary, StringComparison.Ordinal);
        Assert.Contains("StorefrontAccountIdentity.CanonicalName", boundary, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_development_allowlist_unchanged_no_demo_sink()
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
        Assert.DoesNotContain(files, name => name!.Contains("StorefrontDemo", StringComparison.Ordinal));
        Assert.DoesNotContain(files, name => name!.Contains("DemoCatalog", StringComparison.Ordinal));
    }

    [Fact]
    public void SoT_and_evidence_hostStorefrontAmcR4_present()
    {
        var root = FindRepoRoot();
        var sot = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"hostStorefrontAmcR4\"", sot, StringComparison.Ordinal);
        Assert.Contains("CLOSED_HOST_ZERO", sot, StringComparison.Ordinal);
        Assert.True(Directory.Exists(Path.Combine(
            root, "docs/evidence/TB-TMAR-HOST-STOREFRONT-AMC-001-R4")));
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
