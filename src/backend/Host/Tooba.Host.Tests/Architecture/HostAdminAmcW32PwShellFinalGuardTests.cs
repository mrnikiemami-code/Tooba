using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-AMC-001-W32-PW-SHELL-FINAL — delete Host Admin ProductWorkspace* shells (W17 W24-final).
/// </summary>
public sealed class HostAdminAmcW32PwShellFinalGuardTests
{
    [Fact]
    public void Admin_has_zero_ProductWorkspace_cs_files()
    {
        var root = FindRepoRoot();
        var admin = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin");
        var pw = Directory.GetFiles(admin, "ProductWorkspace*.cs", SearchOption.AllDirectories);
        Assert.Empty(pw);
        Assert.False(File.Exists(Path.Combine(admin, "CatalogActorHttpBinding.cs")));
    }

    [Fact]
    public void Development_bootstrap_exists_and_Program_maps_module_only()
    {
        var root = FindRepoRoot();
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Host/Tooba.Host/Development/ProductWorkspaceDevelopmentBootstrap.cs")));
        var bootstrap = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Host/Tooba.Host/Development/ProductWorkspaceDevelopmentBootstrap.cs"));
        Assert.Contains("namespace Tooba.Host.Development", bootstrap, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.DoesNotContain("MapProductWorkspaceEndpoints()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductWorkspaceComposer", program, StringComparison.Ordinal);
        Assert.Contains("MapProductWorkspaceModuleEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("AddProductWorkspaceEndpointPresentation", program, StringComparison.Ordinal);
        Assert.Contains("ProductWorkspaceDevelopmentBootstrap", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_Admin_count_15_StoreAppearance_evacuated()
    {
        var root = FindRepoRoot();
        var admin = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin");
        Assert.Equal(15, Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories).Length);
        Assert.False(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "HoldPolicySettingsEndpoints.cs")));
    }

    [Fact]
    public void SoT_and_evidence_hostAdminAmcPwShellFinal_present()
    {
        var root = FindRepoRoot();
        var sot = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"hostAdminAmcPwShellFinal\"", sot, StringComparison.Ordinal);
        Assert.True(Directory.Exists(Path.Combine(
            root, "docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W32-PW-SHELL-FINAL")));
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
