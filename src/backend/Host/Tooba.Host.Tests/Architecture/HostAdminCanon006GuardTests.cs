using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-CANON-006 — the Host Order admin effective-access reader must depend only on
/// the neutral platform seam (<c>Tooba.BuildingBlocks.Security.IPlatformEffectiveAccessReader</c>)
/// plus <c>Tooba.Order.Contracts.Admin.Operations</c>. Direct AccessControl Application/Domain
/// coupling and <c>IAccessControlDirectory</c> must be ZERO, while the AccessControl-owned adapter
/// implementation must remain inside the AccessControl module.
/// </summary>
public sealed class HostAdminCanon006GuardTests
{
    private const string ReaderFile = "HostOrderAdminEffectiveAccessReader.cs";

    [Fact]
    public void Host_reader_has_zero_accesscontrol_application_or_domain_coupling()
    {
        var text = ReadAdmin(ReaderFile);
        Assert.DoesNotContain("Tooba.AccessControl", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IAccessControlDirectory", text, StringComparison.Ordinal);
        Assert.DoesNotContain("AccessOwnerScope", text, StringComparison.Ordinal);
        Assert.DoesNotContain("RequestServices", text, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_reader_uses_the_neutral_platform_seam()
    {
        var text = ReadAdmin(ReaderFile);
        Assert.Contains("IPlatformEffectiveAccessReader", text, StringComparison.Ordinal);
        Assert.Contains("PlatformAccessOwnerKind.Platform", text, StringComparison.Ordinal);
        Assert.Contains("GetEffectivePermissionsAsync", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_reader_keeps_order_contracts_authority()
    {
        var text = ReadAdmin(ReaderFile);
        Assert.Contains("using Tooba.Order.Contracts.Admin.Operations;", text, StringComparison.Ordinal);
        Assert.Contains("IOrderAdminEffectiveAccessReader", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Order.Application", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Permission_id_and_denied_by_ceiling_mapping_is_preserved()
    {
        var text = ReadAdmin(ReaderFile);
        Assert.Contains("new OrderAdminPermissionGrant(p.PermissionId, p.DeniedByCeiling)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Neutral_seam_exposes_no_module_types_and_owns_denied_by_ceiling()
    {
        var seam = File.ReadAllText(RepoFile(
            "src/backend/BuildingBlocks/Tooba.BuildingBlocks/Security/IPlatformAccessSeams.cs"));
        Assert.Contains("public interface IPlatformEffectiveAccessReader", seam, StringComparison.Ordinal);
        Assert.Contains("bool DeniedByCeiling", seam, StringComparison.Ordinal);
        Assert.Contains("public enum PlatformAccessOwnerKind", seam, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.AccessControl", seam, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Order", seam, StringComparison.Ordinal);
    }

    [Fact]
    public void AccessControl_owns_the_adapter_implementation()
    {
        var adapter = File.ReadAllText(RepoFile(
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/Adapters/Security/PlatformEffectiveAccessReader.cs"));
        Assert.Contains("IPlatformEffectiveAccessReader", adapter, StringComparison.Ordinal);
        Assert.Contains("IAccessControlDirectory", adapter, StringComparison.Ordinal);
        Assert.Contains("AccessControl.Infrastructure", adapter, StringComparison.Ordinal);

        var program = File.ReadAllText(RepoFile("src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains(
            "IPlatformEffectiveAccessReader, Tooba.AccessControl.Infrastructure.Adapters.Security.PlatformEffectiveAccessReader",
            program,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Host_admin_count_remains_platform_floor_15()
    {
        var admin = RepoFile("src/backend/Host/Tooba.Host/Admin");
        Assert.Equal(15, Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories).Length);
    }

    [Fact]
    public void Canon005_and_canon004_surfaces_are_preserved()
    {
        Assert.True(File.Exists(RepoFile(
            "src/backend/Host/Tooba.Host.Tests/Architecture/HostAdminCanon005GuardTests.cs")));
        Assert.True(File.Exists(RepoFile(
            "src/backend/Host/Tooba.Host.Tests/Architecture/HostAdminCanon004GuardTests.cs")));

        var contracts = File.ReadAllText(RepoFile(
            "src/backend/Modules/Order/Tooba.Order.Contracts/Admin/Operations/AdminOrderEffectiveAccessContracts.cs"));
        Assert.Contains("namespace Tooba.Order.Contracts.Admin.Operations;", contracts, StringComparison.Ordinal);

        var authorizer = ReadAdmin("HostOrderAdminAuthorizer.cs");
        Assert.Contains("IAdminPanelAccess adminAccess", authorizer, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.AccessControl.Application", authorizer, StringComparison.Ordinal);
    }

    private static string ReadAdmin(string fileName) =>
        File.ReadAllText(Directory.GetFiles(
            Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Admin"),
            fileName,
            SearchOption.AllDirectories).Single());

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
