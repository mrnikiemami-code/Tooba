using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-AMC-001-W30 — ProductWorkspace category/brand taxonomy to module + Catalog Commands.
/// </summary>
public sealed class HostAdminAmcW30PwTaxonomyGuardTests
{
    private static readonly Regex MapRouteRegex = new(
        @"\bMap(Get|Post|Put|Patch|Delete)\s*\(",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Taxonomy_routes_owned_by_ProductWorkspace_and_absent_from_Host()
    {
        var root = FindRepoRoot();



        var module = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/ProductWorkspace/Tooba.ProductWorkspace.Endpoints/ProductWorkspaceEndpointModule.cs"));
        Assert.Equal(17, MapRouteRegex.Matches(module).Count);
        Assert.Contains("MapPut(\"/{productId:guid}/category\"", module, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{productId:guid}/categories/additional\"", module, StringComparison.Ordinal);
        Assert.Contains("MapDelete(\"/{productId:guid}/categories/additional/{categoryId:guid}\"", module, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/{productId:guid}/brand\"", module, StringComparison.Ordinal);
        Assert.Contains("AssignProductCategoryCommand", module, StringComparison.Ordinal);
        Assert.Contains("AddAdditionalCategoryCommand", module, StringComparison.Ordinal);
        Assert.Contains("RemoveAdditionalCategoryCommand", module, StringComparison.Ordinal);
        Assert.Contains("AssignProductBrandCommand", module, StringComparison.Ordinal);
        Assert.Contains("GetProductWorkspaceQuery", module, StringComparison.Ordinal);
        Assert.Contains("expectedUpdatedAt", module, StringComparison.Ordinal);
        Assert.Contains("CategoryAssignmentStale", module, StringComparison.Ordinal);
        Assert.Contains("CanEditCatalog", module, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", module, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogDirectory", module, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", module, StringComparison.Ordinal);

        Assert.False(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceComposer.cs")));
    }

    [Fact]
    public void Catalog_ProductTaxonomy_CQRS_port_and_codes_exist()
    {
        var root = FindRepoRoot();
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductTaxonomy/Commands/AssignProductCategoryCommand.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductTaxonomy/Commands/AddAdditionalCategoryCommand.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductTaxonomy/Commands/RemoveAdditionalCategoryCommand.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductTaxonomy/Commands/AssignProductBrandCommand.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductTaxonomy/Ports/IProductTaxonomyDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductTaxonomy/Models/ProductTaxonomyWriteModels.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Directories/ProductTaxonomyDirectory.cs")));

        var directory = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Directories/ProductTaxonomyDirectory.cs"));
        Assert.Contains("ICatalogDirectory", directory, StringComparison.Ordinal);
        Assert.Contains("ReplaceProductPrimaryCategoryAsync", directory, StringComparison.Ordinal);
        Assert.Contains("AssignCategoryAsync", directory, StringComparison.Ordinal);
        Assert.Contains("AddProductAdditionalCategoryAsync", directory, StringComparison.Ordinal);
        Assert.Contains("RemoveProductAdditionalCategoryAsync", directory, StringComparison.Ordinal);
        Assert.Contains("AppendProductHistoryAsync", directory, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/CatalogModule.cs"));
        Assert.Contains("IProductTaxonomyDirectory", module, StringComparison.Ordinal);
        Assert.Contains("ProductTaxonomyDirectory", module, StringComparison.Ordinal);

        var codes = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Errors/CatalogErrorCodes.cs"));
        Assert.Contains("workspace.product.category.schema-impact", codes, StringComparison.Ordinal);
        Assert.Contains("workspace.product.category.assign.rejected", codes, StringComparison.Ordinal);
        Assert.Contains("workspace.product.brand.invalid", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.category.assignment.duplicate_primary", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.category.assignment.duplicate", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.category.assignment.invalid", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.category.assignment.cannot_remove_primary", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.category.assignment.missing", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.category.assignment.stale", codes, StringComparison.Ordinal);
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
