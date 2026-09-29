using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>LandingPageDevelopmentSeed owned by Catalog after Host Admin evacuation.</summary>
public sealed class HostAdminAmcLandingPageSeedGuardTests
{
    [Fact]
    public void Landing_page_dev_seed_owned_by_Catalog_and_absent_from_Host_Admin()
    {
        var root = FindRepoRoot();
        Assert.False(File.Exists(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Admin/LandingPageDevelopmentSeed.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Development/LandingPageDevelopmentSeed.cs")));
        // The Host wrapper was evacuated; Program now invokes the Catalog-owned seed
        // through the single Host development composition seam.
        Assert.False(File.Exists(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Development/LandingPageDevelopmentSeedHost.cs")));

        var catalogSeed = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Development/LandingPageDevelopmentSeed.cs"));
        Assert.Contains("PublishedSlug", catalogSeed, StringComparison.Ordinal);
        Assert.Contains("IStoreLandingPageWorkspace", catalogSeed, StringComparison.Ordinal);
        Assert.DoesNotContain("ControlPlaneRegistry", catalogSeed, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync", program, StringComparison.Ordinal);

        var hostSeam = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Development/CatalogDevelopmentSeed.cs"));
        Assert.Contains("IHostEnvironment", hostSeam, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_Admin_count_15_StoreAppearance_evacuated()
    {
        var root = FindRepoRoot();
        var admin = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin");
        Assert.Equal(15, Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories).Length);
        Assert.False(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
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
