using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-HOST-ADMIN-AMC-001-W5 — Catalog MegaMenu evacuated to Catalog with CQRS/Result.</summary>
public sealed class HostAdminAmcW5GuardTests
{
    [Fact]
    public void Host_CatalogMegaMenu_endpoint_absent_and_not_mapped()
    {
        var root = FindRepoRoot();
        Assert.False(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/CatalogMegaMenuEndpoints.cs")));
        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.DoesNotContain("MapCatalogMegaMenuEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("MapCatalogModuleEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("MapStoreAppearanceSettingsEndpoints()", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_Endpoints_own_five_megamenu_routes_exactly_once_with_canonical_pipeline()
    {
        var root = FindRepoRoot();
        var adminPath = Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/MegaMenu/CatalogMegaMenuAdminEndpoints.cs");
        var storefrontPath = Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Storefront/MegaMenu/CatalogMegaMenuStorefrontEndpoints.cs");
        Assert.True(File.Exists(adminPath));
        Assert.True(File.Exists(storefrontPath));

        var admin = File.ReadAllText(adminPath);
        Assert.Contains("/v1/admin/catalog/categories/{categoryId:guid}/mega-menu", admin, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"\", GetCategoryMegaMenuAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/placement-options\", ListPlacementOptionsAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"\", UpsertCategoryMegaMenuAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapDelete(\"\", RemoveCategoryMegaMenuAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("ISender", admin, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", admin, StringComparison.Ordinal);
        Assert.Contains("ICatalogAdminAuthorizer", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogDirectory", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("IMegaMenuDirectory", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Infrastructure", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Problem", admin, StringComparison.Ordinal);

        var storefront = File.ReadAllText(storefrontPath);
        Assert.Contains("/v1/storefront/mega-menu", storefront, StringComparison.Ordinal);
        Assert.Contains("ISender", storefront, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", storefront, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogAdminAuthorizer", storefront, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogDirectory", storefront, StringComparison.Ordinal);
        Assert.DoesNotContain("IMegaMenuDirectory", storefront, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", storefront, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", storefront, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", storefront, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/CatalogEndpointModule.cs"));
        Assert.Single(Regex.Matches(module, @"MapCatalogMegaMenuAdminEndpoints\(\)"));
        Assert.Single(Regex.Matches(module, @"MapCatalogMegaMenuStorefrontEndpoints\(\)"));
    }

    [Fact]
    public void MegaMenu_Application_is_capability_first_without_Contracts_bundle()
    {
        var megaRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/MegaMenu");
        Assert.True(Directory.Exists(megaRoot));
        foreach (var folder in new[] { "Commands", "Queries", "Models", "Ports", "Validators" })
        {
            Assert.True(Directory.Exists(Path.Combine(megaRoot, folder)), folder);
        }

        Assert.Empty(Directory.GetDirectories(Path.Combine(megaRoot, "Commands")));
        Assert.Empty(Directory.GetDirectories(Path.Combine(megaRoot, "Queries")));
        Assert.Empty(Directory.GetFiles(megaRoot, "*Contracts.cs", SearchOption.AllDirectories));

        var appRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application");
        Assert.Single(Directory.GetFiles(appRoot, "GetCategoryMegaMenuQuery.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "ListMegaMenuPlacementOptionsQuery.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "UpsertCategoryMegaMenuCommand.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "RemoveCategoryMegaMenuCommand.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "GetStorefrontMegaMenuQuery.cs", SearchOption.AllDirectories));
        Assert.True(File.Exists(Path.Combine(megaRoot, "Ports", "IMegaMenuDirectory.cs")));
    }

    [Fact]
    public void MegaMenu_path_namespace_exact_and_validators_classified()
    {
        var megaRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/MegaMenu");
        var appRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application");
        foreach (var file in Directory.GetFiles(megaRoot, "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(appRoot, file).Replace('\\', '/');
            var expectedNs = "Tooba.Catalog.Application." + Path.GetDirectoryName(relative)!
                .Replace('/', '.')
                .Replace('\\', '.');
            var text = File.ReadAllText(file);
            Assert.True(
                Regex.IsMatch(text, $@"namespace\s+{Regex.Escape(expectedNs)}\s*;", RegexOptions.CultureInvariant),
                $"{relative} expected namespace {expectedNs}");
        }

        Assert.True(File.Exists(Path.Combine(megaRoot, "Validators", "UpsertCategoryMegaMenuCommandValidator.cs")));
        Assert.False(File.Exists(Path.Combine(megaRoot, "Validators", "GetCategoryMegaMenuQueryValidator.cs")));
        Assert.False(File.Exists(Path.Combine(megaRoot, "Validators", "ListMegaMenuPlacementOptionsQueryValidator.cs")));
        Assert.False(File.Exists(Path.Combine(megaRoot, "Validators", "RemoveCategoryMegaMenuCommandValidator.cs")));
        Assert.False(File.Exists(Path.Combine(megaRoot, "Validators", "GetStorefrontMegaMenuQueryValidator.cs")));
    }

    [Fact]
    public void MegaMenuDirectory_uses_Result_not_exceptions_or_message_as_code()
    {
        var root = FindRepoRoot();
        var directory = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/MegaMenuDirectory.cs"));
        Assert.Contains("Result.Failure", directory, StringComparison.Ordinal);
        Assert.Contains("CatalogErrorCodes", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("رده در Catalog این Tenant نیست", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("ابتدا زیرمجموعه‌های presentation", directory, StringComparison.Ordinal);

        var endpointsCsproj = Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Tooba.Catalog.Endpoints.csproj");
        var endpointRefs = XDocument.Load(endpointsCsproj)
            .Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .ToArray();
        Assert.DoesNotContain(endpointRefs, r => r.Contains(".Infrastructure", StringComparison.OrdinalIgnoreCase));

        foreach (var csproj in Directory.GetFiles(Path.Combine(root, "src/backend/Modules/Catalog"), "*.csproj", SearchOption.AllDirectories))
        {
            var projectRefs = XDocument.Load(csproj)
                .Descendants("ProjectReference")
                .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
                .ToArray();
            Assert.DoesNotContain(projectRefs, r => r.Contains("Tooba.Host", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Host_Admin_file_count_is_55_and_prior_waves_preserved()
    {
        var admin = Path.Combine(FindRepoRoot(), "src/backend/Host/Tooba.Host/Admin");
        var files = Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories);
        Assert.Equal(55, files.Length);
        Assert.False(File.Exists(Path.Combine(admin, "CatalogMegaMenuEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "CatalogTagEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "UnitOfMeasureEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "QuantitySettingsEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsComposer.cs")));
        Assert.True(File.Exists(Path.Combine(admin, "CatalogAttributeEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(admin, "CatalogCategoryEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(admin, "CatalogFacetEndpoints.cs")));

        var tagsRoot = Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/Tags");
        Assert.True(Directory.Exists(tagsRoot));
        var megaRoot = Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/MegaMenu");
        Assert.True(Directory.Exists(megaRoot));
    }

    [Fact]
    public void Error_catalog_owns_megamenu_codes()
    {
        var root = FindRepoRoot();
        var codes = File.ReadAllText(Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Errors/CatalogErrorCodes.cs"));
        Assert.Contains("catalog.megamenu.category.missing", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.megamenu.placement.invalid", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.megamenu.remove.has_children", codes, StringComparison.Ordinal);

        var contributor = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Errors/CatalogErrorCatalogContributor.cs"));
        Assert.Contains("MegaMenuCategoryMissing", contributor, StringComparison.Ordinal);
        Assert.Contains("MegaMenuPlacementInvalid", contributor, StringComparison.Ordinal);
        Assert.Contains("MegaMenuRemoveHasChildren", contributor, StringComparison.Ordinal);

        var resx = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Resources/CatalogErrors.resx"));
        Assert.Contains("catalog.megamenu.category.missing", resx, StringComparison.Ordinal);
        Assert.Contains("catalog.megamenu.placement.invalid", resx, StringComparison.Ordinal);
        Assert.Contains("catalog.megamenu.remove.has_children", resx, StringComparison.Ordinal);
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
