using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-AMC-001-W27 — ProductWorkspace variant create/patch to module + Catalog Commands.
/// </summary>
public sealed class HostAdminAmcW27PwVariantsGuardTests
{
    private static readonly Regex MapRouteRegex = new(
        @"\bMap(Get|Post|Put|Patch|Delete)\s*\(",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Variant_routes_owned_by_ProductWorkspace_and_absent_from_Host()
    {
        var root = FindRepoRoot();
        var host = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceEndpoints.cs"));
        Assert.Equal(10, MapRouteRegex.Matches(host).Count);
        Assert.DoesNotContain("/variants", host, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateVariantAsync", host, StringComparison.Ordinal);
        Assert.DoesNotContain("PatchVariantAsync", host, StringComparison.Ordinal);

        var composer = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceComposer.cs"));
        Assert.DoesNotContain("CreateVariantAsync", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("PatchVariantAsync", composer, StringComparison.Ordinal);

        var models = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceModels.cs"));
        Assert.DoesNotContain("AdminProductVariantCreateRequest", models, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminProductVariantPatchRequest", models, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/ProductWorkspace/Tooba.ProductWorkspace.Endpoints/ProductWorkspaceEndpointModule.cs"));
        Assert.Equal(7, MapRouteRegex.Matches(module).Count);
        Assert.Contains("MapPost(\"/{productId:guid}/variants\"", module, StringComparison.Ordinal);
        Assert.Contains("MapPatch(\"/{productId:guid}/variants/{variantId:guid}\"", module, StringComparison.Ordinal);
        Assert.Contains("CreateProductWorkspaceVariantCommand", module, StringComparison.Ordinal);
        Assert.Contains("PatchProductWorkspaceVariantCommand", module, StringComparison.Ordinal);
        Assert.Contains("CanEditCatalog", module, StringComparison.Ordinal);
        Assert.Contains("Status201Created", module, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", module, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogDirectory", module, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", module, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceComposer.cs")));
    }

    [Fact]
    public void Catalog_Variants_workspace_commands_and_codes_exist()
    {
        var root = FindRepoRoot();
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/Variants/Commands/CreateProductWorkspaceVariantCommand.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/Variants/Commands/PatchProductWorkspaceVariantCommand.cs")));
        var port = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/Variants/Ports/IProductVariantDirectory.cs"));
        Assert.Contains("CreateWorkspaceVariantAsync", port, StringComparison.Ordinal);
        Assert.Contains("PatchWorkspaceVariantAsync", port, StringComparison.Ordinal);

        var codes = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Errors/CatalogErrorCodes.cs"));
        Assert.Contains("workspace.variant.axes.missing", codes, StringComparison.Ordinal);
        Assert.Contains("workspace.variant.create.rejected", codes, StringComparison.Ordinal);
        Assert.Contains("workspace.variant.missing", codes, StringComparison.Ordinal);
        Assert.Contains("workspace.variant.status.invalid", codes, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_Admin_count_31_StoreAppearance_deferred_RETAIN_PARTIAL()
    {
        var root = FindRepoRoot();
        var admin = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin");
        Assert.Equal(31, Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories).Length);
        Assert.True(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(admin, "ProductWorkspaceEndpoints.cs")));
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
