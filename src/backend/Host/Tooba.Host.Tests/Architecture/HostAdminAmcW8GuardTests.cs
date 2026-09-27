using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-HOST-ADMIN-AMC-001-W8 — Attribute Definition Admin evacuated; Host file retained partial.</summary>
public sealed class HostAdminAmcW8GuardTests
{
    [Fact]
    public void Host_CatalogAttribute_file_retained_without_definition_routes()
    {
        var root = FindRepoRoot();
        var hostPath = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/CatalogAttributeEndpoints.cs");
        Assert.True(File.Exists(hostPath));
        var host = File.ReadAllText(hostPath);
        Assert.DoesNotContain("attribute-schema", host, StringComparison.Ordinal);
        Assert.DoesNotContain("ListDefinitionsAsync", host, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateDefinitionAsync", host, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateAttributeDefinitionRequest", host, StringComparison.Ordinal);
        Assert.DoesNotContain("/v1/admin/catalog/attribute-definitions", host, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/attributes\"", host, StringComparison.Ordinal);
        Assert.DoesNotContain("SetProductAttributesRequest", host, StringComparison.Ordinal);
        Assert.Contains("/variants/", host, StringComparison.Ordinal);
        Assert.Contains("variant-axes", host, StringComparison.Ordinal);
        Assert.DoesNotContain("MapAttributeInvalid", host, StringComparison.Ordinal);
        Assert.Contains("MapCatalogAttributeEndpoints", host, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("MapCatalogAttributeEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("MapCatalogModuleEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("MapStoreAppearanceSettingsEndpoints()", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_Endpoints_own_seven_definition_routes_exactly_once_with_canonical_pipeline()
    {
        var root = FindRepoRoot();
        var adminPath = Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/Attributes/Definitions/CatalogAttributeDefinitionAdminEndpoints.cs");
        Assert.True(File.Exists(adminPath));
        var admin = File.ReadAllText(adminPath);
        Assert.Contains("/v1/admin/catalog/attribute-definitions", admin, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/\", ListDefinitionsAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/{definitionId:guid}\", GetDefinitionAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/\", CreateDefinitionAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapPatch(\"/{definitionId:guid}\", UpdateDefinitionAsync)", admin, StringComparison.Ordinal);
        Assert.Contains(
            "variant-axis-capability/disable-preview",
            admin,
            StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/{definitionId:guid}/variant-axis-capability\"", admin, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{definitionId:guid}/options\", AddOptionAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("ISender", admin, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", admin, StringComparison.Ordinal);
        Assert.Contains("ICatalogAdminAuthorizer", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogDirectory", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("IAttributeDefinitionDirectory", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Infrastructure", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Offer", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Problem", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("تکراری", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("MapAttributeInvalid", admin, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/CatalogEndpointModule.cs"));
        Assert.Single(Regex.Matches(module, @"MapCatalogAttributeDefinitionAdminEndpoints\(\)"));
    }

    [Fact]
    public void Attribute_Definitions_Application_is_capability_first_without_Contracts_bundle()
    {
        var defsRoot = Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/Attributes/Definitions");
        Assert.True(Directory.Exists(defsRoot));
        foreach (var folder in new[] { "Commands", "Queries", "Models", "Ports", "Validators" })
        {
            Assert.True(Directory.Exists(Path.Combine(defsRoot, folder)), folder);
        }

        Assert.Empty(Directory.GetDirectories(Path.Combine(defsRoot, "Commands")));
        Assert.Empty(Directory.GetDirectories(Path.Combine(defsRoot, "Queries")));
        Assert.Empty(Directory.GetFiles(defsRoot, "*Contracts.cs", SearchOption.AllDirectories));

        var appRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application");
        Assert.Single(Directory.GetFiles(appRoot, "ListAttributeDefinitionsQuery.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "GetAttributeDefinitionQuery.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "CreateAttributeDefinitionCommand.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "UpdateAttributeDefinitionCommand.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "PreviewVariantAxisCapabilityDisableQuery.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "SetVariantAxisCapabilityCommand.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "AddAttributeOptionCommand.cs", SearchOption.AllDirectories));
        Assert.True(File.Exists(Path.Combine(defsRoot, "Ports", "IAttributeDefinitionDirectory.cs")));
        Assert.False(Directory.Exists(Path.Combine(appRoot, "Attributes", "Commands")));
    }

    [Fact]
    public void Attribute_Definitions_path_namespace_exact_and_validators_classified()
    {
        var defsRoot = Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/Attributes/Definitions");
        var appRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application");
        foreach (var file in Directory.GetFiles(defsRoot, "*.cs", SearchOption.AllDirectories))
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

        Assert.True(File.Exists(Path.Combine(defsRoot, "Validators", "CreateAttributeDefinitionCommandValidator.cs")));
        Assert.True(File.Exists(Path.Combine(defsRoot, "Validators", "AddAttributeOptionCommandValidator.cs")));
        Assert.False(File.Exists(Path.Combine(defsRoot, "Validators", "ListAttributeDefinitionsQueryValidator.cs")));
        Assert.False(File.Exists(Path.Combine(defsRoot, "Validators", "GetAttributeDefinitionQueryValidator.cs")));
        Assert.False(File.Exists(Path.Combine(defsRoot, "Validators", "UpdateAttributeDefinitionCommandValidator.cs")));
        Assert.False(File.Exists(Path.Combine(defsRoot, "Validators", "PreviewVariantAxisCapabilityDisableQueryValidator.cs")));
        Assert.False(File.Exists(Path.Combine(defsRoot, "Validators", "SetVariantAxisCapabilityCommandValidator.cs")));
    }

    [Fact]
    public void AttributeDefinitionDirectory_uses_Result_not_exceptions_or_message_as_code()
    {
        var root = FindRepoRoot();
        var directory = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/AttributeDefinitionDirectory.cs"));
        Assert.Contains("Result.Failure", directory, StringComparison.Ordinal);
        Assert.Contains("CatalogErrorCodes", directory, StringComparison.Ordinal);
        Assert.Contains("AttributeCodeDuplicate", directory, StringComparison.Ordinal);
        Assert.Contains("AttributeNameDuplicate", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("Contains(\"کد\"", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("تکراری", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Offer", directory, StringComparison.Ordinal);

        var endpointsCsproj = Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Tooba.Catalog.Endpoints.csproj");
        var endpointRefs = XDocument.Load(endpointsCsproj)
            .Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .ToArray();
        Assert.DoesNotContain(endpointRefs, r => r.Contains(".Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(endpointRefs, r => r.Contains("Tooba.Offer", StringComparison.OrdinalIgnoreCase));

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
        Assert.Equal(53, files.Length);
        Assert.True(File.Exists(Path.Combine(admin, "CatalogAttributeEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "CatalogCategoryEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "CatalogFacetEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "CatalogMegaMenuEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "CatalogTagEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "UnitOfMeasureEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "QuantitySettingsEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsComposer.cs")));

        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/Tags")));
        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/MegaMenu")));
        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/Facets")));
        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/Categories")));
        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/Attributes/Definitions")));
        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/Attributes/Schema")));
    }

    [Fact]
    public void Error_catalog_owns_attribute_definition_codes()
    {
        var root = FindRepoRoot();
        var codes = File.ReadAllText(Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Errors/CatalogErrorCodes.cs"));
        Assert.Contains("catalog.attribute.missing", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.attribute.invalid", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.attribute.code.duplicate", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.attribute.name.duplicate", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.attribute.variant_axis.value_kind.invalid", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.attribute.variant_axis.in_use", codes, StringComparison.Ordinal);

        var contributor = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Errors/CatalogErrorCatalogContributor.cs"));
        Assert.Contains("AttributeMissing", contributor, StringComparison.Ordinal);
        Assert.Contains("AttributeCodeDuplicate", contributor, StringComparison.Ordinal);
        Assert.Contains("AttributeNameDuplicate", contributor, StringComparison.Ordinal);

        var resx = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Resources/CatalogErrors.resx"));
        Assert.Contains("catalog.attribute.missing", resx, StringComparison.Ordinal);
        Assert.Contains("catalog.attribute.code.duplicate", resx, StringComparison.Ordinal);
        var fa = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Resources/CatalogErrors.fa.resx"));
        Assert.Contains("catalog.attribute.name.duplicate", fa, StringComparison.Ordinal);
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
