using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>Store Landing Admin/Storefront HTTP owned by Catalog after Host evacuation.</summary>
public sealed class HostAdminAmcStoreLandingGuardTests
{
    [Fact]
    public void Store_landing_owned_by_Catalog_and_absent_from_Host_Admin()
    {
        var root = FindRepoRoot();
        Assert.False(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/StoreLandingPageEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/StoreLandingPageComposer.cs")));

        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/CatalogEndpointModule.cs"));
        Assert.Contains("MapCatalogStoreLandingPageAdminEndpoints()", module, StringComparison.Ordinal);
        Assert.Contains("MapCatalogStoreLandingPageStorefrontEndpoints()", module, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.DoesNotContain("MapStoreLandingPageEndpoints()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("StoreLandingPageComposer", program, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/StoreLandingPageWorkspace.cs")));
    }

    [Fact]
    public void Host_Admin_count_28_StoreAppearance_deferred()
    {
        var root = FindRepoRoot();
        var admin = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin");
        Assert.Equal(28, Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories).Length);
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
