using Tooba.Offer.Domain;
using Tooba.Pricing.Contracts;
using Tooba.Pricing.Contracts.Ports;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>Characterization for Inventory→Offer.Contracts and Order→Pricing.Contracts.</summary>
public sealed class ContractsW5CharacterizationTests
{
    [Fact]
    public void Inventory_application_references_offer_contracts_not_offer_application()
    {
        var root = FindRepoRoot();
        var csproj = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Inventory", "Tooba.Inventory.Application", "Tooba.Inventory.Application.csproj"));
        Assert.Contains("Tooba.Offer.Contracts", csproj);
        Assert.DoesNotContain("Tooba.Offer.Application", csproj);
    }

    [Fact]
    public void Order_application_references_pricing_contracts_not_pricing_application()
    {
        var root = FindRepoRoot();
        var csproj = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Order", "Tooba.Order.Application", "Tooba.Order.Application.csproj"));
        Assert.Contains("Tooba.Pricing.Contracts", csproj);
        Assert.DoesNotContain("Tooba.Pricing.Application", csproj);
    }

    [Fact]
    public void Price_lookup_gateway_is_pricing_contracts_owned()
    {
        // TB-TMAR-PRICING-AMSC-001-W2: the W2 structure wave made every Pricing Contracts namespace
        // path-derived, so the price-lookup boundary now declares Tooba.Pricing.Contracts.Ports while
        // still being owned by the Tooba.Pricing.Contracts assembly.
        Assert.Equal("Tooba.Pricing.Contracts.Ports", typeof(IPriceLookupGateway).Namespace);
        Assert.Equal("Tooba.Pricing.Contracts", typeof(PriceQuote).Assembly.GetName().Name);
        var quote = new PriceQuote(Guid.NewGuid(), Guid.NewGuid(), "IR", SalesChannel.Marketplace, 100m, "IRR", true, true);
        Assert.Equal(100m, quote.Amount);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "SarvNewVer.sln")) ||
                File.Exists(Path.Combine(dir.FullName, "docs", "architecture", "TOOBA-ARCHITECT-BOOTSTRAP.md")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}

