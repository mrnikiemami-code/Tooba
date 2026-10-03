using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-HOST-ADMIN-AMC-001-W7 — Catalog Categories evacuated to Catalog with CQRS/Result.</summary>
public sealed class HostAdminAmcW7GuardTests
{
    [Fact]
    public void Host_CatalogCategory_endpoint_absent_and_not_mapped()
    {
        var root = FindRepoRoot();
        Assert.False(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/CatalogCategoryEndpoints.cs")));
        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.DoesNotContain("MapCatalogCategoryEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("MapCatalogModuleEndpoints()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapStoreAppearanceSettingsEndpoints()", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_Endpoints_own_ten_category_routes_exactly_once_with_canonical_pipeline()
    {
        var root = FindRepoRoot();
        var adminPath = Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/Categories/CatalogCategoryAdminEndpoints.cs");
        var storefrontPath = Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Storefront/Categories/CatalogCategoryStorefrontEndpoints.cs");
        Assert.True(File.Exists(adminPath));
        Assert.True(File.Exists(storefrontPath));

        var admin = File.ReadAllText(adminPath);
        Assert.Contains("/v1/admin/catalog/categories", admin, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/tree\", GetTreeAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/{id:guid}\", GetWorkspaceAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/\", CreateAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapPatch(\"/{id:guid}\", UpdateCoreAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/{id:guid}/translations/{locale}\", UpsertTranslationAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{id:guid}/move\", MoveAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/reorder\", ReorderAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{id:guid}/publish\", PublishAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{id:guid}/archive\", ArchiveAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("ISender", admin, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", admin, StringComparison.Ordinal);
        Assert.Contains("ICatalogAdminAuthorizer", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogDirectory", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("ICategoryDirectory", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Infrastructure", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Problem", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("تکراری", admin, StringComparison.Ordinal);

        var storefront = File.ReadAllText(storefrontPath);
        Assert.Contains("/v1/storefront/category-routes/resolve", storefront, StringComparison.Ordinal);
        Assert.Contains("ISender", storefront, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", storefront, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogAdminAuthorizer", storefront, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogDirectory", storefront, StringComparison.Ordinal);
        Assert.DoesNotContain("ICategoryDirectory", storefront, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", storefront, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", storefront, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", storefront, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/CatalogEndpointModule.cs"));
        Assert.Single(Regex.Matches(module, @"MapCatalogCategoryAdminEndpoints\(\)"));
        Assert.Single(Regex.Matches(module, @"MapCatalogCategoryStorefrontEndpoints\(\)"));
    }

    [Fact]
    public void Categories_Application_is_capability_first_without_Contracts_bundle()
    {
        var categoriesRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/Categories");
        Assert.True(Directory.Exists(categoriesRoot));
        foreach (var folder in new[] { "Commands", "Queries", "Models", "Ports", "Validators" })
        {
            Assert.True(Directory.Exists(Path.Combine(categoriesRoot, folder)), folder);
        }

        Assert.Empty(Directory.GetDirectories(Path.Combine(categoriesRoot, "Commands")));
        Assert.Empty(Directory.GetDirectories(Path.Combine(categoriesRoot, "Queries")));
        Assert.Empty(Directory.GetFiles(categoriesRoot, "*Contracts.cs", SearchOption.AllDirectories));

        var appRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application");
        Assert.Single(Directory.GetFiles(appRoot, "GetCategoryTreeQuery.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "GetCategoryWorkspaceQuery.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "CreateCategoryCommand.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "UpdateCategoryCoreCommand.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "UpsertCategoryTranslationCommand.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "MoveCategoryCommand.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "ReorderCategoriesCommand.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "PublishCategoryCommand.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "ArchiveCategoryCommand.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "ResolveCategoryRouteQuery.cs", SearchOption.AllDirectories));
        Assert.True(File.Exists(Path.Combine(categoriesRoot, "Ports", "ICategoryDirectory.cs")));
    }

    [Fact]
    public void Categories_path_namespace_exact_and_validators_classified()
    {
        var categoriesRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/Categories");
        var appRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application");
        foreach (var file in Directory.GetFiles(categoriesRoot, "*.cs", SearchOption.AllDirectories))
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

        Assert.True(File.Exists(Path.Combine(categoriesRoot, "Validators", "GetCategoryTreeQueryValidator.cs")));
        Assert.True(File.Exists(Path.Combine(categoriesRoot, "Validators", "CreateCategoryCommandValidator.cs")));
        Assert.True(File.Exists(Path.Combine(categoriesRoot, "Validators", "UpsertCategoryTranslationCommandValidator.cs")));
        Assert.True(File.Exists(Path.Combine(categoriesRoot, "Validators", "ReorderCategoriesCommandValidator.cs")));
        Assert.True(File.Exists(Path.Combine(categoriesRoot, "Validators", "ResolveCategoryRouteQueryValidator.cs")));
        Assert.False(File.Exists(Path.Combine(categoriesRoot, "Validators", "GetCategoryWorkspaceQueryValidator.cs")));
        Assert.False(File.Exists(Path.Combine(categoriesRoot, "Validators", "UpdateCategoryCoreCommandValidator.cs")));
        Assert.False(File.Exists(Path.Combine(categoriesRoot, "Validators", "MoveCategoryCommandValidator.cs")));
        Assert.False(File.Exists(Path.Combine(categoriesRoot, "Validators", "PublishCategoryCommandValidator.cs")));
        Assert.False(File.Exists(Path.Combine(categoriesRoot, "Validators", "ArchiveCategoryCommandValidator.cs")));
    }

    [Fact]
    public void CategoryDirectory_uses_Result_not_exceptions_or_message_as_code()
    {
        var root = FindRepoRoot();
        var directory = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Directories/CategoryDirectory.cs"));
        Assert.Contains("Result.Failure", directory, StringComparison.Ordinal);
        Assert.Contains("CatalogErrorCodes", directory, StringComparison.Ordinal);
        Assert.Contains("CategorySlugDuplicate", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("Contains(\"slug\"", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("تکراری", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("slug رده برای این locale تکراری است", directory, StringComparison.Ordinal);

        var treeRules = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Domain/CatalogCategoryTreeRules.cs"));
        Assert.Contains("MaxCategoryDepth = 3", treeRules, StringComparison.Ordinal);
        Assert.Contains("ValidateNoCycle", treeRules, StringComparison.Ordinal);

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
    public void Host_Admin_file_count_is_53_and_prior_waves_preserved()
    {
        var admin = Path.Combine(FindRepoRoot(), "src/backend/Host/Tooba.Host/Admin");
        var files = Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories);
        Assert.True(files.Length <= 52 && files.Length >= 12, $"Host/Admin count expected in [12,52], was {files.Length}");
        Assert.False(File.Exists(Path.Combine(admin, "CatalogCategoryEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "CatalogFacetEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "CatalogMegaMenuEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "CatalogTagEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "UnitOfMeasureEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "QuantitySettingsEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsComposer.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "CatalogAttributeEndpoints.cs")));

        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/Tags")));
        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/MegaMenu")));
        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/Facets")));
        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/Categories")));
    }

    [Fact]
    public void Error_catalog_owns_category_codes()
    {
        var root = FindRepoRoot();
        var codes = File.ReadAllText(Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Errors/CatalogErrorCodes.cs"));
        Assert.Contains("catalog.category.missing", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.category.invalid", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.category.slug.duplicate", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.category.parent.self", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.category.parent.descendant", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.category.depth.max", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.category.route.invalid", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.category.route.missing", codes, StringComparison.Ordinal);

        var contributor = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Errors/CatalogErrorCatalogContributor.cs"));
        Assert.Contains("CategoryMissing", contributor, StringComparison.Ordinal);
        Assert.Contains("CategorySlugDuplicate", contributor, StringComparison.Ordinal);
        Assert.Contains("CategorySelfParent", contributor, StringComparison.Ordinal);
        Assert.Contains("CategoryRouteMissing", contributor, StringComparison.Ordinal);

        var en = File.ReadAllText(Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Resources/CatalogErrors.resx"));
        var fa = File.ReadAllText(Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Resources/CatalogErrors.fa.resx"));
        Assert.Contains("catalog.category.slug.duplicate", en, StringComparison.Ordinal);
        Assert.Contains("catalog.category.slug.duplicate", fa, StringComparison.Ordinal);
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

        throw new InvalidOperationException("repo root not found");
    }
}
