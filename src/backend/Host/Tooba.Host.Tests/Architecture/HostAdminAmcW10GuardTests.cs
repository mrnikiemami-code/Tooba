using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-HOST-ADMIN-AMC-001-W10 — Product Attribute Editor/Readiness evacuated; Host file retained partial.</summary>
public sealed class HostAdminAmcW10GuardTests
{
    [Fact]
    public void Host_CatalogAttribute_file_deleted_after_W12_and_product_attribute_routes_remain_Catalog_owned()
    {
        var root = FindRepoRoot();
        var hostPath = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/CatalogAttributeEndpoints.cs");
        Assert.False(File.Exists(hostPath));

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.DoesNotContain("MapCatalogAttributeEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("MapCatalogModuleEndpoints()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapStoreAppearanceSettingsEndpoints()", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_Endpoints_own_four_product_attribute_routes_exactly_once_with_canonical_pipeline()
    {
        var root = FindRepoRoot();
        var adminPath = Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/Attributes/ProductValues/CatalogProductAttributeAdminEndpoints.cs");
        Assert.True(File.Exists(adminPath));
        var admin = File.ReadAllText(adminPath);
        Assert.Contains("/v1/admin/catalog/products/{productId:guid}", admin, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/attributes\", GetEditorStateAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/attributes\", SetAttributesAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/attributes/readiness\", GetReadinessAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/attributes/{definitionId:guid}\", SetAttributeAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("ISender", admin, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", admin, StringComparison.Ordinal);
        Assert.Contains("ICatalogAdminAuthorizer", admin, StringComparison.Ordinal);
        Assert.Contains("CatalogActorRequestBinding", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogDirectory", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("IProductAttributeDirectory", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Infrastructure", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Offer", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Problem", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("MapAttributeInvalid", admin, StringComparison.Ordinal);

        var binding = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/CatalogActorRequestBinding.cs"));
        Assert.Contains("ICatalogActorContext", binding, StringComparison.Ordinal);
        Assert.Contains("IActorDisplayLookup", binding, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", binding, StringComparison.Ordinal);
        Assert.DoesNotContain("OperatorProfile.Application", binding, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/CatalogEndpointModule.cs"));
        Assert.Single(Regex.Matches(module, @"MapCatalogProductAttributeAdminEndpoints\(\)"));
        Assert.Single(Regex.Matches(module, @"MapCatalogCategoryAttributeSchemaAdminEndpoints\(\)"));
        Assert.Single(Regex.Matches(module, @"MapCatalogAttributeDefinitionAdminEndpoints\(\)"));
    }

    [Fact]
    public void Attribute_ProductValues_Application_is_capability_first_without_Contracts_bundle()
    {
        var productRoot = Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/Attributes/ProductValues");
        Assert.True(Directory.Exists(productRoot));
        foreach (var folder in new[] { "Commands", "Queries", "Models", "Ports", "Validators" })
        {
            Assert.True(Directory.Exists(Path.Combine(productRoot, folder)), folder);
        }

        Assert.Empty(Directory.GetDirectories(Path.Combine(productRoot, "Commands")));
        Assert.Empty(Directory.GetDirectories(Path.Combine(productRoot, "Queries")));
        Assert.Empty(Directory.GetFiles(productRoot, "*Contracts.cs", SearchOption.AllDirectories));

        var appRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application");
        Assert.Single(Directory.GetFiles(appRoot, "GetProductAttributeEditorStateQuery.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "GetProductAttributeReadinessQuery.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "SetProductAttributesCommand.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "SetProductAttributeCommand.cs", SearchOption.AllDirectories));
        Assert.True(File.Exists(Path.Combine(productRoot, "Ports", "IProductAttributeDirectory.cs")));
        Assert.True(Directory.Exists(Path.Combine(appRoot, "Attributes", "Definitions")));
        Assert.True(Directory.Exists(Path.Combine(appRoot, "Attributes", "Schema")));
    }

    [Fact]
    public void Attribute_ProductValues_path_namespace_exact_and_validators_classified()
    {
        var productRoot = Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/Attributes/ProductValues");
        var appRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application");
        foreach (var file in Directory.GetFiles(productRoot, "*.cs", SearchOption.AllDirectories))
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

        Assert.True(File.Exists(Path.Combine(productRoot, "Validators", "SetProductAttributesCommandValidator.cs")));
        Assert.False(File.Exists(Path.Combine(productRoot, "Validators", "GetProductAttributeEditorStateQueryValidator.cs")));
        Assert.False(File.Exists(Path.Combine(productRoot, "Validators", "GetProductAttributeReadinessQueryValidator.cs")));
        Assert.False(File.Exists(Path.Combine(productRoot, "Validators", "SetProductAttributeCommandValidator.cs")));
    }

    [Fact]
    public void ProductAttributeDirectory_uses_Result_not_exceptions_or_message_as_code()
    {
        var root = FindRepoRoot();
        var directory = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Directories/ProductAttributeDirectory.cs"));
        Assert.Contains("Result.Failure", directory, StringComparison.Ordinal);
        Assert.Contains("CatalogErrorCodes", directory, StringComparison.Ordinal);
        Assert.Contains("ProductMissing", directory, StringComparison.Ordinal);
        Assert.Contains("AttributeSchemaNotAllowed", directory, StringComparison.Ordinal);
        Assert.Contains("AttributeVariantAxisOnProductForbidden", directory, StringComparison.Ordinal);
        Assert.Contains("BeginTransactionAsync", directory, StringComparison.Ordinal);
        Assert.Contains("EventAttributesChanged", directory, StringComparison.Ordinal);
        Assert.Contains("CatalogAttributeCanonicalizer", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("Contains(\"کد\"", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Offer", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", directory, StringComparison.Ordinal);

        var endpointsCsproj = Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Tooba.Catalog.Endpoints.csproj");
        var endpointRefs = XDocument.Load(endpointsCsproj)
            .Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .ToArray();
        Assert.DoesNotContain(endpointRefs, r => r.Contains(".Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(endpointRefs, r => r.Contains("Tooba.Offer", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(endpointRefs, r => r.Contains("OperatorProfile.Contracts", StringComparison.OrdinalIgnoreCase));

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
        Assert.False(File.Exists(Path.Combine(admin, "CatalogAttributeEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "CatalogCategoryEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "CatalogFacetEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));

        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/Attributes/Definitions")));
        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/Attributes/Schema")));
        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/Attributes/ProductValues")));
    }

    [Fact]
    public void Error_catalog_owns_product_attribute_codes()
    {
        var root = FindRepoRoot();
        var codes = File.ReadAllText(Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Errors/CatalogErrorCodes.cs"));
        Assert.Contains("catalog.product.missing", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.attribute.definition.inactive", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.attribute.schema.not_allowed", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.attribute.variant_axis.on_product_forbidden", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.attribute.enum_option.required", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.attribute.validation.bounds", codes, StringComparison.Ordinal);

        var contributor = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Errors/CatalogErrorCatalogContributor.cs"));
        Assert.Contains("ProductMissing", contributor, StringComparison.Ordinal);
        Assert.Contains("AttributeSchemaNotAllowed", contributor, StringComparison.Ordinal);
        Assert.Contains("AttributeVariantAxisOnProductForbidden", contributor, StringComparison.Ordinal);

        var resx = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Resources/CatalogErrors.resx"));
        Assert.Contains("catalog.product.missing", resx, StringComparison.Ordinal);
        var fa = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Resources/CatalogErrors.fa.resx"));
        Assert.Contains("catalog.attribute.clear.required_forbidden", fa, StringComparison.Ordinal);
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
