using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-AMC-001-W33 — Host Admin Merchandising evacuated to Promotion module.
/// </summary>
public sealed class HostAdminAmcW33MerchandisingGuardTests
{
    private static readonly Regex MapRouteRegex = new(
        @"\bMap(Get|Post|Put|Patch|Delete)\s*\(",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Merchandising_admin_routes_owned_by_Promotion_and_absent_from_Host_Admin()
    {
        var root = FindRepoRoot();
        var admin = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin");
        Assert.False(File.Exists(Path.Combine(admin, "MerchandisingCampaignAdminEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "MerchandisingCampaignDevelopmentSeed.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "MerchandisingStoreLandingReferenceGate.cs")));

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.DoesNotContain("MapMerchandisingCampaignAdminEndpoints", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MerchandisingCampaignAdminComposer", program, StringComparison.Ordinal);
        Assert.Contains("MapPromotionEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("CatalogAdapters.MerchandisingStoreLandingReferenceGate", program, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/Promotion/Tooba.Promotion.Endpoints/PromotionEndpointModule.cs"));
        Assert.Contains("MerchandisingCampaignAdminEndpoints.Map", module, StringComparison.Ordinal);
        Assert.Contains("PromotionAdminAuthorizer", module, StringComparison.Ordinal);

        var endpoints = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/Promotion/Tooba.Promotion.Endpoints/Admin/MerchandisingCampaignAdminEndpoints.cs"));
        Assert.Equal(12, MapRouteRegex.Matches(endpoints).Count);
        Assert.Contains("ApiResponseFactory", endpoints, StringComparison.Ordinal);
        Assert.Contains("IPromotionAdminAuthorizer", endpoints, StringComparison.Ordinal);
        Assert.Contains("/v1/admin/merchandising-campaigns", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("PartyDbContext", endpoints, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Promotion/Tooba.Promotion.Infrastructure/Merchandising/MerchandisingCampaignAdminComposer.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Promotion/Tooba.Promotion.Infrastructure/Development/MerchandisingCampaignDevelopmentSeed.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Host/Tooba.Host/CatalogAdapters/MerchandisingStoreLandingReferenceGate.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Host/Tooba.Host/Admin/HostPromotionAdminAuthorizer.cs")));
    }

    [Fact]
    public void Merchandising_CQRS_and_contracts_enrichment_ports_exist()
    {
        var root = FindRepoRoot();
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Promotion/Tooba.Promotion.Application/Merchandising/Admin/MerchandisingCampaignAdminCqrs.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Promotion/Tooba.Promotion.Application/Merchandising/IMerchandisingCampaignAdminComposer.cs")));

        var composer = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/Promotion/Tooba.Promotion.Infrastructure/Merchandising/MerchandisingCampaignAdminComposer.cs"));
        Assert.Contains("ICatalogVariantLookup", composer, StringComparison.Ordinal);
        Assert.Contains("IPartyLookup", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("PartyDbContext", composer, StringComparison.Ordinal);

        var catalogLookup = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/ICatalogVariantLookup.cs"));
        Assert.Contains("GetPublishedVariantIdsAsync", catalogLookup, StringComparison.Ordinal);

        var promoModule = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/Promotion/Tooba.Promotion.Infrastructure/DependencyInjection/PromotionModule.cs"));
        Assert.Contains("IMerchandisingCampaignAdminComposer", promoModule, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_Admin_count_15_StoreAppearance_evacuated_PW_shells_ABSENT()
    {
        var root = FindRepoRoot();
        var admin = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin");
        Assert.Equal(15, Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories).Length);
        Assert.False(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "ProductWorkspaceEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "MerchandisingCampaignAdminEndpoints.cs")));
    }

    [Fact]
    public void SoT_and_evidence_hostAdminAmcMerchandising_present()
    {
        var root = FindRepoRoot();
        var sot = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"hostAdminAmcMerchandising\"", sot, StringComparison.Ordinal);
        Assert.True(Directory.Exists(Path.Combine(
            root, "docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W33-MERCHANDISING")));
    }

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
