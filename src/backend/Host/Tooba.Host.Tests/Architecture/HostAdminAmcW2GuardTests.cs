using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-HOST-ADMIN-AMC-001-W2 — Quantity settings evacuated; StoreAppearance deferred.</summary>
public sealed class HostAdminAmcW2GuardTests
{
    [Fact]
    public void Host_Admin_no_longer_maps_quantity_settings()
    {
        var root = FindRepoRoot();
        Assert.False(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/QuantitySettingsEndpoints.cs")));
        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.DoesNotContain("MapQuantitySettingsEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("MapCatalogModuleEndpoints()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapStoreAppearanceSettingsEndpoints()", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_Endpoints_own_quantity_routes_with_ISender_and_ApiResponseFactory()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/Settings/QuantitySettingsEndpoints.cs");
        Assert.True(File.Exists(path));
        var source = File.ReadAllText(path);
        Assert.Contains("/v1/admin/settings/quantity-rounding", source, StringComparison.Ordinal);
        Assert.Contains("ISender", source, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", source, StringComparison.Ordinal);
        Assert.Contains("ICatalogAdminAuthorizer", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogLookupGateway", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", source, StringComparison.Ordinal);
        Assert.DoesNotContain("StoreAppearanceProjector", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_Endpoints_csproj_has_no_Host_or_Infrastructure()
    {
        var csproj = Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Tooba.Catalog.Endpoints.csproj");
        var refs = XDocument.Load(csproj)
            .Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .ToArray();
        Assert.DoesNotContain(refs, r => r.Contains("Tooba.Host", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains(".Infrastructure", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Catalog_projects_have_no_Host_Storefront_reference()
    {
        var catalogRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog");
        foreach (var csproj in Directory.GetFiles(catalogRoot, "*.csproj", SearchOption.AllDirectories))
        {
            var refs = XDocument.Load(csproj)
                .Descendants("ProjectReference")
                .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
                .ToArray();
            Assert.DoesNotContain(refs, r => r.Contains("Tooba.Host", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(refs, r => r.Contains("Host.Storefront", StringComparison.OrdinalIgnoreCase));
        }

        foreach (var cs in Directory.GetFiles(catalogRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (cs.Contains($"{Path.DirectorySeparatorChar}Persistence{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(cs);
            Assert.DoesNotContain("Tooba.Host.Storefront", text, StringComparison.Ordinal);
            // StoreAppearanceProjector is Catalog-owned (W36); Host.Storefront coupling remains forbidden.
        }
    }

    [Fact]
    public void Host_Admin_file_count_after_quantity_evacuation_preserved_through_W5()
    {
        // Historical W2: 59 → 58 (Quantity). W3: 58 → 57 (UoM). W4: 57 → 56 (Tags). W5: 56 → 55 (MegaMenu).
        var admin = Path.Combine(FindRepoRoot(), "src/backend/Host/Tooba.Host/Admin");
        var files = Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories);
        Assert.True(files.Length <= 52 && files.Length >= 12, $"Host/Admin count expected in [12,52], was {files.Length}");
        Assert.False(File.Exists(Path.Combine(admin, "QuantitySettingsEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "UnitOfMeasureEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "CatalogTagEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "CatalogMegaMenuEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsComposer.cs")));
    }

    [Fact]
    public void W1_foundation_still_wired()
    {
        var program = File.ReadAllText(Path.Combine(FindRepoRoot(), "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("AddCatalogEndpointPresentation()", program, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(program, @"MapCatalogModuleEndpoints\(\)"));
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
