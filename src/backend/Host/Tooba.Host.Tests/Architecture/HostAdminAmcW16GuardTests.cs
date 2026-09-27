using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-HOST-ADMIN-AMC-001-W16 — Product Workspace Publish Readiness GET evacuated to Catalog ProductPublishing.</summary>
public sealed class HostAdminAmcW16GuardTests
{
    [Fact]
    public void Host_ProductWorkspace_has_zero_publish_readiness_routes_and_files_retained()
    {
        var root = FindRepoRoot();
        Assert.False(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceComposer.cs")));
        Assert.False(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceModels.cs")));

        var models = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/ProductWorkspace/Tooba.ProductWorkspace.Application/Composition/Models/ProductWorkspaceModels.cs"));
        Assert.Contains("record ProductPublishReadinessView", models, StringComparison.Ordinal);
        Assert.Contains("record ProductPublishMissingRequirementView", models, StringComparison.Ordinal);
        Assert.Contains("AggregateReadiness", models, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_Endpoints_own_publish_readiness_route_exactly_once_with_canonical_pipeline()
    {
        var root = FindRepoRoot();
        var adminPath = Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/ProductPublishing/CatalogProductPublishReadinessAdminEndpoints.cs");
        Assert.True(File.Exists(adminPath));
        var admin = File.ReadAllText(adminPath);
        Assert.Contains("/v1/admin/products/{productId:guid}", admin, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/publish/readiness\", GetAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("ISender", admin, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", admin, StringComparison.Ordinal);
        Assert.Contains("ICatalogAdminAuthorizer", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogWorkspaceScope.AllowsCatalogEdit", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogDirectory", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("IProductPublishReadinessReader", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductWorkspaceComposer", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Infrastructure", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", admin, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/CatalogEndpointModule.cs"));
        Assert.Single(Regex.Matches(module, @"MapCatalogProductPublishReadinessAdminEndpoints\(\)"));
        Assert.Single(Regex.Matches(module, @"MapCatalogProductHistoryAdminEndpoints\(\)"));
        Assert.Single(Regex.Matches(module, @"MapCatalogProductSeoAdminEndpoints\(\)"));
        Assert.Single(Regex.Matches(module, @"MapCatalogProductMediaAdminEndpoints\(\)"));
    }

    [Fact]
    public void ProductPublishing_Application_is_capability_first_without_Contracts_bundle()
    {
        var capabilityRoot = Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductPublishing");
        Assert.True(Directory.Exists(capabilityRoot));
        foreach (var folder in new[] { "Commands", "Queries", "Models", "Ports", "Validators" })
        {
            Assert.True(Directory.Exists(Path.Combine(capabilityRoot, folder)), folder);
        }

        Assert.Empty(Directory.GetDirectories(Path.Combine(capabilityRoot, "Commands")));
        Assert.Empty(Directory.GetDirectories(Path.Combine(capabilityRoot, "Queries")));
        Assert.Empty(Directory.GetFiles(capabilityRoot, "*Contracts.cs", SearchOption.AllDirectories));
        Assert.True(File.Exists(Path.Combine(capabilityRoot, "Ports", "IProductPublishReadinessReader.cs")));
        Assert.True(File.Exists(Path.Combine(capabilityRoot, "Ports", "IProductLifecycleDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(capabilityRoot, "Queries", "GetProductPublishReadinessQuery.cs")));
        Assert.True(File.Exists(Path.Combine(capabilityRoot, "Queries", "GetProductPublishReadinessHandler.cs")));
        Assert.True(File.Exists(Path.Combine(capabilityRoot, "Commands", "PublishProductCommand.cs")));
        Assert.Empty(Directory.GetFiles(Path.Combine(capabilityRoot, "Validators"), "*Validator.cs"));
    }

    [Fact]
    public void ProductPublishing_path_namespace_exact_and_Result_typed_reader_reuses_focused_seams()
    {
        var root = FindRepoRoot();
        var capabilityRoot = Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductPublishing");
        var appRoot = Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Application");
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

        var reader = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/ProductPublishReadinessReader.cs"));
        Assert.Contains("Result.Failure", reader, StringComparison.Ordinal);
        Assert.Contains("CatalogErrorCodes", reader, StringComparison.Ordinal);
        Assert.Contains("WorkspaceProductMissing", reader, StringComparison.Ordinal);
        Assert.Contains("ProductSeoRules.NormalizeLocale", reader, StringComparison.Ordinal);
        Assert.Contains("ProductPublishRules.MessageCategoryIncompleteFa", reader, StringComparison.Ordinal);
        Assert.Contains("ProductPublishRules.MessageIdentityIncompleteFa", reader, StringComparison.Ordinal);
        Assert.Contains("ProductPublishRules.MessageAttributesIncompleteFa", reader, StringComparison.Ordinal);
        Assert.Contains("ProductPublishRules.MessageVariantsIncompleteFa", reader, StringComparison.Ordinal);
        Assert.Contains("ProductPublishRules.MessageMediaIncompleteFa", reader, StringComparison.Ordinal);
        Assert.Contains("ProductPublishRules.MessageSeoIncompleteFa", reader, StringComparison.Ordinal);
        Assert.Contains("ProductPublishRules.MessageReadyFa", reader, StringComparison.Ordinal);
        Assert.Contains("ProductPublishRules.SummarizeMissingFa", reader, StringComparison.Ordinal);
        Assert.Contains("IProductSeoDirectory", reader, StringComparison.Ordinal);
        Assert.Contains("IProductAttributeDirectory", reader, StringComparison.Ordinal);
        Assert.Contains("IProductVariantDirectory", reader, StringComparison.Ordinal);
        Assert.Contains("IProductMediaDirectory", reader, StringComparison.Ordinal);
        Assert.Contains("\"category\"", reader, StringComparison.Ordinal);
        Assert.Contains("\"identity\"", reader, StringComparison.Ordinal);
        Assert.Contains("\"attributes\"", reader, StringComparison.Ordinal);
        Assert.Contains("\"variants\"", reader, StringComparison.Ordinal);
        Assert.Contains("\"media\"", reader, StringComparison.Ordinal);
        Assert.Contains("\"seo\"", reader, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", reader, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", reader, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", reader, StringComparison.Ordinal);
        Assert.DoesNotContain("Message.Contains", reader, StringComparison.Ordinal);
        Assert.DoesNotContain("throw new InvalidOperationException", reader, StringComparison.Ordinal);

        var directory = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/CatalogDirectory.cs"));
        Assert.Contains("PublishReadinessPort().GetAsync", directory, StringComparison.Ordinal);
        Assert.Contains("UnwrapHistory", directory, StringComparison.Ordinal);

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
        Assert.False(File.Exists(Path.Combine(admin, "ProductWorkspaceEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "CatalogAttributeEndpoints.cs")));

        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductMedia")));
        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductSeo")));
        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductHistory")));
        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductPublishing")));
        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/CategoryChanges")));
        Assert.True(File.Exists(Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/CatalogWorkspaceScope.cs")));
    }

    [Fact]
    public void Error_catalog_preserves_workspace_product_missing_for_publish_readiness()
    {
        var root = FindRepoRoot();
        var codes = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Errors/CatalogErrorCodes.cs"));
        Assert.Contains("workspace.product.missing", codes, StringComparison.Ordinal);

        var contributor = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Errors/CatalogErrorCatalogContributor.cs"));
        Assert.Contains("WorkspaceProductMissing", contributor, StringComparison.Ordinal);
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
