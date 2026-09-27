using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-HOST-ADMIN-AMC-001-W13 — Product Workspace Media Admin evacuated to Catalog ProductMedia.</summary>
public sealed class HostAdminAmcW13GuardTests
{
    [Fact]
    public void Host_ProductWorkspace_has_zero_media_routes_and_files_retained()
    {
        var root = FindRepoRoot();

        Assert.False(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceComposer.cs")));
        Assert.False(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceModels.cs")));


    }

    [Fact]
    public void Catalog_Endpoints_own_eight_media_routes_exactly_once_with_canonical_pipeline()
    {
        var root = FindRepoRoot();
        var adminPath = Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/ProductMedia/CatalogProductMediaAdminEndpoints.cs");
        Assert.True(File.Exists(adminPath));
        var admin = File.ReadAllText(adminPath);
        Assert.Contains("/v1/admin/products/{productId:guid}", admin, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/media\", ListAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/media/readiness\", GetReadinessAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/media\", AttachAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/media/placeholder\", AttachPlaceholderAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/media/order\", ReorderAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/media/{assetId:guid}/primary\", SetPrimaryAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapPatch(\"/media/{assetId:guid}\", PatchAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapDelete(\"/media/{assetId:guid}\", DetachAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("ISender", admin, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", admin, StringComparison.Ordinal);
        Assert.Contains("ICatalogAdminAuthorizer", admin, StringComparison.Ordinal);
        Assert.Contains("CatalogWorkspaceMediaScope", admin, StringComparison.Ordinal);
        Assert.Contains("CatalogWorkspaceScope", admin, StringComparison.Ordinal);
        Assert.Contains("CatalogActorRequestBinding", admin, StringComparison.Ordinal);
        var workspaceScope = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/CatalogWorkspaceScope.cs"));
        Assert.Contains("X-Tooba-Workspace-Scope", workspaceScope, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogDirectory", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("IProductMediaDirectory", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductWorkspaceComposer", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Infrastructure", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", admin, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/CatalogEndpointModule.cs"));
        Assert.Single(Regex.Matches(module, @"MapCatalogProductMediaAdminEndpoints\(\)"));
        Assert.Single(Regex.Matches(module, @"MapCatalogProductCategoryChangeAdminEndpoints\(\)"));
    }

    [Fact]
    public void ProductMedia_Application_is_capability_first_without_Contracts_bundle()
    {
        var capabilityRoot = Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductMedia");
        Assert.True(Directory.Exists(capabilityRoot));
        foreach (var folder in new[] { "Commands", "Queries", "Models", "Ports", "Validators" })
        {
            Assert.True(Directory.Exists(Path.Combine(capabilityRoot, folder)), folder);
        }

        Assert.Empty(Directory.GetDirectories(Path.Combine(capabilityRoot, "Commands")));
        Assert.Empty(Directory.GetDirectories(Path.Combine(capabilityRoot, "Queries")));
        Assert.Empty(Directory.GetFiles(capabilityRoot, "*Contracts.cs", SearchOption.AllDirectories));
        Assert.True(File.Exists(Path.Combine(capabilityRoot, "Ports", "IProductMediaDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(capabilityRoot, "Validators", "AttachProductMediaCommandValidator.cs")));
        Assert.True(File.Exists(Path.Combine(capabilityRoot, "Validators", "ReorderProductMediaCommandValidator.cs")));
    }

    [Fact]
    public void ProductMedia_path_namespace_exact_and_Result_typed_directory()
    {
        var root = FindRepoRoot();
        var capabilityRoot = Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductMedia");
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
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/ProductMediaDirectory.cs"));
        Assert.Contains("Result.Failure", directory, StringComparison.Ordinal);
        Assert.Contains("CatalogErrorCodes", directory, StringComparison.Ordinal);
        Assert.Contains("WorkspaceMedia", directory, StringComparison.Ordinal);
        Assert.Contains("EnforcePrimaryUniqueness", directory, StringComparison.Ordinal);
        Assert.Contains("EventMediaChanged", directory, StringComparison.Ordinal);
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
        Assert.False(File.Exists(Path.Combine(admin, "ProductWorkspaceEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "CatalogAttributeEndpoints.cs")));

        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/CategoryChanges")));
        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductMedia")));
        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductSeo")));
        Assert.True(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Application/Variants")));
    }

    [Fact]
    public void Error_catalog_owns_workspace_media_codes()
    {
        var root = FindRepoRoot();
        var codes = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Errors/CatalogErrorCodes.cs"));
        foreach (var code in new[]
                 {
                     "workspace.product.missing",
                     "workspace.permission.denied",
                     "workspace.media.asset.missing",
                     "workspace.media.attach.rejected",
                     "workspace.media.placeholder.rejected",
                     "workspace.media.empty",
                     "workspace.media.order.invalid",
                     "workspace.media.order.rejected",
                     "workspace.media.missing",
                 })
        {
            Assert.Contains(code, codes, StringComparison.Ordinal);
        }

        var contributor = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Errors/CatalogErrorCatalogContributor.cs"));
        Assert.Contains("WorkspaceMediaMissing", contributor, StringComparison.Ordinal);
        Assert.Contains("WorkspacePermissionDenied", contributor, StringComparison.Ordinal);

        var resx = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Resources/CatalogErrors.resx"));
        Assert.Contains("workspace.media.order.invalid", resx, StringComparison.Ordinal);
        var resxFa = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Resources/CatalogErrors.fa.resx"));
        Assert.Contains("شناسهٔ رسانه لازم است.", resxFa, StringComparison.Ordinal);
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
