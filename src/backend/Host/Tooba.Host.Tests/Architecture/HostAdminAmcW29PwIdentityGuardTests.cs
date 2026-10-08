using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-AMC-001-W29 — ProductWorkspace create/title/core/quantity to module + Catalog Commands.
/// </summary>
public sealed class HostAdminAmcW29PwIdentityGuardTests
{
    private static readonly Regex MapRouteRegex = new(
        @"\bMap(Get|Post|Put|Patch|Delete)\s*\(",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Identity_routes_owned_by_ProductWorkspace_and_absent_from_Host()
    {
        var root = FindRepoRoot();



        var module = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/ProductWorkspace/Tooba.ProductWorkspace.Endpoints/ProductWorkspaceEndpointModule.cs"));
        Assert.Equal(17, MapRouteRegex.Matches(module).Count);
        Assert.Contains("MapPost(\"/\", CreateProductAsync)", module, StringComparison.Ordinal);
        Assert.Contains("MapPatch(\"/{productId:guid}/catalog-title\"", module, StringComparison.Ordinal);
        Assert.Contains("MapPatch(\"/{productId:guid}/core\"", module, StringComparison.Ordinal);
        Assert.Contains("MapPatch(\"/{productId:guid}/quantity-policy\"", module, StringComparison.Ordinal);
        Assert.Contains("CreateWorkspaceProductCommand", module, StringComparison.Ordinal);
        Assert.Contains("UpdateWorkspaceProductCatalogTitleCommand", module, StringComparison.Ordinal);
        Assert.Contains("UpdateWorkspaceProductCoreCommand", module, StringComparison.Ordinal);
        Assert.Contains("UpdateWorkspaceProductQuantityPolicyCommand", module, StringComparison.Ordinal);
        Assert.Contains("GetProductWorkspaceQuery", module, StringComparison.Ordinal);
        Assert.Contains("Status201Created", module, StringComparison.Ordinal);
        Assert.Contains("CanEditCatalog", module, StringComparison.Ordinal);
        Assert.Contains("ICatalogAdminProductWorkspaceMutationGateway", module, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Catalog.Application", module, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", module, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogDirectory", module, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", module, StringComparison.Ordinal);

        Assert.False(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceComposer.cs")));
    }

    [Fact]
    public void Catalog_ProductIdentity_CQRS_port_and_codes_exist()
    {
        var root = FindRepoRoot();
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductIdentity/Commands/CreateWorkspaceProductCommand.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductIdentity/Commands/UpdateProductCatalogTitleCommand.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductIdentity/Commands/UpdateProductCoreCommand.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductIdentity/Commands/UpdateProductQuantityPolicyCommand.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductIdentity/Ports/IProductIdentityDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductIdentity/Models/ProductIdentityWriteModels.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Directories/ProductIdentityDirectory.cs")));

        var directory = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Directories/ProductIdentityDirectory.cs"));
        Assert.Contains("ICatalogDirectory", directory, StringComparison.Ordinal);
        Assert.Contains("CreateProductAsync", directory, StringComparison.Ordinal);
        Assert.Contains("AssignCategoryAsync", directory, StringComparison.Ordinal);
        Assert.Contains("AppendProductHistoryAsync", directory, StringComparison.Ordinal);
        Assert.Contains("Result<Guid>", directory, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/CatalogModule.cs"));
        Assert.Contains("IProductIdentityDirectory", module, StringComparison.Ordinal);
        Assert.Contains("ProductIdentityDirectory", module, StringComparison.Ordinal);

        var codes = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Errors/CatalogErrorCodes.cs"));
        Assert.Contains("workspace.product.title.missing", codes, StringComparison.Ordinal);
        Assert.Contains("workspace.product.category.missing", codes, StringComparison.Ordinal);
        Assert.Contains("workspace.product.category.invalid", codes, StringComparison.Ordinal);
        Assert.Contains("workspace.product.create.rejected", codes, StringComparison.Ordinal);
        Assert.Contains("workspace.product.slug.missing", codes, StringComparison.Ordinal);
        Assert.Contains("workspace.quantity.rejected", codes, StringComparison.Ordinal);
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
