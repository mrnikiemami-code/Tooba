using Tooba.Offer.Domain;
using Tooba.Tax.Contracts;
using Tooba.Tax.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>Characterization for Cart→Offer.Contracts and Order→Tax.Contracts edges.</summary>
public sealed class ContractsW4CharacterizationTests
{
    [Fact]
    public void Cart_application_references_offer_contracts_not_offer_application()
    {
        var root = FindRepoRoot();
        var csproj = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Cart", "Tooba.Cart.Application", "Tooba.Cart.Application.csproj"));
        Assert.Contains("Tooba.Offer.Contracts", csproj);
        Assert.DoesNotContain("Tooba.Offer.Application", csproj);
    }

    [Fact]
    public void Order_application_references_tax_contracts_not_tax_application()
    {
        var root = FindRepoRoot();
        var csproj = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Order", "Tooba.Order.Application", "Tooba.Order.Application.csproj"));
        Assert.Contains("Tooba.Tax.Contracts", csproj);
        Assert.DoesNotContain("Tooba.Tax.Application", csproj);
    }

    [Fact]
    public void Tax_outcome_and_calculator_are_contracts_assembly_owned()
    {
        Assert.Equal("Tooba.Tax.Domain", typeof(TaxOutcome).Namespace);
        Assert.Equal("Tooba.Tax.Contracts", typeof(TaxOutcome).Assembly.GetName().Name);
        Assert.Equal("Tooba.Tax.Contracts", typeof(ITaxCalculator).Namespace);
        Assert.Equal("Tooba.Tax.Contracts", typeof(ITaxCalculator).Assembly.GetName().Name);
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
