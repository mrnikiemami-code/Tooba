using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-AMC-001-W26 — ProductWorkspace lifecycle POSTs to module + Catalog Commands.
/// </summary>
public sealed class HostAdminAmcW26PwLifecycleGuardTests
{
    private static readonly Regex MapRouteRegex = new(
        @"\bMap(Get|Post|Put|Patch|Delete)\s*\(",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Lifecycle_POSTs_owned_by_ProductWorkspace_and_absent_from_Host()
    {
        var root = FindRepoRoot();
        var host = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceEndpoints.cs"));
        Assert.Equal(10, MapRouteRegex.Matches(host).Count);
        Assert.DoesNotContain("MapPost(\"/{productId:guid}/publish\"", host, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/{productId:guid}/unpublish\"", host, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/{productId:guid}/archive\"", host, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/{productId:guid}/restore\"", host, StringComparison.Ordinal);

        var composer = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceComposer.cs"));
        Assert.DoesNotContain("public async Task<ProductWorkspaceView> PublishAsync", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("EnsurePublish", composer, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/ProductWorkspace/Tooba.ProductWorkspace.Endpoints/ProductWorkspaceEndpointModule.cs"));
        Assert.Equal(7, MapRouteRegex.Matches(module).Count);
        Assert.Contains("MapPost(\"/{productId:guid}/publish\"", module, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{productId:guid}/unpublish\"", module, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{productId:guid}/archive\"", module, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{productId:guid}/restore\"", module, StringComparison.Ordinal);
        Assert.Contains("PublishProductCommand", module, StringComparison.Ordinal);
        Assert.Contains("GetProductWorkspaceQuery", module, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", module, StringComparison.Ordinal);
        Assert.Contains("CanPublish", module, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", module, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogDirectory", module, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", module, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceComposer.cs")));
        Assert.True(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceModels.cs")));
    }

    [Fact]
    public void Catalog_ProductPublishing_Commands_and_lifecycle_port_exist()
    {
        var root = FindRepoRoot();
        var commands = Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductPublishing/Commands");
        Assert.True(Directory.Exists(commands));
        Assert.True(File.Exists(Path.Combine(commands, "PublishProductCommand.cs")));
        Assert.True(File.Exists(Path.Combine(commands, "UnpublishProductCommand.cs")));
        Assert.True(File.Exists(Path.Combine(commands, "ArchiveProductCommand.cs")));
        Assert.True(File.Exists(Path.Combine(commands, "RestoreProductCommand.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductPublishing/Ports/IProductLifecycleDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/ProductLifecycleDirectory.cs")));

        var codes = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Errors/CatalogErrorCodes.cs"));
        Assert.Contains("WorkspaceProductPublishRejected", codes, StringComparison.Ordinal);
        Assert.Contains("workspace.product.publish.rejected", codes, StringComparison.Ordinal);
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
