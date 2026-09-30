using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-AMC-001-W34-TEMPLATE-SEEDS — evacuate Admin template/attribute seeds to Catalog.
/// </summary>
public sealed class HostAdminAmcW34TemplateSeedsGuardTests
{
    private static readonly string[] EvacuatedAdminSeeds =
    [
        "FashionTemplateCatalogSeed.cs",
        "IndustryBatchATemplateCatalogSeed.cs",
        "IndustryBatchBTemplateCatalogSeed.cs",
        "IndustryBatchCTemplateCatalogSeed.cs",
        "CatalogAttributeSchemaDevelopmentBootstrap.cs",
    ];

    [Fact]
    public void Template_and_attribute_seeds_absent_from_Host_Admin()
    {
        var root = FindRepoRoot();
        var admin = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin");
        foreach (var name in EvacuatedAdminSeeds)
        {
            Assert.False(File.Exists(Path.Combine(admin, name)), name);
        }
    }

    [Fact]
    public void Catalog_Development_owns_seeds_and_Host_keeps_no_seed_wrappers()
    {
        var root = FindRepoRoot();
        var catalogDev = Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Development");
        Assert.True(File.Exists(Path.Combine(catalogDev, "FashionTemplateCatalogSeed.cs")));
        Assert.True(File.Exists(Path.Combine(catalogDev, "IndustryBatchATemplateCatalogSeed.cs")));
        Assert.True(File.Exists(Path.Combine(catalogDev, "IndustryBatchBTemplateCatalogSeed.cs")));
        Assert.True(File.Exists(Path.Combine(catalogDev, "IndustryBatchCTemplateCatalogSeed.cs")));
        Assert.True(File.Exists(Path.Combine(catalogDev, "CatalogAttributeSchemaDevelopmentSeed.cs")));

        var fashion = File.ReadAllText(Path.Combine(catalogDev, "FashionTemplateCatalogSeed.cs"));
        Assert.Contains("namespace Tooba.Catalog.Infrastructure.Development", fashion, StringComparison.Ordinal);
        Assert.DoesNotContain("ControlPlaneRegistry", fashion, StringComparison.Ordinal);

        var attr = File.ReadAllText(Path.Combine(catalogDev, "CatalogAttributeSchemaDevelopmentSeed.cs"));
        Assert.Contains("ICatalogAttributeSchemaSellableEnricher", attr, StringComparison.Ordinal);
        Assert.DoesNotContain("ControlPlaneRegistry", attr, StringComparison.Ordinal);
        Assert.DoesNotContain("IOfferQueryGateway", attr, StringComparison.Ordinal);

        // Catalog owns the seeds; the eight Host seed wrappers were evacuated into Catalog
        // and replaced by a single Host composition seam.
        var hostDev = Path.Combine(root, "src/backend/Host/Tooba.Host/Development");
        foreach (var evacuated in new[]
        {
            "FashionTemplateCatalogSeedHost.cs",
            "IndustryBatchATemplateCatalogSeedHost.cs",
            "IndustryBatchBTemplateCatalogSeedHost.cs",
            "IndustryBatchCTemplateCatalogSeedHost.cs",
            "CatalogAttributeSchemaDevelopmentSeedHost.cs",
            "LandingPageDevelopmentSeedHost.cs",
            "StoreMenuDevelopmentSeedHost.cs",
        })
        {
            Assert.False(File.Exists(Path.Combine(hostDev, evacuated)), evacuated);
        }

        Assert.True(File.Exists(Path.Combine(hostDev, "DevelopmentTenantCommerceContext.cs")));

        // Catalog owns the schema/demo sellable workflow; the cross-module enricher moved out of Host
        // into Catalog.Infrastructure and reaches Offer/Party/Pricing/Inventory/Tax only via Contracts.
        Assert.False(File.Exists(Path.Combine(hostDev, "CatalogAttributeSchemaSellableEnricher.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Development/CatalogAttributeSchemaSellableEnricher.cs")));
    }

    [Fact]
    public void Program_wires_Catalog_seeds_via_single_Host_seam_and_catalog_owned_enricher()
    {
        var root = FindRepoRoot();
        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("DevelopmentSchemaMigrator.ApplyAsync", program, StringComparison.Ordinal);
        Assert.Contains("CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync", program, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogAttributeSchemaDevelopmentBootstrap", program, StringComparison.Ordinal);

        // Catalog-owned legacy seed orchestration lives in the Catalog Infrastructure seam, not in Host.
        var catalogSeam = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Development/CatalogDevelopmentSeed.cs"));
        Assert.Contains("CatalogAttributeSchemaDevelopmentSeed.ApplyAsync", catalogSeam, StringComparison.Ordinal);
        Assert.Contains("FashionTemplateCatalogSeed.ApplyAsync", catalogSeam, StringComparison.Ordinal);
        Assert.Contains("IndustryBatchATemplateCatalogSeed.ApplyAsync", catalogSeam, StringComparison.Ordinal);
        Assert.Contains("IndustryBatchBTemplateCatalogSeed.ApplyAsync", catalogSeam, StringComparison.Ordinal);
        Assert.Contains("IndustryBatchCTemplateCatalogSeed.ApplyAsync", catalogSeam, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogAttributeSchemaDevelopmentSeedHost", program, StringComparison.Ordinal);
        Assert.DoesNotContain("FashionTemplateCatalogSeedHost", program, StringComparison.Ordinal);
        // Host no longer owns or registers the Catalog enricher; Catalog registers its own implementation.
        Assert.DoesNotContain("ICatalogAttributeSchemaSellableEnricher", program, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogAttributeSchemaSellableEnricher", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Storefront_does_not_import_Host_Admin_for_template_constants()
    {
        var root = FindRepoRoot();
        var fashion = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Development/TemplateCatalog/FashionTemplatePreviewQuery.cs"));
        var industry = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Development/TemplateCatalog/IndustryPersistedTemplateCatalog.cs"));
        Assert.DoesNotContain("using Tooba.Host.Admin;", fashion, StringComparison.Ordinal);
        Assert.DoesNotContain("using Tooba.Host.Admin;", industry, StringComparison.Ordinal);
        Assert.Contains("Tooba.Catalog.Infrastructure.Development", fashion, StringComparison.Ordinal);
        Assert.Contains("Tooba.Catalog.Infrastructure.Development", industry, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Storefront/FashionTemplatePreviewQuery.cs")));
        Assert.False(File.Exists(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Storefront/IndustryPersistedTemplateCatalog.cs")));
        Assert.False(File.Exists(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Storefront/IndustryTemplatePreviewQuery.cs")));
    }

    [Fact]
    public void Host_Admin_count_15_StoreAppearance_and_HoldPolicy_evacuated()
    {
        var root = FindRepoRoot();
        var admin = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin");
        Assert.Equal(15, Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories).Length);
        Assert.False(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "HoldPolicySettingsEndpoints.cs")));
    }

    [Fact]
    public void SoT_and_evidence_hostAdminAmcTemplateSeeds_present()
    {
        var root = FindRepoRoot();
        var sot = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"hostAdminAmcTemplateSeeds\"", sot, StringComparison.Ordinal);
        Assert.True(Directory.Exists(Path.Combine(
            root, "docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W34-TEMPLATE-SEEDS")));
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
