using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-CATALOGADAPTERS-AMC-001 — CLOSED_HOST_ZERO for Host/CatalogAdapters.
/// </summary>
public sealed class HostCatalogAdaptersAmcGuardTests
{
    [Fact]
    public void Host_catalog_adapters_folder_is_absent()
    {
        var folder = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "CatalogAdapters");
        Assert.False(Directory.Exists(folder), "Host/CatalogAdapters must be HOST_ZERO / ABSENT");
    }

    [Fact]
    public void Host_has_no_catalog_adapters_namespace()
    {
        var hostRoot = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host");
        foreach (var path in Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("namespace Tooba.Host.CatalogAdapters", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Host.CatalogAdapters", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Catalog_infrastructure_owns_store_landing_adapters_with_promotion_contracts()
    {
        var root = FindRepoRoot();
        var gate = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/StoreLanding/MerchandisingStoreLandingReferenceGate.cs"));
        var merch = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/StoreLanding/StoreLandingMerchandisingAdapter.cs"));
        var shell = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/StoreLanding/StoreLandingShellAdapter.cs"));

        Assert.Contains("namespace Tooba.Catalog.Infrastructure.StoreLanding", gate, StringComparison.Ordinal);
        Assert.Contains("namespace Tooba.Catalog.Infrastructure.StoreLanding", merch, StringComparison.Ordinal);
        Assert.Contains("namespace Tooba.Catalog.Infrastructure.StoreLanding", shell, StringComparison.Ordinal);
        Assert.Contains("Tooba.Promotion.Contracts.Merchandising", gate, StringComparison.Ordinal);
        Assert.Contains("Tooba.Promotion.Contracts.Merchandising", merch, StringComparison.Ordinal);
        Assert.Contains("IMerchandisingCampaignQuery", gate, StringComparison.Ordinal);
        Assert.Contains("MerchandisingPromotionTypeCodes.Amazing", merch, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Promotion.Application", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Promotion.Application", merch, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Promotion.Domain", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Promotion.Domain", merch, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/CatalogModule.cs"));
        Assert.Contains("MerchandisingStoreLandingReferenceGate", module, StringComparison.Ordinal);
        Assert.Contains("StoreLandingShellAdapter", module, StringComparison.Ordinal);
        Assert.Contains("StoreLandingMerchandisingAdapter", module, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.DoesNotContain("CatalogAdapters", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Promotion_contracts_owns_merchandising_campaign_query_seam()
    {
        var contracts = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Promotion/Tooba.Promotion.Contracts/Merchandising/MerchandisingCampaignContracts.cs"));
        Assert.Contains("namespace Tooba.Promotion.Contracts.Merchandising", contracts, StringComparison.Ordinal);
        Assert.Contains("interface IMerchandisingCampaignQuery", contracts, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Promotion/Tooba.Promotion.Application/Merchandising/MerchandisingCampaignRuntimePorts.cs")));
    }

    [Fact]
    public void Sot_records_catalog_adapters_host_zero()
    {
        var sot = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs", "architecture", "tmar-current-state.json"));
        Assert.Contains("\"hostCatalogAdaptersAmc\"", sot, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-HOST-CATALOGADAPTERS-AMC-001", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_CATALOGADAPTERS_AMC_001_CLOSED_HOST_ZERO", sot, StringComparison.Ordinal);
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
