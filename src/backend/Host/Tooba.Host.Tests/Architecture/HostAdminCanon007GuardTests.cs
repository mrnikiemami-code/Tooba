using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-CANON-007 — the Development admin actor bootstrap must depend on Identity
/// Contracts only (never Identity.Infrastructure) and must not use a blanket
/// <c>catch (InvalidOperationException)</c> as control flow for the tuple-membership write.
/// The tuple write contract is an idempotent upsert, so it runs without any expected-flow catch.
/// </summary>
public sealed class HostAdminCanon007GuardTests
{
    private const string BootstrapFile = "AdminDevActorBootstrap.cs";

    [Fact]
    public void Dev_actor_bootstrap_has_zero_identity_infrastructure_coupling()
    {
        var text = ReadAdmin(BootstrapFile);
        Assert.DoesNotContain("Tooba.Identity.Infrastructure", text, StringComparison.Ordinal);
        Assert.DoesNotContain("RequestServices", text, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService<IdentityDbContext", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Dev_actor_bootstrap_is_identity_contracts_only()
    {
        var text = ReadAdmin(BootstrapFile);
        Assert.Contains("using Tooba.Identity.Contracts;", text, StringComparison.Ordinal);
        Assert.Contains("IIdentityAuthenticationService", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Dev_actor_bootstrap_has_no_blanket_invalid_operation_catch()
    {
        var text = ReadAdmin(BootstrapFile);
        Assert.DoesNotContain("catch (InvalidOperationException)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (System.InvalidOperationException)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Dev_actor_bootstrap_keeps_typed_duplicate_registration_handling()
    {
        var text = ReadAdmin(BootstrapFile);
        Assert.Contains("catch (IdentityDuplicateIdentifierFault)", text, StringComparison.Ordinal);
        Assert.Contains("RegisterAsync", text, StringComparison.Ordinal);
        Assert.Contains("FindUserIdByIdentifierAsync", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Dev_actor_bootstrap_writes_membership_through_the_idempotent_tuple_contract()
    {
        var text = ReadAdmin(BootstrapFile);
        Assert.Contains("IAuthorizationTupleWriter", text, StringComparison.Ordinal);
        Assert.Contains("AuthorizationObjectTypes.Tenant", text, StringComparison.Ordinal);
        Assert.Contains("AuthorizationRelations.Member", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Dev_actor_bootstrap_behavior_surface_is_preserved()
    {
        var text = ReadAdmin(BootstrapFile);
        Assert.Contains("const string AdminEmail = \"admin-actor@tooba.local\";", text, StringComparison.Ordinal);
        Assert.Contains("admin-dev-horse-1", text, StringComparison.Ordinal);
        Assert.Contains("new AdminDevActorSnapshot(actor.Value,", text, StringComparison.Ordinal);
        Assert.Contains("tenant.TenantId.Value);", text, StringComparison.Ordinal);
        Assert.Contains("private static readonly object Gate = new();", text, StringComparison.Ordinal);
        Assert.Contains("lock (Gate)", text, StringComparison.Ordinal);
        Assert.Contains("ICurrentTenant", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Tuple_writer_contract_is_documented_idempotent_upsert()
    {
        var seam = File.ReadAllText(RepoFile(
            "src/backend/BuildingBlocks/Tooba.BuildingBlocks/Authorization.cs"));
        Assert.Contains("AuthorizationRelationshipOperation.Delete => RelationshipUpdate.Types.Operation.Delete", File.ReadAllText(RepoFile(
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/Authorization/SpiceDbAuthorizationAdapter.cs")), StringComparison.Ordinal);
        Assert.Contains("_tuples[key] = 1;", File.ReadAllText(RepoFile(
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/Authorization/AuthorizationAdapters.cs")), StringComparison.Ordinal);
        Assert.Contains("public interface IAuthorizationTupleWriter", seam, StringComparison.Ordinal);
    }

    [Fact]
    public void Spicedb_authorization_authority_lives_in_access_control_module()
    {
        Assert.False(
            Directory.Exists(RepoFile("src/backend/Host/Tooba.Host/Authorization")),
            "Host/Authorization must stay evacuated into the AccessControl module");
        Assert.True(File.Exists(RepoFile(
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/Authorization/SpiceDbAuthorizationAdapter.cs")));
        Assert.False(
            File.Exists(RepoFile("src/backend/Host/Tooba.Host/Health/SpiceDbHealthProbe.cs")),
            "SpiceDB readiness probe must live with the AccessControl authorization slice");
    }

    [Fact]
    public void Host_admin_count_remains_platform_floor_15()
    {
        var admin = RepoFile("src/backend/Host/Tooba.Host/Admin");
        Assert.Equal(15, Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories).Length);
    }

    [Fact]
    public void Canon006_reader_boundary_is_preserved()
    {
        Assert.True(File.Exists(RepoFile(
            "src/backend/Host/Tooba.Host.Tests/Architecture/HostAdminCanon006GuardTests.cs")));
        var reader = ReadAdmin("HostOrderAdminEffectiveAccessReader.cs");
        Assert.Contains("IPlatformEffectiveAccessReader", reader, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.AccessControl", reader, StringComparison.Ordinal);
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
