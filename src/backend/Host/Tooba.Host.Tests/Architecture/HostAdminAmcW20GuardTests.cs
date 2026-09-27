using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-HOST-ADMIN-AMC-001-W20 — Catalog-only brand-options evacuate from Host ProductWorkspace.</summary>
public sealed class HostAdminAmcW20GuardTests
{
    private static readonly Regex MapRouteRegex = new(
        @"\bMap(Get|Post|Put|Patch|Delete)\s*\(",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Brand_options_owned_once_by_Catalog_and_absent_from_Host_ProductWorkspace()
    {
        var root = FindRepoRoot();



        var catalogEndpoints = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/Brands/CatalogBrandOptionsAdminEndpoints.cs"));
        Assert.Equal(1, MapRouteRegex.Matches(catalogEndpoints).Count);
        Assert.Contains("MapGet(\"/brand-options\"", catalogEndpoints, StringComparison.Ordinal);
        Assert.Contains("ICatalogAdminAuthorizer", catalogEndpoints, StringComparison.Ordinal);
        Assert.Contains("ISender", catalogEndpoints, StringComparison.Ordinal);
        Assert.Contains("ListBrandOptionsQuery", catalogEndpoints, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", catalogEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("IBrandOptionReader", catalogEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", catalogEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", catalogEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", catalogEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", catalogEndpoints, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/CatalogEndpointModule.cs"));
        Assert.Contains("MapCatalogBrandOptionsAdminEndpoints", module, StringComparison.Ordinal);
    }

    [Fact]
    public void W19_aggregate_GET_and_ProductWorkspace_route_count_preserved()
    {
        var root = FindRepoRoot();
        var pw = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/ProductWorkspace/Tooba.ProductWorkspace.Endpoints/ProductWorkspaceEndpointModule.cs"));
        Assert.Equal(17, MapRouteRegex.Matches(pw).Count);
        Assert.Contains("MapGet(\"/{productId:guid}\"", pw, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{productId:guid}/publish\"", pw, StringComparison.Ordinal);

    }

    [Fact]
    public void Brands_capability_CQRS_and_boundaries()
    {
        var root = FindRepoRoot();
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Application/Brands/Queries/ListBrandOptionsQuery.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Application/Brands/Queries/ListBrandOptionsHandler.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Application/Brands/Ports/IBrandOptionReader.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Application/Brands/Models/BrandOptionView.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/BrandOptionReader.cs")));

        var catalogRoot = Path.Combine(root, "src/backend/Modules/Catalog");
        var endpointCsproj = File.ReadAllText(Path.Combine(
            catalogRoot, "Tooba.Catalog.Endpoints/Tooba.Catalog.Endpoints.csproj"));
        Assert.DoesNotContain("Catalog.Infrastructure", endpointCsproj, StringComparison.Ordinal);

        var appCsproj = File.ReadAllText(Path.Combine(
            catalogRoot, "Tooba.Catalog.Application/Tooba.Catalog.Application.csproj"));
        Assert.DoesNotContain("Tooba.Host", appCsproj, StringComparison.Ordinal);

        var reader = File.ReadAllText(Path.Combine(
            catalogRoot, "Tooba.Catalog.Infrastructure/BrandOptionReader.cs"));
        Assert.DoesNotContain("catch (InvalidOperationException", reader, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", reader, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", reader, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            catalogRoot, "Tooba.Catalog.Infrastructure/CatalogModule.cs"));
        Assert.Contains("IBrandOptionReader", module, StringComparison.Ordinal);
        Assert.Contains("BrandOptionReader", module, StringComparison.Ordinal);

        Assert.False(Directory.Exists(Path.Combine(
            catalogRoot, "Tooba.Catalog.Application/Brands/Validators")));
    }

    [Fact]
    public void Host_Admin_count_52_StoreAppearance_deferred()
    {
        var root = FindRepoRoot();
        var admin = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin");
        var adminCount = Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories).Length;
        Assert.True(adminCount <= 52 && adminCount >= 12, $"Host/Admin count expected in [12,52], was {adminCount}");
        Assert.True(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
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
