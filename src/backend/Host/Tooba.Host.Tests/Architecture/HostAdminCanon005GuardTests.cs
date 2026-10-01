using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-CANON-005 — the Order admin effective-access port/model authority must be
/// owned by <c>Tooba.Order.Contracts</c>; the Host reader path must have ZERO
/// <c>Tooba.Order.Application</c> reference, with exactly one authoritative definition of each
/// contract type and no duplicate public authority left in Order.Application.
/// AccessControl coupling on the reader is additionally pinned to the neutral platform seam
/// (CANON-006) and must never regress to AccessControl Application/Domain.
/// </summary>
public sealed class HostAdminCanon005GuardTests
{
    private const string ReaderFile = "HostOrderAdminEffectiveAccessReader.cs";

    [Fact]
    public void Host_reader_consumes_order_contracts_only()
    {
        var text = ReadAdmin(ReaderFile);
        Assert.Contains("using Tooba.Order.Contracts.Admin.Operations;", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Order.Application", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Admin.Operations.Ports", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_reader_accesscontrol_dependency_is_contracts_or_neutral_seam_only()
    {
        var text = ReadAdmin(ReaderFile);
        Assert.Contains("IPlatformEffectiveAccessReader", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IAccessControlDirectory", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.AccessControl", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Order_contracts_own_the_effective_access_authority_exactly_once()
    {
        var text = File.ReadAllText(RepoFile(
            "src/backend/Modules/Order/Tooba.Order.Contracts/Admin/Operations/AdminOrderEffectiveAccessContracts.cs"));
        Assert.Equal(1, Occurrences(text, "public interface IOrderAdminEffectiveAccessReader"));
        Assert.Equal(1, Occurrences(text, "public sealed record OrderAdminEffectiveAccess("));
        Assert.Equal(1, Occurrences(text, "public sealed record OrderAdminPermissionGrant("));
        Assert.Contains("namespace Tooba.Order.Contracts.Admin.Operations;", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Order_application_has_no_duplicate_public_authority()
    {
        var text = File.ReadAllText(RepoFile(
            "src/backend/Modules/Order/Tooba.Order.Application/Admin/Operations/Ports/AdminOrderOperationsPorts.cs"));
        Assert.DoesNotContain("public interface IOrderAdminEffectiveAccessReader", text, StringComparison.Ordinal);
        Assert.DoesNotContain("public sealed record OrderAdminEffectiveAccess(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("public sealed record OrderAdminPermissionGrant(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Authoritative_contract_types_have_a_single_definition_repo_wide()
    {
        var contracts = CountRepoWide("public interface IOrderAdminEffectiveAccessReader");
        var model = CountRepoWide("public sealed record OrderAdminEffectiveAccess(");
        var grant = CountRepoWide("public sealed record OrderAdminPermissionGrant(");
        Assert.Equal(1, contracts);
        Assert.Equal(1, model);
        Assert.Equal(1, grant);
    }

    [Fact]
    public void Host_reader_still_registered_against_contracts_authority()
    {
        var program = File.ReadAllText(RepoFile("src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains(
            "Tooba.Order.Contracts.Admin.Operations.IOrderAdminEffectiveAccessReader,",
            program,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "Tooba.Order.Application.Admin.Operations.Ports.IOrderAdminEffectiveAccessReader",
            program,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Host_admin_count_remains_platform_floor_19()
    {
        var admin = RepoFile("src/backend/Host/Tooba.Host/Admin");
        Assert.Equal(19, Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories).Length);
    }

    [Fact]
    public void Canon004_authorizer_surface_is_preserved()
    {
        Assert.True(File.Exists(RepoFile(
            "src/backend/Host/Tooba.Host.Tests/Architecture/HostAdminCanon004GuardTests.cs")));
        var authorizer = ReadAdmin("HostOrderAdminAuthorizer.cs");
        Assert.Contains("IAdminPanelAccess adminAccess", authorizer, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.AccessControl.Application", authorizer, StringComparison.Ordinal);
    }

    private static int Occurrences(string text, string needle)
    {
        var count = 0;
        var index = text.IndexOf(needle, StringComparison.Ordinal);
        while (index >= 0)
        {
            count++;
            index = text.IndexOf(needle, index + needle.Length, StringComparison.Ordinal);
        }

        return count;
    }

    private static int CountRepoWide(string needle)
    {
        var backend = RepoFile("src/backend");
        var count = 0;
        foreach (var file in Directory.GetFiles(backend, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.EndsWith("Tests.cs", StringComparison.Ordinal))
            {
                continue;
            }

            count += Occurrences(File.ReadAllText(file), needle);
        }

        return count;
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
