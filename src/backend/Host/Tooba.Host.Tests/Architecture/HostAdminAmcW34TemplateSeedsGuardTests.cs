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
    public void Catalog_Development_owns_seeds_and_Host_wrappers_exist()
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

        var hostDev = Path.Combine(root, "src/backend/Host/Tooba.Host/Development");
        Assert.True(File.Exists(Path.Combine(hostDev, "FashionTemplateCatalogSeedHost.cs")));
        Assert.True(File.Exists(Path.Combine(hostDev, "IndustryBatchATemplateCatalogSeedHost.cs")));
        Assert.True(File.Exists(Path.Combine(hostDev, "IndustryBatchBTemplateCatalogSeedHost.cs")));
        Assert.True(File.Exists(Path.Combine(hostDev, "IndustryBatchCTemplateCatalogSeedHost.cs")));
        Assert.True(File.Exists(Path.Combine(hostDev, "CatalogAttributeSchemaDevelopmentSeedHost.cs")));
        Assert.True(File.Exists(Path.Combine(hostDev, "CatalogAttributeSchemaSellableEnricher.cs")));

        var enricher = File.ReadAllText(Path.Combine(hostDev, "CatalogAttributeSchemaSellableEnricher.cs"));
        Assert.Contains("ICatalogAttributeSchemaSellableEnricher", enricher, StringComparison.Ordinal);
        Assert.Contains("namespace Tooba.Host.Development", enricher, StringComparison.Ordinal);
    }

    [Fact]
    public void Program_wires_Catalog_seed_hosts_and_sellable_enricher()
    {
        var root = FindRepoRoot();
        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("CatalogAttributeSchemaDevelopmentSeedHost.ApplyAsync", program, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogAttributeSchemaDevelopmentBootstrap", program, StringComparison.Ordinal);
        Assert.Contains("FashionTemplateCatalogSeedHost.ApplyAsync", program, StringComparison.Ordinal);
        Assert.Contains("IndustryBatchATemplateCatalogSeedHost.ApplyAsync", program, StringComparison.Ordinal);
        Assert.Contains("IndustryBatchBTemplateCatalogSeedHost.ApplyAsync", program, StringComparison.Ordinal);
        Assert.Contains("IndustryBatchCTemplateCatalogSeedHost.ApplyAsync", program, StringComparison.Ordinal);
        Assert.Contains("ICatalogAttributeSchemaSellableEnricher", program, StringComparison.Ordinal);
        Assert.Contains("CatalogAttributeSchemaSellableEnricher", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Storefront_does_not_import_Host_Admin_for_template_constants()
    {
        var root = FindRepoRoot();
        var fashion = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Storefront/FashionTemplatePreviewQuery.cs"));
        var industry = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Storefront/IndustryPersistedTemplateCatalog.cs"));
        Assert.DoesNotContain("using Tooba.Host.Admin;", fashion, StringComparison.Ordinal);
        Assert.DoesNotContain("using Tooba.Host.Admin;", industry, StringComparison.Ordinal);
        Assert.Contains("Tooba.Catalog.Infrastructure.Development", fashion, StringComparison.Ordinal);
        Assert.Contains("Tooba.Catalog.Infrastructure.Development", industry, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_Admin_count_18_StoreAppearance_and_HoldPolicy_deferred()
    {
        var root = FindRepoRoot();
        var admin = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin");
        Assert.Equal(18, Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories).Length);
        Assert.True(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(admin, "HoldPolicySettingsEndpoints.cs")));
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
