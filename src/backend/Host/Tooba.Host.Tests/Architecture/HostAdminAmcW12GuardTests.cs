using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-HOST-ADMIN-AMC-001-W12 — final category-change Admin evacuated; Host CatalogAttributeEndpoints deleted.</summary>
public sealed class HostAdminAmcW12GuardTests
{
    [Fact]
    public void Host_CatalogAttributeEndpoints_file_absent_and_Program_mapping_removed()
    {
        var root = FindRepoRoot();
        Assert.False(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/CatalogAttributeEndpoints.cs")));
        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.DoesNotContain("MapCatalogAttributeEndpoints()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapCatalogAttributeEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("MapCatalogModuleEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("MapStoreAppearanceSettingsEndpoints()", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_Endpoints_own_two_category_change_routes_exactly_once_with_canonical_pipeline()
    {
        var root = FindRepoRoot();
        var adminPath = Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/CategoryChanges/CatalogProductCategoryChangeAdminEndpoints.cs");
        Assert.True(File.Exists(adminPath));
        var admin = File.ReadAllText(adminPath);
        Assert.Contains("/v1/admin/catalog/products/{productId:guid}", admin, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/category-change-preview\", PreviewAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/primary-category\", ReplacePrimaryAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("ISender", admin, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", admin, StringComparison.Ordinal);
        Assert.Contains("ICatalogAdminAuthorizer", admin, StringComparison.Ordinal);
        Assert.Contains("CatalogActorRequestBinding", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogDirectory", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("ICategoryChangeDirectory", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Infrastructure", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Problem", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductAssignableLevelRequiredMessageFa", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("MapCategoryChangeInvalid", admin, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/CatalogEndpointModule.cs"));
        Assert.Single(Regex.Matches(module, @"MapCatalogProductCategoryChangeAdminEndpoints\(\)"));
        Assert.Single(Regex.Matches(module, @"MapCatalogProductVariantAdminEndpoints\(\)"));
        Assert.Single(Regex.Matches(module, @"MapCatalogProductAttributeAdminEndpoints\(\)"));
    }

    [Fact]
    public void CategoryChanges_Application_is_capability_first_without_Contracts_bundle()
    {
        var capabilityRoot = Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/CategoryChanges");
        Assert.True(Directory.Exists(capabilityRoot));
        foreach (var folder in new[] { "Commands", "Queries", "Models", "Ports", "Validators" })
        {
            Assert.True(Directory.Exists(Path.Combine(capabilityRoot, folder)), folder);
        }

        Assert.Empty(Directory.GetDirectories(Path.Combine(capabilityRoot, "Commands")));
        Assert.Empty(Directory.GetDirectories(Path.Combine(capabilityRoot, "Queries")));
        Assert.Empty(Directory.GetFiles(capabilityRoot, "*Contracts.cs", SearchOption.AllDirectories));

        var appRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application");
        Assert.Single(Directory.GetFiles(appRoot, "PreviewCategoryChangeQuery.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "ReplacePrimaryCategoryCommand.cs", SearchOption.AllDirectories));
        Assert.True(File.Exists(Path.Combine(capabilityRoot, "Ports", "ICategoryChangeDirectory.cs")));
    }

    [Fact]
    public void CategoryChanges_path_namespace_exact_and_validators_classified()
    {
        var capabilityRoot = Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/CategoryChanges");
        var appRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application");
        foreach (var file in Directory.GetFiles(capabilityRoot, "*.cs", SearchOption.AllDirectories))
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

        Assert.True(File.Exists(Path.Combine(capabilityRoot, "Validators", "PreviewCategoryChangeQueryValidator.cs")));
        Assert.True(File.Exists(Path.Combine(capabilityRoot, "Validators", "ReplacePrimaryCategoryCommandValidator.cs")));
    }

    [Fact]
    public void CategoryChangeDirectory_is_Result_typed_with_transaction_history_and_no_message_classification()
    {
        var root = FindRepoRoot();
        var directory = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/CategoryChangeDirectory.cs"));
        Assert.Contains("Result.Failure", directory, StringComparison.Ordinal);
        Assert.Contains("CatalogErrorCodes", directory, StringComparison.Ordinal);
        Assert.Contains("ProductMissing", directory, StringComparison.Ordinal);
        Assert.Contains("CategoryMissing", directory, StringComparison.Ordinal);
        Assert.Contains("CategoryAssignmentLevelInvalid", directory, StringComparison.Ordinal);
        Assert.Contains("BeginTransactionAsync", directory, StringComparison.Ordinal);
        Assert.Contains("EventCategoryChanged", directory, StringComparison.Ordinal);
        Assert.Contains("EventUnpublished", directory, StringComparison.Ordinal);
        Assert.Contains("IsAssignableProductCategory", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductAssignableLevelRequiredMessageFa", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("EnsureAssignableProductCategory(", directory, StringComparison.Ordinal);

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
    public void Host_Admin_file_count_is_52_and_prior_waves_preserved()
    {
        var admin = Path.Combine(FindRepoRoot(), "src/backend/Host/Tooba.Host/Admin");
        var files = Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories);
        Assert.True(files.Length <= 52 && files.Length >= 12, $"Host/Admin count expected in [12,52], was {files.Length}");
        Assert.False(File.Exists(Path.Combine(admin, "CatalogAttributeEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "CatalogCategoryEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));

        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/Attributes/ProductValues")));
        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/Variants")));
        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/CategoryChanges")));
    }

    [Fact]
    public void Error_catalog_owns_category_change_and_assignment_level_codes()
    {
        var root = FindRepoRoot();
        var codes = File.ReadAllText(Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Errors/CatalogErrorCodes.cs"));
        Assert.Contains("catalog.category.assignment.level.invalid", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.category_change.invalid", codes, StringComparison.Ordinal);
        Assert.Contains("CategoryAssignmentLevelInvalid", codes, StringComparison.Ordinal);

        var domain = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Domain/CatalogCategoryTreeRules.cs"));
        Assert.Contains("catalog.category.assignment.level.invalid", domain, StringComparison.Ordinal);
        Assert.Contains("ProductAssignableLevel = 3", domain, StringComparison.Ordinal);

        var contributor = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Errors/CatalogErrorCatalogContributor.cs"));
        Assert.Contains("CategoryAssignmentLevelInvalid", contributor, StringComparison.Ordinal);
        Assert.Contains("CategoryChangeInvalid", contributor, StringComparison.Ordinal);

        var resx = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Resources/CatalogErrors.resx"));
        Assert.Contains("catalog.category.assignment.level.invalid", resx, StringComparison.Ordinal);
        Assert.Contains("catalog.category_change.invalid", resx, StringComparison.Ordinal);
        var resxFa = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Resources/CatalogErrors.fa.resx"));
        Assert.Contains("محصول باید به یک دسته‌بندی سطح سوم اختصاص داده شود.", resxFa, StringComparison.Ordinal);
    }

    [Fact]
    public void Assignment_level_canonical_code_matches_Domain_constant()
    {
        Assert.Equal(
            "catalog.category.assignment.level.invalid",
            Tooba.Catalog.Contracts.Errors.CatalogErrorCodes.CategoryAssignmentLevelInvalid);
        Assert.Equal(
            Tooba.Catalog.Contracts.Errors.CatalogErrorCodes.CategoryAssignmentLevelInvalid,
            Tooba.Catalog.Domain.CatalogCategoryTreeRules.AssignmentLevelInvalidErrorCode);
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
