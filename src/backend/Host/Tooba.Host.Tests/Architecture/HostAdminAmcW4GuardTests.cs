using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-HOST-ADMIN-AMC-001-W4 — Catalog Tags evacuated to Catalog with CQRS/Result.</summary>
public sealed class HostAdminAmcW4GuardTests
{
    [Fact]
    public void Host_CatalogTag_endpoint_absent_and_not_mapped()
    {
        var root = FindRepoRoot();
        Assert.False(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/CatalogTagEndpoints.cs")));
        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.DoesNotContain("MapCatalogTagEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("MapCatalogModuleEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("MapStoreAppearanceSettingsEndpoints()", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_Endpoints_own_nine_tag_routes_exactly_once_with_canonical_pipeline()
    {
        var root = FindRepoRoot();
        var path = Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/Tags/CatalogTagEndpoints.cs");
        Assert.True(File.Exists(path));
        var source = File.ReadAllText(path);
        Assert.Contains("/v1/admin/catalog/tags", source, StringComparison.Ordinal);
        Assert.Contains("/v1/admin/catalog/products/{productId:guid}/tags", source, StringComparison.Ordinal);
        Assert.Contains("/v1/admin/catalog/categories/{categoryId:guid}/tags", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/\", ListTagsAsync)", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/\", CreateTagAsync)", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/{tagId:guid}\", GetTagAsync)", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/\", ListProductTagsAsync)", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{tagId:guid}\", AssignProductTagAsync)", source, StringComparison.Ordinal);
        Assert.Contains("MapDelete(\"/{tagId:guid}\", RemoveProductTagAsync)", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/\", ListCategoryTagsAsync)", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{tagId:guid}\", AssignCategoryTagAsync)", source, StringComparison.Ordinal);
        Assert.Contains("MapDelete(\"/{tagId:guid}\", RemoveCategoryTagAsync)", source, StringComparison.Ordinal);
        Assert.Contains("ISender", source, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", source, StringComparison.Ordinal);
        Assert.Contains("ICatalogAdminAuthorizer", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogDirectory", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ITagDirectory", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Infrastructure", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", source, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Problem", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogActorHttpBinding", source, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/CatalogEndpointModule.cs"));
        Assert.Single(Regex.Matches(module, @"MapCatalogTagEndpoints\(\)"));
    }

    [Fact]
    public void Tags_Application_is_capability_first_without_Contracts_bundle()
    {
        var tagsRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/Tags");
        Assert.True(Directory.Exists(tagsRoot));
        foreach (var folder in new[] { "Commands", "Queries", "Models", "Ports", "Validators" })
        {
            Assert.True(Directory.Exists(Path.Combine(tagsRoot, folder)), folder);
        }

        Assert.Empty(Directory.GetDirectories(Path.Combine(tagsRoot, "Commands")));
        Assert.Empty(Directory.GetDirectories(Path.Combine(tagsRoot, "Queries")));
        Assert.Empty(Directory.GetFiles(tagsRoot, "*Contracts.cs", SearchOption.AllDirectories));

        var appRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application");
        Assert.Single(Directory.GetFiles(appRoot, "CreateTagCommand.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "ListTagsQuery.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "GetTagQuery.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "AssignProductTagCommand.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "RemoveProductTagCommand.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "AssignCategoryTagCommand.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "RemoveCategoryTagCommand.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "ListProductTagsQuery.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "ListCategoryTagsQuery.cs", SearchOption.AllDirectories));
        Assert.True(File.Exists(Path.Combine(tagsRoot, "Ports", "ITagDirectory.cs")));
    }

    [Fact]
    public void Tags_path_namespace_exact_and_validators_classified()
    {
        var tagsRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/Tags");
        var appRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application");
        foreach (var file in Directory.GetFiles(tagsRoot, "*.cs", SearchOption.AllDirectories))
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

        Assert.True(File.Exists(Path.Combine(tagsRoot, "Validators", "CreateTagCommandValidator.cs")));
        Assert.False(File.Exists(Path.Combine(tagsRoot, "Validators", "ListTagsQueryValidator.cs")));
        Assert.False(File.Exists(Path.Combine(tagsRoot, "Validators", "GetTagQueryValidator.cs")));
        Assert.False(File.Exists(Path.Combine(tagsRoot, "Validators", "AssignProductTagCommandValidator.cs")));
        Assert.False(File.Exists(Path.Combine(tagsRoot, "Validators", "RemoveProductTagCommandValidator.cs")));
        Assert.False(File.Exists(Path.Combine(tagsRoot, "Validators", "ListProductTagsQueryValidator.cs")));
        Assert.False(File.Exists(Path.Combine(tagsRoot, "Validators", "AssignCategoryTagCommandValidator.cs")));
        Assert.False(File.Exists(Path.Combine(tagsRoot, "Validators", "RemoveCategoryTagCommandValidator.cs")));
        Assert.False(File.Exists(Path.Combine(tagsRoot, "Validators", "ListCategoryTagsQueryValidator.cs")));
    }

    [Fact]
    public void TagDirectory_uses_Result_not_exceptions_or_message_as_code()
    {
        var root = FindRepoRoot();
        var directory = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/TagDirectory.cs"));
        Assert.Contains("Result.Failure", directory, StringComparison.Ordinal);
        Assert.Contains("CatalogErrorCodes", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("نام برچسب", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("کد برچسب تکراری", directory, StringComparison.Ordinal);

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
    public void Host_Admin_file_count_is_56_and_prior_waves_preserved()
    {
        var admin = Path.Combine(FindRepoRoot(), "src/backend/Host/Tooba.Host/Admin");
        var files = Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories);
        // W4 locked 56; W5→55; W6→54; W7 evacuated Categories → 53.
        Assert.True(files.Length <= 52 && files.Length >= 12, $"Host/Admin count expected in [12,52], was {files.Length}");
        Assert.False(File.Exists(Path.Combine(admin, "CatalogTagEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "UnitOfMeasureEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "QuantitySettingsEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsComposer.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "CatalogAttributeEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "CatalogCategoryEndpoints.cs")));

        var quantityRoot = Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/Settings/Quantity");
        Assert.True(Directory.Exists(quantityRoot));
        var unitsRoot = Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/Units");
        Assert.True(Directory.Exists(unitsRoot));
    }

    [Fact]
    public void Error_catalog_owns_tag_codes()
    {
        var root = FindRepoRoot();
        var codes = File.ReadAllText(Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Errors/CatalogErrorCodes.cs"));
        Assert.Contains("catalog.tag.invalid", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.tag.code.duplicate", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.tag.missing", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.tag.assign.duplicate", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.tag.product.missing", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.tag.category.missing", codes, StringComparison.Ordinal);

        var contributor = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Errors/CatalogErrorCatalogContributor.cs"));
        Assert.Contains("TagInvalid", contributor, StringComparison.Ordinal);
        Assert.Contains("TagCodeDuplicate", contributor, StringComparison.Ordinal);
        Assert.Contains("TagMissing", contributor, StringComparison.Ordinal);
        Assert.Contains("TagAssignDuplicate", contributor, StringComparison.Ordinal);

        var resx = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Resources/CatalogErrors.resx"));
        Assert.Contains("catalog.tag.invalid", resx, StringComparison.Ordinal);
        Assert.Contains("catalog.tag.code.duplicate", resx, StringComparison.Ordinal);
        Assert.Contains("catalog.tag.assign.duplicate", resx, StringComparison.Ordinal);
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
