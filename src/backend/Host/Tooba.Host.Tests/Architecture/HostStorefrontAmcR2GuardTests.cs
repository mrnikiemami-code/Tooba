using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-STOREFRONT-AMC-001-R2 — checkout-identity policy + appearance → Catalog;
/// media → Media.Endpoints; Host security adapters relocated under Host/Security.
/// </summary>
public sealed class HostStorefrontAmcR2GuardTests
{
    [Fact]
    public void Host_storefront_no_longer_owns_settings_or_media_routes()
    {
        Assert.False(Directory.Exists(Path.Combine(HostRoot(), "Storefront")));

        var program = File.ReadAllText(Path.Combine(HostRoot(), "Program.cs"));
        Assert.DoesNotContain("MapStorefrontEndpoints()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("using Tooba.Host.Storefront;", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_owns_storefront_settings_routes_and_media_owns_storefront_media()
    {
        var root = FindRepoRoot();
        var catalogModule = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/CatalogEndpointModule.cs"));
        Assert.Contains("MapCatalogStorefrontSettingsEndpoints()", catalogModule, StringComparison.Ordinal);

        var settings = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Storefront/Settings/CatalogStorefrontSettingsEndpoints.cs"));
        Assert.Contains("MapGet(\"/checkout-identity-policy\"", settings, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/appearance\"", settings, StringComparison.Ordinal);
        Assert.Contains("GetStorefrontCheckoutIdentityPolicyQuery", settings, StringComparison.Ordinal);
        Assert.Contains("GetStorefrontAppearanceQuery", settings, StringComparison.Ordinal);

        var mediaModule = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Media/Tooba.Media.Endpoints/MediaEndpointModule.cs"));
        Assert.Contains("MapMediaStorefrontEndpoints()", mediaModule, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Media/Tooba.Media.Endpoints/Storefront/MediaStorefrontEndpoints.cs")));
    }

    [Fact]
    public void Host_security_adapters_relocated_and_gate_uses_catalog_contracts()
    {
        var hostRoot = HostRoot();
        Assert.True(File.Exists(Path.Combine(hostRoot, "Security", "Checkout", "CheckoutIdentityGate.cs")));
        Assert.True(File.Exists(Path.Combine(hostRoot, "Security", "Checkout", "HostCheckoutActorPolicyAdapter.cs")));
        Assert.True(File.Exists(Path.Combine(hostRoot, "Security", "Payment", "HostPaymentStorefrontAuthorizer.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Storefront", "CheckoutIdentityGate.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Storefront", "HostCheckoutActorPolicyAdapter.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Storefront", "HostPaymentStorefrontAuthorizer.cs")));

        var gate = File.ReadAllText(Path.Combine(hostRoot, "Security", "Checkout", "CheckoutIdentityGate.cs"));
        Assert.Contains("ICatalogCheckoutIdentityPolicyLookup", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", gate, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(hostRoot, "Program.cs"));
        Assert.Contains("Tooba.Host.Security.Checkout.CheckoutIdentityGate", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.Host.Security.Checkout.HostCheckoutActorPolicyAdapter", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.Host.Security.Payment.HostPaymentStorefrontAuthorizer", program, StringComparison.Ordinal);
        Assert.Contains("ICatalogCheckoutIdentityPolicyLookup", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_contracts_checkout_identity_lookup_registered()
    {
        var root = FindRepoRoot();
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Checkout/CheckoutIdentityPolicyContracts.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Checkout/CatalogCheckoutIdentityPolicyLookup.cs")));
        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/CatalogModule.cs"));
        Assert.Contains("ICatalogCheckoutIdentityPolicyLookup, Checkout.CatalogCheckoutIdentityPolicyLookup", module, StringComparison.Ordinal);
    }

    [Fact]
    public void SoT_and_evidence_hostStorefrontAmcR2_present()
    {
        var root = FindRepoRoot();
        var sot = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"hostStorefrontAmcR2\"", sot, StringComparison.Ordinal);
        Assert.True(Directory.Exists(Path.Combine(
            root, "docs/evidence/TB-TMAR-HOST-STOREFRONT-AMC-001-R2")));
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
