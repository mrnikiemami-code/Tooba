using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Support.Endpoints.Admin;
using Tooba.Wallet.Endpoints.Admin;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-CANON-009 — the two remaining Host/Admin authorizers must carry ZERO foreign
/// module Application (.Application) dependency while keeping the stable admin-auth machine codes and
/// their 403/503 classification exactly as before, with exactly one registered descriptor per code.
/// </summary>
public sealed class HostAdminCanon009GuardTests
{
    private static readonly string[] AdminFiles =
    [
        "Access/AdminPanelAccess.cs",
        "Access/HostAdminPanelAccess.cs",
        "Access/Authorizers/HostOrderAdminAuthorizer.cs",
        "Access/Authorizers/HostOrderAdminEffectiveAccessReader.cs",
        "Access/Authorizers/HostPaymentAdminAuthorizer.cs",
        "Access/Authorizers/HostPromotionAdminAuthorizer.cs",
        "Access/Authorizers/HostReturnAdminAuthorizer.cs",
        "Access/Authorizers/HostSettlementAdminAuthorizer.cs",
        "Access/Authorizers/HostSupportAdminAuthorizer.cs",
        "Access/Authorizers/HostWalletAdminAuthorizer.cs",
        "Panel/AdminPanelComposer.cs",
        "Panel/AdminPanelEndpoints.cs",
        "Panel/AdminPanelModels.cs",
        "Grid/AdminGridQueryEndpoint.cs",
        "Development/AdminDevActorBootstrap.cs",
    ];

    [Fact]
    public void Host_admin_authorizers_have_zero_foreign_application_reference()
    {
        var support = ReadAdmin("Access/Authorizers/HostSupportAdminAuthorizer.cs");
        Assert.DoesNotContain("Tooba.Support.Application", support, StringComparison.Ordinal);
        Assert.DoesNotContain("SupportErrorCodes", support, StringComparison.Ordinal);
        Assert.Contains("Tooba.Support.Endpoints.Admin", support, StringComparison.Ordinal);

        var wallet = ReadAdmin("Access/Authorizers/HostWalletAdminAuthorizer.cs");
        Assert.DoesNotContain("Tooba.Wallet.Application", wallet, StringComparison.Ordinal);
        Assert.DoesNotContain("WalletErrorCodes", wallet, StringComparison.Ordinal);
        Assert.Contains("Tooba.Wallet.Endpoints.Admin", wallet, StringComparison.Ordinal);
    }

