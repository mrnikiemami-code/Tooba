using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-DEVELOPMENT-ENRICHER-CLOSURE-001 — the Catalog attribute-schema sellable
/// workflow is Catalog-owned and reaches Offer/Party/Pricing/Inventory/Tax only through
/// narrow module Contracts; Host keeps no orchestration file and no sink folder grows.
/// </summary>
public sealed class HostDevelopmentEnricherClosureGuardTests
{
    private static readonly string[] ForeignContracts =
    [
        "Tooba.Offer.Contracts",
        "Tooba.Party.Contracts",
        "Tooba.Pricing.Contracts",
        "Tooba.Inventory.Contracts",
        "Tooba.Tax.Contracts",
    ];

    [Fact]
    public void Host_Development_enricher_file_is_absent()
    {
        var folder = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Development");
        Assert.False(File.Exists(Path.Combine(folder, "CatalogAttributeSchemaSellableEnricher.cs")));
        Assert.True(
            File.Exists(Path.Combine(
                FindRepoRoot(),
                "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Development/CatalogAttributeSchemaSellableEnricher.cs")),
            "Catalog.Infrastructure must own the schema sellable enricher");
    }

    [Fact]
    public void Catalog_enricher_references_only_contracts_across_modules()
    {
        var enricher = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Development/CatalogAttributeSchemaSellableEnricher.cs"));

        foreach (var forbidden in new[]
                 {
                     "Tooba.Offer.Application",
                     "Tooba.Offer.Infrastructure",
                     "Tooba.Offer.Domain",
                     "Tooba.Party.Application",
                     "Tooba.Party.Infrastructure",
                     "Tooba.Party.Domain",
                     "Tooba.Pricing.Application",
                     "Tooba.Pricing.Infrastructure",
                     "Tooba.Pricing.Domain",
                     "Tooba.Inventory.Application",
                     "Tooba.Inventory.Infrastructure",
                     "Tooba.Inventory.Domain",
                     "Tooba.Tax.Application",
                     "Tooba.Tax.Infrastructure",
                     "Tooba.Tax.Domain",
                 })
        {
            Assert.DoesNotContain(forbidden, enricher, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("PartyDbContext", enricher, StringComparison.Ordinal);
        Assert.DoesNotContain("OfferDbContext", enricher, StringComparison.Ordinal);
        Assert.DoesNotContain("PricingDbContext", enricher, StringComparison.Ordinal);
        Assert.DoesNotContain("InventoryDbContext", enricher, StringComparison.Ordinal);
        Assert.DoesNotContain("TaxDbContext", enricher, StringComparison.Ordinal);
        Assert.DoesNotContain("IServiceProvider", enricher, StringComparison.Ordinal);
        Assert.DoesNotContain("MediatR", enricher, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_infrastructure_references_foreign_contracts_not_foreign_internals()
    {
        var csproj = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Tooba.Catalog.Infrastructure.csproj"));

        foreach (var contracts in ForeignContracts)
        {
            Assert.Contains(contracts, csproj, StringComparison.Ordinal);
        }

        foreach (var forbidden in new[]
                 {
                     "Offer.Application", "Offer.Infrastructure", "Offer.Domain",
                     "Party.Application", "Party.Infrastructure", "Party.Domain",
                     "Pricing.Application", "Pricing.Infrastructure", "Pricing.Domain",
                     "Inventory.Application", "Inventory.Infrastructure", "Inventory.Domain",
                     "Tax.Application", "Tax.Infrastructure", "Tax.Domain",
                 })
        {
            Assert.DoesNotContain(forbidden, csproj, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void New_development_gateways_live_in_owning_module_contracts()
    {
        var root = FindRepoRoot();
        foreach (var relative in new[]
                 {
                     "src/backend/Modules/Party/Tooba.Party.Contracts/IPartyDevelopmentSeedGateway.cs",
                     "src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Ports/IPricingDevelopmentSeedGateway.cs",
                     "src/backend/Modules/Inventory/Tooba.Inventory.Contracts/Availability/IInventoryDevelopmentSeedGateway.cs",
                     "src/backend/Modules/Tax/Tooba.Tax.Contracts/Ports/ITaxDevelopmentSeedGateway.cs",
                     "src/backend/Modules/Offer/Tooba.Offer.Contracts/Ports/IOfferDevelopmentSeedGateway.cs",
                 })
        {
            Assert.True(File.Exists(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar))), relative);
        }
    }

    [Fact]
    public void ProductWorkspace_bootstrap_remains_open_debt_and_untouched()
    {
        var folder = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Development");
        var debt = Path.Combine(folder, "ProductWorkspaceDevelopmentBootstrap.cs");
        Assert.True(File.Exists(debt));
        Assert.Contains("ProductWorkspace", File.ReadAllText(debt), StringComparison.Ordinal);
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
