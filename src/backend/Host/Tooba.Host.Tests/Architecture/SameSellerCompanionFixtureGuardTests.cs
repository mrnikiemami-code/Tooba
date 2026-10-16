using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-SHIPPING-ONE-LEVEL-TABS-136 guard: the same-seller companion fixture is Development-only,
/// additive, and reachable ONLY from the create-path seeding (never from the refresh path used by an
/// already-seeded database). It also pins that the companion offer belongs to the SAME existing
/// seller identity and uses a distinct live product/variant/SKU with its own authoritative price.
/// </summary>
public sealed class SameSellerCompanionFixtureGuardTests
{
    private static string CatalogRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src", "backend")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "src", "backend", "Modules", "Catalog", "Tooba.Catalog.Infrastructure", "Development");
    }

    [Fact]
    public void Companion_fixture_is_additive_development_only_and_create_path_bound()
    {
        var root = CatalogRoot();
        var productSeed = File.ReadAllText(Path.Combine(root, "WorkspaceDemoProductSeed.cs"));
        var marketplaceSeed = File.ReadAllText(Path.Combine(root, "WorkspaceDemoMarketplaceSeed.cs"));
        var seed = File.ReadAllText(Path.Combine(root, "WorkspaceDemoSeed.cs"));

        // Distinct companion seams: new product slug, new variant code, new SKU — no reuse of the
        // primary demo product/variant/SKU (otherwise the cart cannot hold two distinct lines).
        Assert.Contains("public const string CompanionSlug = \"workspace-live-scarf\"", productSeed, StringComparison.Ordinal);
        Assert.Contains("public const string CompanionVariantCodeSeam = \"LIVE-SCARF-NVY\"", productSeed, StringComparison.Ordinal);
        Assert.Contains("public const string SellerACompanionSku = \"ARM-SC-01\"", marketplaceSeed, StringComparison.Ordinal);
        Assert.Contains("BrandSlugSeam", productSeed, StringComparison.Ordinal);

        // The companion offer reuses the SAME existing seller identity (Seller A), not a new party.
        var companionMethod = Regex.Match(
            marketplaceSeed,
            @"EnsureSameSellerCompanionOfferAsync.*?\n    \}",
            RegexOptions.Singleline);
        Assert.True(companionMethod.Success, "companion offer method must exist");
        Assert.Contains("SellerADisplayName", companionMethod.Value, StringComparison.Ordinal);
        Assert.Contains("SellerALegalName", companionMethod.Value, StringComparison.Ordinal);
        Assert.DoesNotContain("SellerBDisplayName", companionMethod.Value, StringComparison.Ordinal);
        // Authoritative price + real stock.
        Assert.Contains("EnsureDevelopmentBasePriceAsync", companionMethod.Value, StringComparison.Ordinal);
        Assert.Contains("IncreaseStockAsync", companionMethod.Value, StringComparison.Ordinal);

        // Reachable only from the create path and only behind the existing Development gate.
        var seedMethod = Regex.Match(
            seed,
            @"SeedNewProductAsync\(CancellationToken cancellationToken = default\).*?\n    \}",
            RegexOptions.Singleline);
        Assert.True(seedMethod.Success, "SeedNewProductAsync must exist");
        Assert.Contains("IsDevelopment()", seedMethod.Value, StringComparison.Ordinal);
        Assert.Contains("EnsureCompanionLiveProductAsync", seedMethod.Value, StringComparison.Ordinal);
        Assert.Contains("EnsureSameSellerCompanionOfferAsync", seedMethod.Value, StringComparison.Ordinal);

        var refreshMethod = Regex.Match(
            seed,
            @"RefreshExistingCopyAsync\(CancellationToken cancellationToken = default\).*?\n    \}",
            RegexOptions.Singleline);
        Assert.True(refreshMethod.Success, "RefreshExistingCopyAsync must exist");
        Assert.DoesNotContain("Companion", refreshMethod.Value, StringComparison.Ordinal);
    }
}
