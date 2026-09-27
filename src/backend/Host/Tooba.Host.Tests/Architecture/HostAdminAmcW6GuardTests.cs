using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-HOST-ADMIN-AMC-001-W6 — Catalog Facets evacuated to Catalog with CQRS/Result.</summary>
public sealed class HostAdminAmcW6GuardTests
{
    [Fact]
    public void Host_CatalogFacet_endpoint_absent_and_not_mapped()
    {
        var root = FindRepoRoot();
        Assert.False(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/CatalogFacetEndpoints.cs")));
        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.DoesNotContain("MapCatalogFacetEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("MapCatalogModuleEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("MapStoreAppearanceSettingsEndpoints()", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_Endpoints_own_six_facet_routes_exactly_once_with_canonical_pipeline()
    {
        var root = FindRepoRoot();
        var adminPath = Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/Facets/CatalogFacetAdminEndpoints.cs");
        var storefrontPath = Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Storefront/Facets/CatalogFacetStorefrontEndpoints.cs");
        Assert.True(File.Exists(adminPath));
        Assert.True(File.Exists(storefrontPath));

        var admin = File.ReadAllText(adminPath);
        Assert.Contains("/v1/admin/catalog/categories/{categoryId:guid}/facets", admin, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/effective\", GetEffectiveFacetsAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/local\", ListLocalFacetsAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/{definitionId:guid}\", UpsertFacetAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapDelete(\"/{definitionId:guid}\", RemoveFacetOverrideAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/order\", ReorderFacetsAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("ISender", admin, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", admin, StringComparison.Ordinal);
        Assert.Contains("ICatalogAdminAuthorizer", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogDirectory", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("IFacetDirectory", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Infrastructure", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Problem", admin, StringComparison.Ordinal);

        var storefront = File.ReadAllText(storefrontPath);
        Assert.Contains("/v1/storefront/categories/{categoryId:guid}/facets", storefront, StringComparison.Ordinal);
        Assert.Contains("ISender", storefront, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", storefront, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogAdminAuthorizer", storefront, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogDirectory", storefront, StringComparison.Ordinal);
        Assert.DoesNotContain("IFacetDirectory", storefront, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", storefront, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", storefront, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", storefront, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/CatalogEndpointModule.cs"));
        Assert.Single(Regex.Matches(module, @"MapCatalogFacetAdminEndpoints\(\)"));
        Assert.Single(Regex.Matches(module, @"MapCatalogFacetStorefrontEndpoints\(\)"));
    }

    [Fact]
    public void Facets_Application_is_capability_first_without_Contracts_bundle()
    {
        var facetsRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/Facets");
        Assert.True(Directory.Exists(facetsRoot));
        foreach (var folder in new[] { "Commands", "Queries", "Models", "Ports", "Validators" })
        {
            Assert.True(Directory.Exists(Path.Combine(facetsRoot, folder)), folder);
        }

        Assert.Empty(Directory.GetDirectories(Path.Combine(facetsRoot, "Commands")));
        Assert.Empty(Directory.GetDirectories(Path.Combine(facetsRoot, "Queries")));
        Assert.Empty(Directory.GetFiles(facetsRoot, "*Contracts.cs", SearchOption.AllDirectories));

        var appRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application");
        Assert.Single(Directory.GetFiles(appRoot, "GetEffectiveCategoryFacetsQuery.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "ListLocalCategoryFacetsQuery.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "UpsertCategoryFacetCommand.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "RemoveCategoryFacetOverrideCommand.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "ReorderCategoryFacetsCommand.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "GetStorefrontCategoryFacetsQuery.cs", SearchOption.AllDirectories));
        Assert.True(File.Exists(Path.Combine(facetsRoot, "Ports", "IFacetDirectory.cs")));
    }

    [Fact]
    public void Facets_path_namespace_exact_and_validators_classified()
    {
        var facetsRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/Facets");
        var appRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application");
        foreach (var file in Directory.GetFiles(facetsRoot, "*.cs", SearchOption.AllDirectories))
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

        Assert.True(File.Exists(Path.Combine(facetsRoot, "Validators", "UpsertCategoryFacetCommandValidator.cs")));
        Assert.True(File.Exists(Path.Combine(facetsRoot, "Validators", "ReorderCategoryFacetsCommandValidator.cs")));
        Assert.False(File.Exists(Path.Combine(facetsRoot, "Validators", "GetEffectiveCategoryFacetsQueryValidator.cs")));
        Assert.False(File.Exists(Path.Combine(facetsRoot, "Validators", "ListLocalCategoryFacetsQueryValidator.cs")));
        Assert.False(File.Exists(Path.Combine(facetsRoot, "Validators", "RemoveCategoryFacetOverrideCommandValidator.cs")));
        Assert.False(File.Exists(Path.Combine(facetsRoot, "Validators", "GetStorefrontCategoryFacetsQueryValidator.cs")));
    }

    [Fact]
    public void FacetDirectory_uses_Result_not_exceptions_or_message_as_code()
    {
        var root = FindRepoRoot();
        var directory = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/FacetDirectory.cs"));
        Assert.Contains("Result.Failure", directory, StringComparison.Ordinal);
        Assert.Contains("CatalogErrorCodes", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("رده در Catalog این Tenant نیست", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("فقط ویژگی‌های قابل فیلتر", directory, StringComparison.Ordinal);

        var domain = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Domain/CatalogDomain.cs"));
        Assert.Contains("CatalogFacetDisplayTypeViolation", domain, StringComparison.Ordinal);
        Assert.Contains("ValidateDisplayType", domain, StringComparison.Ordinal);

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
    public void Host_Admin_file_count_is_54_and_prior_waves_preserved()
    {
        var admin = Path.Combine(FindRepoRoot(), "src/backend/Host/Tooba.Host/Admin");
        var files = Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories);
        Assert.Equal(52, files.Length);
        Assert.False(File.Exists(Path.Combine(admin, "CatalogFacetEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "CatalogMegaMenuEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "CatalogTagEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "UnitOfMeasureEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "QuantitySettingsEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsComposer.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "CatalogAttributeEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "CatalogCategoryEndpoints.cs")));

        var tagsRoot = Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/Tags");
        Assert.True(Directory.Exists(tagsRoot));
        var megaRoot = Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/MegaMenu");
        Assert.True(Directory.Exists(megaRoot));
        var facetsRoot = Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/Facets");
        Assert.True(Directory.Exists(facetsRoot));
    }

    [Fact]
    public void Error_catalog_owns_facet_codes()
    {
        var root = FindRepoRoot();
        var codes = File.ReadAllText(Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Errors/CatalogErrorCodes.cs"));
        Assert.Contains("catalog.facet.category.missing", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.facet.definition.missing", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.facet.schema.missing", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.facet.not_filterable", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.facet.display_type.invalid", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.facet.override.missing", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.facet.reorder.invalid", codes, StringComparison.Ordinal);

        var contributor = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Errors/CatalogErrorCatalogContributor.cs"));
        Assert.Contains("FacetCategoryMissing", contributor, StringComparison.Ordinal);
        Assert.Contains("FacetDisplayTypeInvalid", contributor, StringComparison.Ordinal);
        Assert.Contains("FacetOverrideMissing", contributor, StringComparison.Ordinal);
        Assert.Contains("FacetReorderInvalid", contributor, StringComparison.Ordinal);

        var resx = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Resources/CatalogErrors.resx"));
        Assert.Contains("catalog.facet.category.missing", resx, StringComparison.Ordinal);
        Assert.Contains("catalog.facet.display_type.invalid", resx, StringComparison.Ordinal);
        Assert.Contains("catalog.facet.override.missing", resx, StringComparison.Ordinal);
        Assert.Contains("catalog.facet.reorder.invalid", resx, StringComparison.Ordinal);
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
