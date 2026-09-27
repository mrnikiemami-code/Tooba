using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-AMC-001-W31 — ProductWorkspace list + grid to module via Catalog list Contracts.
/// </summary>
public sealed class HostAdminAmcW31PwListGridGuardTests
{
    private static readonly Regex MapRouteRegex = new(
        @"\bMap(Get|Post|Put|Patch|Delete)\s*\(",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void List_grid_owned_by_ProductWorkspace_and_absent_from_Host()
    {
        var root = FindRepoRoot();


        Assert.False(File.Exists(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Grid/AdminProductGridQueryEngine.cs")));
        Assert.False(File.Exists(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Grid/AdminProductGridQueryPolicy.cs")));

        var module = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/ProductWorkspace/Tooba.ProductWorkspace.Endpoints/ProductWorkspaceEndpointModule.cs"));
        Assert.Equal(17, MapRouteRegex.Matches(module).Count);
        Assert.Contains("MapGet(\"/\", ListProductsAsync)", module, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/query\", QueryProductGridAsync)", module, StringComparison.Ordinal);
        Assert.Contains("ListProductWorkspaceQuery", module, StringComparison.Ordinal);
        Assert.Contains("QueryProductWorkspaceGridQuery", module, StringComparison.Ordinal);
        Assert.Contains("IProductWorkspaceAdminAuthorizer", module, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", module, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", module, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", module, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", module, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/ProductWorkspace/Tooba.ProductWorkspace.Application/Composition/Queries/ListProductWorkspaceQuery.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/ProductWorkspace/Tooba.ProductWorkspace.Application/Composition/Queries/QueryProductWorkspaceGridQuery.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/ProductWorkspace/Tooba.ProductWorkspace.Application/Composition/Grid/AdminProductGridQueryPolicy.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/CatalogAdminProductWorkspaceListContracts.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/CatalogAdminProductWorkspaceListGateway.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Grid/AdminProductGridQueryEngine.cs")));

        var application = Path.Combine(root, "src/backend/Modules/ProductWorkspace/Tooba.ProductWorkspace.Application");
        foreach (var file in Directory.GetFiles(application, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("CatalogDbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("PlatformHttpException", text, StringComparison.Ordinal);
        }

        Assert.False(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceComposer.cs")));
        Assert.False(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceModels.cs")));
    }

    [Fact]
    public void Host_Admin_count_23_StoreAppearance_deferred_PW_shells_ABSENT()
    {
        var root = FindRepoRoot();
        var admin = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin");
        Assert.Equal(23, Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories).Length);
        Assert.True(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "ProductWorkspaceEndpoints.cs")));
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
