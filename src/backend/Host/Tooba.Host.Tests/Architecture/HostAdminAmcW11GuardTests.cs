using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-HOST-ADMIN-AMC-001-W11 — Variant Axes + Matrix evacuated; Host Attribute file retained category-change only.</summary>
public sealed class HostAdminAmcW11GuardTests
{
    [Fact]
    public void Host_CatalogAttribute_file_retained_category_change_only_without_variant_routes()
    {
        var root = FindRepoRoot();
        var hostPath = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/CatalogAttributeEndpoints.cs");
        Assert.True(File.Exists(hostPath));
        var host = File.ReadAllText(hostPath);
        Assert.DoesNotContain("/variant-axes", host, StringComparison.Ordinal);
        Assert.DoesNotContain("/variants/", host, StringComparison.Ordinal);
        Assert.DoesNotContain("SetProductVariantAxesAsync", host, StringComparison.Ordinal);
        Assert.DoesNotContain("GetProductVariantEditorStateAsync", host, StringComparison.Ordinal);
        Assert.DoesNotContain("PreviewProductVariantsAsync", host, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplyProductVariantsAsync", host, StringComparison.Ordinal);
        Assert.DoesNotContain("GetProductVariantReadinessAsync", host, StringComparison.Ordinal);
        Assert.DoesNotContain("EnrichVariantEditorWithOfferCountsAsync", host, StringComparison.Ordinal);
        Assert.DoesNotContain("SetProductVariantAxesRequest", host, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductVariantPreviewRequest", host, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductVariantApplyRequest", host, StringComparison.Ordinal);
        Assert.DoesNotContain("IOfferLookupGateway", host, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Offer", host, StringComparison.Ordinal);
        Assert.Contains("category-change-preview", host, StringComparison.Ordinal);
        Assert.Contains("primary-category", host, StringComparison.Ordinal);
        Assert.Contains("CategoryChangeRequest", host, StringComparison.Ordinal);
        Assert.Contains("MapCategoryChangeInvalid", host, StringComparison.Ordinal);
        Assert.Contains("CatalogActorHttpBinding", host, StringComparison.Ordinal);
        Assert.Contains("MapCatalogAttributeEndpoints", host, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("MapCatalogAttributeEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("MapCatalogModuleEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("MapStoreAppearanceSettingsEndpoints()", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_Endpoints_own_five_variant_routes_exactly_once_with_canonical_pipeline()
    {
        var root = FindRepoRoot();
        var adminPath = Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/Variants/CatalogProductVariantAdminEndpoints.cs");
        Assert.True(File.Exists(adminPath));
        var admin = File.ReadAllText(adminPath);
        Assert.Contains("/v1/admin/catalog/products/{productId:guid}", admin, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/variant-axes\", SetAxesAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/variants/editor\", GetEditorStateAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/variants/preview\", PreviewAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/variants/apply\", ApplyAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/variants/readiness\", GetReadinessAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("ISender", admin, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", admin, StringComparison.Ordinal);
        Assert.Contains("ICatalogAdminAuthorizer", admin, StringComparison.Ordinal);
        Assert.Contains("CatalogActorRequestBinding", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogDirectory", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("IProductVariantDirectory", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("IOfferLookupGateway", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Infrastructure", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Offer", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Problem", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", admin, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/CatalogEndpointModule.cs"));
        Assert.Single(Regex.Matches(module, @"MapCatalogProductVariantAdminEndpoints\(\)"));
        Assert.Single(Regex.Matches(module, @"MapCatalogProductAttributeAdminEndpoints\(\)"));
    }

    [Fact]
    public void Variants_Application_is_capability_first_without_Contracts_bundle()
    {
        var variantRoot = Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/Variants");
        Assert.True(Directory.Exists(variantRoot));
        foreach (var folder in new[] { "Commands", "Queries", "Models", "Ports", "Validators" })
        {
            Assert.True(Directory.Exists(Path.Combine(variantRoot, folder)), folder);
        }

        Assert.Empty(Directory.GetDirectories(Path.Combine(variantRoot, "Commands")));
        Assert.Empty(Directory.GetDirectories(Path.Combine(variantRoot, "Queries")));
        Assert.Empty(Directory.GetFiles(variantRoot, "*Contracts.cs", SearchOption.AllDirectories));

        var appRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application");
        Assert.Single(Directory.GetFiles(appRoot, "SetProductVariantAxesCommand.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "ApplyProductVariantMatrixCommand.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "GetProductVariantEditorStateQuery.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "PreviewProductVariantsQuery.cs", SearchOption.AllDirectories));
        Assert.Single(Directory.GetFiles(appRoot, "GetProductVariantReadinessQuery.cs", SearchOption.AllDirectories));
        Assert.True(File.Exists(Path.Combine(variantRoot, "Ports", "IProductVariantDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(variantRoot, "Ports", "IVariantOfferLookup.cs")));
    }

    [Fact]
    public void Variants_path_namespace_exact_and_validators_classified()
    {
        var variantRoot = Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/Variants");
        var appRoot = Path.Combine(FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application");
        foreach (var file in Directory.GetFiles(variantRoot, "*.cs", SearchOption.AllDirectories))
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

        Assert.True(File.Exists(Path.Combine(variantRoot, "Validators", "SetProductVariantAxesCommandValidator.cs")));
        Assert.True(File.Exists(Path.Combine(variantRoot, "Validators", "PreviewProductVariantsQueryValidator.cs")));
        Assert.True(File.Exists(Path.Combine(variantRoot, "Validators", "ApplyProductVariantMatrixCommandValidator.cs")));
        Assert.False(File.Exists(Path.Combine(variantRoot, "Validators", "GetProductVariantEditorStateQueryValidator.cs")));
        Assert.False(File.Exists(Path.Combine(variantRoot, "Validators", "GetProductVariantReadinessQueryValidator.cs")));
    }

    [Fact]
    public void ProductVariantDirectory_and_Offer_adapter_are_Result_and_Contracts_only()
    {
        var root = FindRepoRoot();
        var directory = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/ProductVariantDirectory.cs"));
        Assert.Contains("Result.Failure", directory, StringComparison.Ordinal);
        Assert.Contains("CatalogErrorCodes", directory, StringComparison.Ordinal);
        Assert.Contains("ProductMissing", directory, StringComparison.Ordinal);
        Assert.Contains("VariantAxesDuplicate", directory, StringComparison.Ordinal);
        Assert.Contains("BeginTransactionAsync", directory, StringComparison.Ordinal);
        Assert.Contains("EventVariantsChanged", directory, StringComparison.Ordinal);
        Assert.Contains("MaxVariantCombinations = 200", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Offer", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", directory, StringComparison.Ordinal);

        var adapter = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Adapters/VariantOfferLookupAdapter.cs"));
        Assert.Contains("IOfferLookupGateway", adapter, StringComparison.Ordinal);
        Assert.Contains("Tooba.Offer.Contracts", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("Offer.Application", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("Offer.Infrastructure", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("Offer.Domain", adapter, StringComparison.Ordinal);

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
            Assert.DoesNotContain(projectRefs, r => r.Contains("Offer.Application", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(projectRefs, r => r.Contains("Offer.Infrastructure", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(projectRefs, r => r.Contains("Offer.Domain", StringComparison.OrdinalIgnoreCase));
        }

        var infraCsproj = Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Tooba.Catalog.Infrastructure.csproj");
        var infraRefs = XDocument.Load(infraCsproj)
            .Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .ToArray();
        Assert.Contains(infraRefs, r => r.Contains("Offer.Contracts", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Host_Admin_file_count_is_53_and_prior_waves_preserved()
    {
        var admin = Path.Combine(FindRepoRoot(), "src/backend/Host/Tooba.Host/Admin");
        var files = Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories);
        Assert.Equal(53, files.Length);
        Assert.True(File.Exists(Path.Combine(admin, "CatalogAttributeEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "CatalogCategoryEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));

        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/Attributes/ProductValues")));
        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/Variants")));
    }

    [Fact]
    public void Error_catalog_owns_variant_codes()
    {
        var root = FindRepoRoot();
        var codes = File.ReadAllText(Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Errors/CatalogErrorCodes.cs"));
        Assert.Contains("catalog.variant.axes.duplicate", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.variant.axis.schema_not_enabled", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.variant.combination.limit_exceeded", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.variant.patch.status_invalid", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.variant.default.archived_forbidden", codes, StringComparison.Ordinal);

        var contributor = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Errors/CatalogErrorCatalogContributor.cs"));
        Assert.Contains("VariantAxesDuplicate", contributor, StringComparison.Ordinal);
        Assert.Contains("VariantCombinationLimitExceeded", contributor, StringComparison.Ordinal);

        var resx = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Resources/CatalogErrors.resx"));
        Assert.Contains("catalog.variant.axes.duplicate", resx, StringComparison.Ordinal);
        var resxFa = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Resources/CatalogErrors.fa.resx"));
        Assert.Contains("catalog.variant.default.archived_forbidden", resxFa, StringComparison.Ordinal);
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
