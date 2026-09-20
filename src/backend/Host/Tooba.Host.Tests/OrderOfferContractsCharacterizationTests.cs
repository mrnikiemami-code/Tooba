using Tooba.Offer.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>Characterization for Order.Application → Offer.Contracts SalesChannel edge.</summary>
public sealed class OrderOfferContractsCharacterizationTests
{
    [Fact]
    public void Order_application_references_offer_contracts_not_offer_application()
    {
        var root = FindRepoRoot();
        var csproj = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Order", "Tooba.Order.Application", "Tooba.Order.Application.csproj"));
        Assert.Contains("Tooba.Offer.Contracts", csproj);
        Assert.DoesNotContain("Tooba.Offer.Application", csproj);
    }

    [Fact]
    public void Sales_channel_is_offer_contracts_assembly_owned()
    {
        Assert.Equal("Tooba.Offer.Domain", typeof(SalesChannel).Namespace);
        Assert.Equal("Tooba.Offer.Contracts", typeof(SalesChannel).Assembly.GetName().Name);
        Assert.Equal(SalesChannel.Marketplace, Enum.Parse<SalesChannel>("Marketplace"));
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
