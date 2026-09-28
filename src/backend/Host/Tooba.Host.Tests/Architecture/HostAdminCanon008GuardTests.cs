using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-CANON-008 — the remaining Host/Admin platform files must live in the shallow
/// capability-first structure (Access, Access/Authorizers, Panel, Grid, Development), with namespaces
/// exactly matching the physical capability path. No flat root file, no duplicate old-path shim, no
/// compatibility wrapper, and the CANON-001..007 seams stay intact.
/// </summary>
public sealed class HostAdminCanon008GuardTests
{
    private static readonly Dictionary<string, string[]> ExpectedStructure = new(StringComparer.Ordinal)
    {
        ["Access"] =
        [
            "AdminPanelAccess.cs",
            "HostAdminPanelAccess.cs",
        ],
        ["Access/Authorizers"] =
        [
            "HostOrderAdminAuthorizer.cs",
            "HostOrderAdminEffectiveAccessReader.cs",
            "HostPaymentAdminAuthorizer.cs",
            "HostPromotionAdminAuthorizer.cs",
            "HostReturnAdminAuthorizer.cs",
            "HostSettlementAdminAuthorizer.cs",
            "HostSupportAdminAuthorizer.cs",
            "HostWalletAdminAuthorizer.cs",
        ],
        ["Panel"] =
        [
            "AdminPanelComposer.cs",
            "AdminPanelEndpoints.cs",
            "AdminPanelModels.cs",
        ],
        ["Grid"] =
        [
            "AdminGridQueryEndpoint.cs",
        ],
        ["Development"] =
        [
            "AdminDevActorBootstrap.cs",
        ],
    };

    [Fact]
    public void Admin_root_has_zero_flat_cs_files()
    {
        var root = AdminRoot();
        Assert.Empty(Directory.GetFiles(root, "*.cs", SearchOption.TopDirectoryOnly));
    }

    [Fact]
    public void Admin_recursive_cs_file_count_is_15_and_membership_matches_target()
    {
        var root = AdminRoot();
        var discovered = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Select(p => Path.GetRelativePath(root, p).Replace('\\', '/'))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        var expected = ExpectedStructure
            .SelectMany(kv => kv.Value.Select(f => $"{kv.Key}/{f}"))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(15, discovered.Length);
        Assert.Equal(expected, discovered);
    }

    [Fact]
    public void Each_admin_file_namespace_matches_capability_path()
    {
        var root = AdminRoot();
        foreach (var (folder, files) in ExpectedStructure)
        {
            var expectedNamespace = "Tooba.Host.Admin." + folder.Replace('/', '.');
            foreach (var file in files)
            {
                var path = Path.Combine(root, folder.Replace('/', Path.DirectorySeparatorChar), file);
                Assert.True(File.Exists(path), $"missing {path}");
                var text = File.ReadAllText(path);
                var match = Regex.Match(text, @"(?m)^namespace\s+([A-Za-z0-9_.]+);");
                Assert.True(match.Success, $"no file-scoped namespace in {file}");
                Assert.Equal(expectedNamespace, match.Groups[1].Value);
            }
        }
    }

    [Fact]
    public void No_duplicate_old_path_files_or_root_namespace_shim_remain()
    {
        var host = HostRoot();
        foreach (var file in ExpectedStructure.SelectMany(kv => kv.Value))
        {
            Assert.False(File.Exists(Path.Combine(host, "Admin", file)), $"flat old-path file still present: {file}");
        }

        var admin = AdminRoot();
        var stale = Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories)
            .Where(p => Regex.IsMatch(File.ReadAllText(p), @"(?m)^namespace\s+Tooba\.Host\.Admin;"))
            .Select(Path.GetFileName)
            .ToArray();
        Assert.Empty(stale);
    }

    [Fact]
    public void Canon001_through_007_seams_are_preserved()
    {
        var host = HostRoot();
        var program = File.ReadAllText(Path.Combine(host, "Program.cs"));

        Assert.Contains("Tooba.Host.Admin.Access.Authorizers.HostOrderAdminEffectiveAccessReader", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.Host.Admin.Access.HostAdminPanelAccess", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.Host.Admin.Access.Authorizers.HostWalletAdminAuthorizer", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.Host.Admin.Panel.AdminPanelComposer", program, StringComparison.Ordinal);
        Assert.Contains("MapAdminPanelEndpoints()", program, StringComparison.Ordinal);

        var reader = File.ReadAllText(Path.Combine(host, "Admin", "Access", "Authorizers", "HostOrderAdminEffectiveAccessReader.cs"));
        Assert.Contains("Tooba.Order.Contracts.Admin.Operations", reader, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Order.Application", reader, StringComparison.Ordinal);
        Assert.DoesNotContain("AccessControl.Application", reader, StringComparison.Ordinal);
        Assert.DoesNotContain("AccessControl.Domain", reader, StringComparison.Ordinal);

        var bootstrap = File.ReadAllText(Path.Combine(host, "Admin", "Development", "AdminDevActorBootstrap.cs"));
        Assert.Contains("Tooba.Identity.Contracts", bootstrap, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Identity.Infrastructure", bootstrap, StringComparison.Ordinal);
    }

    [Fact]
    public void No_module_specific_business_endpoint_file_reintroduced_in_admin()
    {
        var admin = AdminRoot();
        var forbidden = new[]
        {
            "AdminOrderOperationsEndpoints.cs",
            "AdminOrderOperationsComposer.cs",
            "AdminOrderOperationsModels.cs",
            "ProductWorkspaceEndpoints.cs",
            "QuantitySettingsEndpoints.cs",
            "CatalogAttributeEndpoints.cs",
            "StoreAppearanceSettingsEndpoints.cs",
        };
        var discovered = Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories)
            .Select(Path.GetFileName)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var name in forbidden)
        {
            Assert.DoesNotContain(name, discovered);
        }
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
