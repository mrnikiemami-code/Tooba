using Tooba.Promotion.Application.Ports;
using Tooba.Promotion.Infrastructure.Queries;
using Tooba.Promotion.Infrastructure.Messaging;
using Tooba.Promotion.Infrastructure.Adapters;
using Tooba.Promotion.Infrastructure.Directories;
using Tooba.Inventory.Infrastructure.Messaging;
using Tooba.Inventory.Infrastructure.Adapters;
using Tooba.Inventory.Infrastructure.Directories;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Pricing.Contracts;
using Tooba.Pricing.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>Characterization for Promotion→Offer.Contracts and Cart→Pricing.Contracts.</summary>
public sealed class ContractsW6CharacterizationTests
{
    [Fact]
    public void Promotion_application_references_offer_contracts_not_offer_application()
    {
        var root = FindRepoRoot();
        var csproj = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Promotion", "Tooba.Promotion.Application", "Tooba.Promotion.Application.csproj"));
        Assert.Contains("Tooba.Offer.Contracts", csproj);
        Assert.DoesNotContain("Tooba.Offer.Application", csproj);
    }

    [Fact]
    public void Cart_application_references_pricing_contracts_not_pricing_application()
    {
        var root = FindRepoRoot();
        var csproj = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Cart", "Tooba.Cart.Application", "Tooba.Cart.Application.csproj"));
        Assert.Contains("Tooba.Pricing.Contracts", csproj);
        Assert.DoesNotContain("Tooba.Pricing.Application", csproj);
    }

    [Fact]
    public void Campaign_cart_price_authority_and_currency_are_pricing_contracts_owned()
    {
        Assert.Equal("Tooba.Pricing.Contracts", typeof(ICampaignCartPriceAuthority).Namespace);
        Assert.Equal("Tooba.Pricing.Contracts", typeof(CurrencyCode).Assembly.GetName().Name);
        Assert.Equal("IRR", CurrencyCode.Parse("irr").Value);
    }

    [Fact]
    public void Return_policy_resolver_is_offer_contracts_owned()
    {
        Assert.Equal("Tooba.Offer.Contracts.Ports", typeof(IReturnPolicyResolver).Namespace);
        var resolved = new ReturnPolicyResolver(new ReturnPolicyOptions()).ResolveForCheckout(OfferReturnPolicyChoices.Default, null);
        Assert.True(resolved.IsReturnable);
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
