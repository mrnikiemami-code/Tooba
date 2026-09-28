using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-CANON-002 — simple Host Admin endpoint-authorizer adapters delegate
/// through the Host platform seam <c>IAdminPanelAccess</c> with no service locator.
/// </summary>
public sealed class HostAdminCanon002GuardTests
{
    private static readonly string[] SimpleAuthorizers =
    [
        "HostPaymentAdminAuthorizer.cs",
        "HostPromotionAdminAuthorizer.cs",
        "HostReturnAdminAuthorizer.cs",
        "HostSettlementAdminAuthorizer.cs",
    ];

    [Fact]
    public void Simple_admin_authorizers_contain_no_service_locator()
    {
        foreach (var name in SimpleAuthorizers)
        {
            var text = ReadAdmin(name);
            Assert.DoesNotContain("RequestServices", text, StringComparison.Ordinal);
            Assert.DoesNotContain("GetRequiredService", text, StringComparison.Ordinal);
            Assert.DoesNotContain("GetService(", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Simple_admin_authorizers_depend_on_IAdminPanelAccess()
    {
        foreach (var name in SimpleAuthorizers)
        {
            var text = ReadAdmin(name);
            Assert.Contains("IAdminPanelAccess", text, StringComparison.Ordinal);
            Assert.Contains("adminAccess.RequireAuthorizedAsync(", text, StringComparison.Ordinal);
            Assert.Contains(".Request,", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Simple_admin_authorizers_own_no_tenant_or_guard_decision_logic()
    {
        foreach (var name in SimpleAuthorizers)
        {
            var text = ReadAdmin(name);
            Assert.DoesNotContain("ICurrentTenant", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IAuthorizationGuard", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ControlPlaneRegistry", text, StringComparison.Ordinal);
            Assert.DoesNotContain("CurrentAuthenticatedSession", text, StringComparison.Ordinal);
            Assert.DoesNotContain("AuthorizationCheck", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ToobaEdition", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Settlement_adapter_has_no_duplicate_marketplace_platform_logic()
    {
        var settlement = ReadAdmin("HostSettlementAdminAuthorizer.cs");
        Assert.DoesNotContain("MarketplacePlatformTenantId", settlement, StringComparison.Ordinal);
        Assert.DoesNotContain("marketplace-platform", settlement, StringComparison.Ordinal);
        Assert.DoesNotContain("AuthorizationDecisionKind", settlement, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminPanelAccess.", settlement, StringComparison.Ordinal);
    }

    [Fact]
    public void HostAdminPanelAccess_is_the_platform_implementation()
    {
        var text = ReadAdmin("HostAdminPanelAccess.cs");
        Assert.Contains(": IAdminPanelAccess", text, StringComparison.Ordinal);
        Assert.Contains("public const string MarketplacePlatformTenantId", text, StringComparison.Ordinal);
        Assert.Contains("environment.IsDevelopment()", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Development_bootstrap_uses_platform_seam_constant()
    {
        var bootstrap = File.ReadAllText(RepoFile(
            "src/backend/Host/Tooba.Host/Development/MarketplaceAdminDevBootstrap.cs"));
        Assert.Contains("HostAdminPanelAccess.MarketplacePlatformTenantId", bootstrap, StringComparison.Ordinal);
        Assert.DoesNotContain("HostSettlementAdminAuthorizer.MarketplacePlatformTenantId", bootstrap, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_admin_count_remains_platform_floor_15()
    {
        var admin = RepoFile("src/backend/Host/Tooba.Host/Admin");
        Assert.Equal(15, Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories).Length);
    }

    [Fact]
    public void Program_still_registers_the_three_bound_admin_adapters()
    {
        var program = File.ReadAllText(RepoFile("src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("HostPaymentAdminAuthorizer", program, StringComparison.Ordinal);
        Assert.Contains("HostReturnAdminAuthorizer", program, StringComparison.Ordinal);
        Assert.Contains("HostSettlementAdminAuthorizer", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Canon001_guard_surface_is_preserved()
    {
        Assert.True(File.Exists(RepoFile(
            "src/backend/Host/Tooba.Host.Tests/Architecture/HostAdminCanon001GuardTests.cs")));
        var composer = File.ReadAllText(RepoFile("src/backend/Host/Tooba.Host/Admin/AdminPanelComposer.cs"));
        Assert.Contains("ICatalogAdminProductCountGateway", composer, StringComparison.Ordinal);
        Assert.Contains("IPartyAdminSellerReadGateway", composer, StringComparison.Ordinal);
    }

    private static string ReadAdmin(string fileName) =>
        File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Admin", fileName));

    private static string RepoFile(string relative) =>
        Path.Combine(FindRepoRoot(), relative.Replace('/', Path.DirectorySeparatorChar));

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