    [Fact]
    public void Stable_admin_auth_codes_are_unchanged()
    {
        Assert.Equal("admin.authorization.denied", FoundationErrorCodes.AdminAuthorizationDenied);
        Assert.Equal("support.authorization.unavailable", SupportAdminAuthorizationCodes.AuthorizationUnavailable);
        Assert.Equal("wallet.authorization.unavailable", WalletAdminAuthorizationCodes.AuthorizationUnavailable);

        var support = ReadAdmin("Access/Authorizers/HostSupportAdminAuthorizer.cs");
        Assert.Contains("FoundationErrorCodes.AdminAuthorizationDenied", support, StringComparison.Ordinal);
        Assert.Contains("SupportAdminAuthorizationCodes.AuthorizationUnavailable", support, StringComparison.Ordinal);

        var wallet = ReadAdmin("Access/Authorizers/HostWalletAdminAuthorizer.cs");
        Assert.Contains("FoundationErrorCodes.AdminAuthorizationDenied", wallet, StringComparison.Ordinal);
        Assert.Contains("WalletAdminAuthorizationCodes.AuthorizationUnavailable", wallet, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_authorizers_keep_403_and_fail_closed_503_statuses()
    {
        var support = ReadAdmin("Access/Authorizers/HostSupportAdminAuthorizer.cs");
        Assert.Contains("PlatformHttpException(", support, StringComparison.Ordinal);
        Assert.Contains("403", support, StringComparison.Ordinal);
        Assert.Contains("AuthorizationDecisionKind.Unavailable", support, StringComparison.Ordinal);
        Assert.Contains("503", support, StringComparison.Ordinal);
        Assert.Contains("AuthorizationDecisionKind.Allow", support, StringComparison.Ordinal);

        var wallet = ReadAdmin("Access/Authorizers/HostWalletAdminAuthorizer.cs");
        Assert.Contains("PlatformHttpException(", wallet, StringComparison.Ordinal);
        Assert.Contains("403", wallet, StringComparison.Ordinal);
        Assert.Contains("AuthorizationDecisionKind.Unavailable", wallet, StringComparison.Ordinal);
        Assert.Contains("503", wallet, StringComparison.Ordinal);
        Assert.Contains("AuthorizationDecisionKind.Allow", wallet, StringComparison.Ordinal);
    }

    [Fact]
    public void Unavailable_descriptors_remain_platform_503_and_exactly_once()
    {
        var supportCatalog = ReadRepo("src/backend/Modules/Support/Tooba.Support.Endpoints/Errors/SupportErrorCatalogContributor.cs");
        AssertDescriptorOnce(supportCatalog, "SupportAdminAuthorizationCodes.AuthorizationUnavailable");

        var walletCatalog = ReadRepo("src/backend/Modules/Wallet/Tooba.Wallet.Endpoints/Errors/WalletErrorCatalogContributor.cs");
        AssertDescriptorOnce(walletCatalog, "WalletAdminAuthorizationCodes.AuthorizationUnavailable");

        Assert.Equal(0, CountDescriptors(supportCatalog, "admin.authorization.denied"));
        Assert.Equal(0, CountDescriptors(walletCatalog, "admin.authorization.denied"));

        var supportEndpoints = ReadRepo("src/backend/Modules/Support/Tooba.Support.Endpoints/Admin/SupportAdminAuthorizationCodes.cs");
        Assert.DoesNotContain("ErrorDescriptor", supportEndpoints, StringComparison.Ordinal);
        var walletEndpoints = ReadRepo("src/backend/Modules/Wallet/Tooba.Wallet.Endpoints/Admin/WalletAdminAuthorizationCodes.cs");
        Assert.DoesNotContain("ErrorDescriptor", walletEndpoints, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_recursive_file_count_is_15_and_canon008_structure_is_preserved()
    {
        var root = AdminRoot();
        Assert.Empty(Directory.GetFiles(root, "*.cs", SearchOption.TopDirectoryOnly));

        var discovered = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Select(p => Path.GetRelativePath(root, p).Replace('\\', '/'))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(15, discovered.Length);
        Assert.Equal(AdminFiles.OrderBy(x => x, StringComparer.Ordinal).ToArray(), discovered);
    }

    private static void AssertDescriptorOnce(string catalogText, string codeSymbol)
    {
        var lines = DescriptorLines(catalogText).ToArray();

        var matches = lines.Where(x => x.Contains(codeSymbol, StringComparison.Ordinal)).ToArray();
        Assert.Single(matches);
        Assert.Contains("ErrorClassification.Platform", matches[0], StringComparison.Ordinal);
        Assert.Contains("Status503ServiceUnavailable", matches[0], StringComparison.Ordinal);
    }

    private static int CountDescriptors(string catalogText, string code) =>
        DescriptorLines(catalogText).Count(x => x.Contains(code, StringComparison.Ordinal));

    private static IEnumerable<string> DescriptorLines(string catalogText) =>
        catalogText
            .Split('\n')
            .Select(x => x.Trim())
            .Where(x => x.StartsWith("D(", StringComparison.Ordinal));

    private static string ReadAdmin(string relative)
    {
        var path = Path.Combine(AdminRoot(), relative.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(path), $"missing {path}");
        return File.ReadAllText(path);
    }

    private static string ReadRepo(string relative)
    {
        var path = Path.Combine(FindRepoRoot(), relative.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(path), $"missing {path}");
        return File.ReadAllText(path);
    }

    private static string AdminRoot() => Path.Combine(HostRoot(), "Admin");

    private static string HostRoot() =>
        Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host");

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
