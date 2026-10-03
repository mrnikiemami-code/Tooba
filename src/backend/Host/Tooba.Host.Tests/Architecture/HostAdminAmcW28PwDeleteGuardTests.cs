using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-AMC-001-W28 — product DELETE evacuated to Catalog (Offer.Contracts gate).
/// </summary>
public sealed class HostAdminAmcW28PwDeleteGuardTests
{
    private static readonly Regex MapRouteRegex = new(
        @"\bMap(Get|Post|Put|Patch|Delete)\s*\(",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Product_DELETE_owned_by_Catalog_and_absent_from_Host_ProductWorkspace()
    {
        var root = FindRepoRoot();


        var catalogEndpoints = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/ProductDeletion/CatalogProductDeletionAdminEndpoints.cs"));
        Assert.Contains("MapDelete(\"/{productId:guid}\"", catalogEndpoints, StringComparison.Ordinal);
        Assert.Contains("DeleteProductCommand", catalogEndpoints, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", catalogEndpoints, StringComparison.Ordinal);
        Assert.Contains("ICatalogAdminAuthorizer", catalogEndpoints, StringComparison.Ordinal);
        Assert.Contains("CatalogWorkspaceScope.AllowsCatalogEdit", catalogEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", catalogEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("IOfferQueryGateway", catalogEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", catalogEndpoints, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/CatalogEndpointModule.cs"));
        Assert.Contains("MapCatalogProductDeletionAdminEndpoints", module, StringComparison.Ordinal);

        var pw = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/ProductWorkspace/Tooba.ProductWorkspace.Endpoints/ProductWorkspaceEndpointModule.cs"));
        Assert.Equal(17, MapRouteRegex.Matches(pw).Count);
        Assert.DoesNotContain("MapDelete(\"/{productId:guid}\"", pw, StringComparison.Ordinal);
        Assert.DoesNotContain("DeleteProductCommand", pw, StringComparison.Ordinal);

        Assert.False(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceComposer.cs")));
    }

    [Fact]
    public void Catalog_ProductDeletion_CQRS_Offer_Contracts_and_codes()
    {
        var root = FindRepoRoot();
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductDeletion/Commands/DeleteProductCommand.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductDeletion/Ports/IProductDeletionDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Directories/ProductDeletionDirectory.cs")));

        var directory = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Directories/ProductDeletionDirectory.cs"));
        Assert.Contains("IOfferQueryGateway", directory, StringComparison.Ordinal);
        Assert.Contains("AnyOffersForCatalogVariantIdsAsync", directory, StringComparison.Ordinal);
        Assert.Contains("WorkspaceProductDeleteReferenced", directory, StringComparison.Ordinal);

        var appCsproj = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/Tooba.Catalog.Application.csproj"));
        Assert.DoesNotContain("Offer.Contracts", appCsproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Offer.Application", appCsproj, StringComparison.Ordinal);

        var codes = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Errors/CatalogErrorCodes.cs"));
        Assert.Contains("workspace.product.delete.referenced", codes, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_Admin_count_15_StoreAppearance_evacuated_PW_shells_ABSENT()
    {
        var root = FindRepoRoot();
        var admin = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin");
        Assert.Equal(15, Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories).Length);
        Assert.False(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
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
