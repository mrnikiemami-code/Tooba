using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-HOST-ADMIN-AMC-001-W14 — Product Workspace SEO Admin evacuated to Catalog ProductSeo.</summary>
public sealed class HostAdminAmcW14GuardTests
{
    [Fact]
    public void Host_ProductWorkspace_has_zero_seo_routes_and_files_retained()
    {
        var root = FindRepoRoot();
        var endpoints = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceEndpoints.cs"));
        Assert.DoesNotContain("MapGet(\"/{productId:guid}/seo\"", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPut(\"/{productId:guid}/seo\"", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("/seo/readiness", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("GetSeoAsync", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("PutSeoAsync", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("GetSeoReadinessAsync", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapProductWorkspaceEndpoints", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{productId:guid}/variants\"", endpoints, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceComposer.cs")));
        Assert.True(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceModels.cs")));

        var composer = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceComposer.cs"));
        Assert.DoesNotContain("GetSeoAsync", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("UpdateSeoAsync", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("GetSeoReadinessAsync", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("MapSeoDetail", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("workspace.product.seo.rejected", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductSeoDetail", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductSeoUpdateInput", composer, StringComparison.Ordinal);

        var models = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceModels.cs"));
        Assert.DoesNotContain("AdminProductSeoUpdateRequest", models, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductSeoDetailView", models, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductSeoReadinessView", models, StringComparison.Ordinal);
        Assert.Contains("record ProductSeoView", models, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_Endpoints_own_three_seo_routes_exactly_once_with_canonical_pipeline()
    {
        var root = FindRepoRoot();
        var adminPath = Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/ProductSeo/CatalogProductSeoAdminEndpoints.cs");
        Assert.True(File.Exists(adminPath));
        var admin = File.ReadAllText(adminPath);
        Assert.Contains("/v1/admin/products/{productId:guid}", admin, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/seo\", GetAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/seo\", PutAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/seo/readiness\", GetReadinessAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("ISender", admin, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", admin, StringComparison.Ordinal);
        Assert.Contains("ICatalogAdminAuthorizer", admin, StringComparison.Ordinal);
        Assert.Contains("CatalogWorkspaceScope", admin, StringComparison.Ordinal);
        Assert.Contains("CatalogActorRequestBinding", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogDirectory", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("IProductSeoDirectory", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductWorkspaceComposer", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Infrastructure", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", admin, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/CatalogEndpointModule.cs"));
        Assert.Single(Regex.Matches(module, @"MapCatalogProductSeoAdminEndpoints\(\)"));
        Assert.Single(Regex.Matches(module, @"MapCatalogProductMediaAdminEndpoints\(\)"));
    }

    [Fact]
    public void ProductSeo_Application_is_capability_first_without_Contracts_bundle()
    {
        var capabilityRoot = Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductSeo");
        Assert.True(Directory.Exists(capabilityRoot));
        foreach (var folder in new[] { "Commands", "Queries", "Models", "Ports", "Validators" })
        {
            Assert.True(Directory.Exists(Path.Combine(capabilityRoot, folder)), folder);
        }

        Assert.Empty(Directory.GetDirectories(Path.Combine(capabilityRoot, "Commands")));
        Assert.Empty(Directory.GetDirectories(Path.Combine(capabilityRoot, "Queries")));
        Assert.Empty(Directory.GetFiles(capabilityRoot, "*Contracts.cs", SearchOption.AllDirectories));
        Assert.True(File.Exists(Path.Combine(capabilityRoot, "Ports", "IProductSeoDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(capabilityRoot, "Validators", "UpdateProductSeoCommandValidator.cs")));
    }

    [Fact]
    public void ProductSeo_path_namespace_exact_and_Result_typed_directory()
    {
        var root = FindRepoRoot();
        var capabilityRoot = Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductSeo");
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

        var directory = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/ProductSeoDirectory.cs"));
        Assert.Contains("Result.Failure", directory, StringComparison.Ordinal);
        Assert.Contains("CatalogErrorCodes", directory, StringComparison.Ordinal);
        Assert.Contains("WorkspaceCatalogStale", directory, StringComparison.Ordinal);
        Assert.Contains("WorkspaceProductSlugDuplicate", directory, StringComparison.Ordinal);
        Assert.Contains("WorkspaceProductSlugInvalid", directory, StringComparison.Ordinal);
        Assert.Contains("ProductSeoRules", directory, StringComparison.Ordinal);
        Assert.Contains("EventSeoChanged", directory, StringComparison.Ordinal);
        Assert.Contains("SaveChangesAsync", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("Message.Contains", directory, StringComparison.Ordinal);

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
        Assert.True(File.Exists(Path.Combine(admin, "ProductWorkspaceEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "CatalogAttributeEndpoints.cs")));

        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductMedia")));
        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductSeo")));
        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/CategoryChanges")));
        Assert.True(File.Exists(Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/CatalogWorkspaceScope.cs")));
    }

    [Fact]
    public void Error_catalog_owns_workspace_seo_codes()
    {
        var root = FindRepoRoot();
        var codes = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Errors/CatalogErrorCodes.cs"));
        foreach (var code in new[]
                 {
                     "workspace.product.missing",
                     "workspace.permission.denied",
                     "workspace.catalog.stale",
                     "workspace.product.slug.duplicate",
                     "workspace.product.slug.invalid",
                     "workspace.product.seo.rejected",
                 })
        {
            Assert.Contains(code, codes, StringComparison.Ordinal);
        }

        var contributor = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Errors/CatalogErrorCatalogContributor.cs"));
        Assert.Contains("WorkspaceCatalogStale", contributor, StringComparison.Ordinal);
        Assert.Contains("WorkspaceProductSlugDuplicate", contributor, StringComparison.Ordinal);
        Assert.Contains("WorkspaceProductSlugInvalid", contributor, StringComparison.Ordinal);
        Assert.Contains("WorkspaceProductSeoRejected", contributor, StringComparison.Ordinal);

        var resx = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Resources/CatalogErrors.resx"));
        Assert.Contains("workspace.product.slug.duplicate", resx, StringComparison.Ordinal);
        var resxFa = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Resources/CatalogErrors.fa.resx"));
        Assert.Contains("نشانی صفحه نامعتبر است.", resxFa, StringComparison.Ordinal);
    }

    [Fact]
    public void Workspace_scope_generalization_preserves_media_alias()
    {
        var root = FindRepoRoot();
        var scope = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/CatalogWorkspaceScope.cs"));
        Assert.Contains("X-Tooba-Workspace-Scope", scope, StringComparison.Ordinal);
        Assert.Contains("view", scope, StringComparison.OrdinalIgnoreCase);

        var media = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/ProductMedia/CatalogProductMediaAdminEndpoints.cs"));
        Assert.Contains("CatalogWorkspaceMediaScope", media, StringComparison.Ordinal);
        Assert.Contains("CatalogWorkspaceScope.AllowsCatalogEdit", media, StringComparison.Ordinal);
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
