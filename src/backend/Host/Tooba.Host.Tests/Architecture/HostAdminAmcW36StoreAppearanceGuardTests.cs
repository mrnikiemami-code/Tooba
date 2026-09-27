using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-AMC-001-W36-STORE-APPEARANCE — evacuate Host Admin store-appearance to Catalog.
/// </summary>
public sealed class HostAdminAmcW36StoreAppearanceGuardTests
{
    private static readonly Regex MapRouteRegex = new(@"Map(Get|Post|Put|Patch|Delete)\(", RegexOptions.Compiled);

    [Fact]
    public void Store_appearance_settings_owned_by_Catalog_and_absent_from_Host_Admin()
    {
        var root = FindRepoRoot();
        var hostEndpoints = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/StoreAppearanceSettingsEndpoints.cs");
        var hostComposer = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/StoreAppearanceSettingsComposer.cs");
        var hostProjector = Path.Combine(root, "src/backend/Host/Tooba.Host/Storefront/StoreAppearanceProjection.cs");
        Assert.False(File.Exists(hostEndpoints));
        Assert.False(File.Exists(hostComposer));
        Assert.False(File.Exists(hostProjector));

        var catalogEndpoints = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/Settings/StoreAppearanceSettingsEndpoints.cs"));
        Assert.Contains("MapGet(\"/\", GetAsync)", catalogEndpoints, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/\", PutAsync)", catalogEndpoints, StringComparison.Ordinal);
        Assert.Contains("/v1/admin/settings/appearance", catalogEndpoints, StringComparison.Ordinal);
        Assert.Contains("ICatalogAdminAuthorizer", catalogEndpoints, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", catalogEndpoints, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/CatalogEndpointModule.cs"));
        Assert.Contains("MapStoreAppearanceSettingsEndpoints()", module, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.DoesNotContain("MapStoreAppearanceSettingsEndpoints()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("StoreAppearanceSettingsComposer", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Host.Storefront.StoreAppearanceProjector", program, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/StoreAppearance/StoreAppearanceProjector.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Application/Settings/StoreAppearance/Queries/GetStoreAppearanceAdminSettingsQuery.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Application/StoreAppearanceSettingsWriteContracts.cs")));

        var catalogModule = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/CatalogModule.cs"));
        Assert.Contains("IStoreAppearanceProjector", catalogModule, StringComparison.Ordinal);
        _ = MapRouteRegex;
    }

    [Fact]
    public void Host_Admin_count_15_KEEP_platform_floor_StoreAppearance_evacuated()
    {
        var root = FindRepoRoot();
        var admin = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin");
        var adminCount = Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories).Length;
        Assert.Equal(15, adminCount);
        Assert.False(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsComposer.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "HoldPolicySettingsEndpoints.cs")));
    }

    [Fact]
    public void SoT_and_evidence_hostAdminAmcStoreAppearance_present()
    {
        var root = FindRepoRoot();
        var sot = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"hostAdminAmcStoreAppearance\"", sot, StringComparison.Ordinal);
        Assert.True(Directory.Exists(Path.Combine(
            root, "docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W36-STORE-APPEARANCE")));
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
