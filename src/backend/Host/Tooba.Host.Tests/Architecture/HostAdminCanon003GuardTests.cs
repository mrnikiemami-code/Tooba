using Microsoft.AspNetCore.Http;
using Tooba.AccessControl.Infrastructure.Authorization;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Security;
using Tooba.Host.Admin.Access.Authorizers;
using Tooba.Support.Endpoints.Admin;
using Tooba.Wallet.Endpoints.Admin;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-CANON-003 — Support/Wallet Host Admin capability checks must fail closed
/// and delegate the panel gate through <see cref="IAdminPanelAccess"/> with no service locator.
/// </summary>
public sealed class HostAdminCanon003GuardTests
{
    private static readonly string[] ScopedAuthorizers =
    [
        "HostSupportAdminAuthorizer.cs",
        "HostWalletAdminAuthorizer.cs",
    ];

    private static readonly Guid AdminActor = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaa0003");

    [Fact]
    public void Support_and_wallet_authorizers_depend_on_IAdminPanelAccess()
    {
        foreach (var name in ScopedAuthorizers)
        {
            var text = ReadAdmin(name);
            Assert.Contains("IAdminPanelAccess adminAccess", text, StringComparison.Ordinal);
            Assert.Contains("adminAccess.RequireAuthorizedAsync(", text, StringComparison.Ordinal);
            Assert.Contains("IAuthorizationService authz", text, StringComparison.Ordinal);
            Assert.Contains("ICurrentTenant tenant", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Support_and_wallet_authorizers_have_no_service_locator()
    {
        foreach (var name in ScopedAuthorizers)
        {
            var text = ReadAdmin(name);
            Assert.DoesNotContain("RequestServices", text, StringComparison.Ordinal);
            Assert.DoesNotContain("GetRequiredService", text, StringComparison.Ordinal);
            Assert.DoesNotContain("AdminPanelAccess.RequireAuthorizedAsync", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Support_and_wallet_authorizers_have_no_fail_open_branch()
    {
        foreach (var name in ScopedAuthorizers)
        {
            var text = ReadAdmin(name);
            Assert.DoesNotContain("fail-open", text, StringComparison.Ordinal);
            Assert.Contains("AuthorizationDecisionKind.Unavailable", text, StringComparison.Ordinal);
            Assert.Contains("throw new SemanticException(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("PlatformHttpException", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Support_and_wallet_unavailable_paths_use_stable_module_codes()
    {
        var support = ReadAdmin("HostSupportAdminAuthorizer.cs");
        Assert.Contains("SupportAdminAuthorizationCodes.AuthorizationUnavailable", support, StringComparison.Ordinal);
        Assert.DoesNotContain("Support.Application", support, StringComparison.Ordinal);
        Assert.DoesNotContain("Support.Application", support, StringComparison.Ordinal);
        Assert.Contains("FoundationErrorCodes.AdminAuthorizationDenied", support, StringComparison.Ordinal);
        Assert.Equal("support.authorization.unavailable", SupportAdminAuthorizationCodes.AuthorizationUnavailable);

        var wallet = ReadAdmin("HostWalletAdminAuthorizer.cs");
        Assert.Contains("WalletAdminAuthorizationCodes.AuthorizationUnavailable", wallet, StringComparison.Ordinal);
        Assert.DoesNotContain("Wallet.Application", wallet, StringComparison.Ordinal);
        Assert.DoesNotContain("Wallet.Application", wallet, StringComparison.Ordinal);
        Assert.Contains("FoundationErrorCodes.AdminAuthorizationDenied", wallet, StringComparison.Ordinal);
        Assert.Equal("wallet.authorization.unavailable", WalletAdminAuthorizationCodes.AuthorizationUnavailable);

        Assert.Equal("admin.authorization.denied", FoundationErrorCodes.AdminAuthorizationDenied);
    }

    [Fact]
    public void Module_error_catalogs_own_the_unavailability_descriptors()
    {
        var support = File.ReadAllText(RepoFile(
            "src/backend/Modules/Support/Tooba.Support.Endpoints/Errors/SupportErrorCatalogContributor.cs"));
        Assert.Contains("SupportAdminAuthorizationCodes.AuthorizationUnavailable", support, StringComparison.Ordinal);

        var wallet = File.ReadAllText(RepoFile(
            "src/backend/Modules/Wallet/Tooba.Wallet.Endpoints/Contracts/Errors/WalletErrorCatalogContributor.cs"));
        Assert.Contains("WalletAdminAuthorizationCodes.AuthorizationUnavailable", wallet, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_admin_count_remains_platform_floor_18()
    {
        var admin = RepoFile("src/backend/Host/Tooba.Host/Admin");
        Assert.Equal(17, Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories).Length);
    }

    [Fact]
    public void Canon002_guard_surface_is_preserved()
    {
        Assert.True(File.Exists(RepoFile(
            "src/backend/Host/Tooba.Host.Tests/Architecture/HostAdminCanon002GuardTests.cs")));
        var settlement = ReadAdmin("HostSettlementAdminAuthorizer.cs");
        Assert.DoesNotContain("MarketplacePlatformTenantId", settlement, StringComparison.Ordinal);
        Assert.Contains("IAdminPanelAccess", settlement, StringComparison.Ordinal);
    }

    // ---- behavior ----

    [Fact]
    public async Task Support_allow_returns_actor()
    {
        var authorizer = await SupportWith(grant: "support.view");
        var actor = await authorizer.RequireAuthorizedAsync(Http(), "support.view", CancellationToken.None);
        Assert.Equal(AdminActor, actor);
    }

    [Fact]
    public async Task Support_deny_returns_403_with_stable_code()
    {
        var authorizer = await SupportWith(grant: "support.view");
        var denied = await Assert.ThrowsAsync<SemanticException>(() =>
            authorizer.RequireAuthorizedAsync(Http(), "support.manage", CancellationToken.None));
        Assert.Equal(FoundationErrorCodes.AdminAuthorizationDenied, denied.Error.Code);
    }

    [Fact]
    public async Task Support_unavailable_returns_503_with_stable_code()
    {
        var authorizer = new HostSupportAdminAuthorizer(
            new StubAdminPanelAccess(),
            UnavailableAuthz(),
            CurrentTenant());
        var unavailable = await Assert.ThrowsAsync<SemanticException>(() =>
            authorizer.RequireAuthorizedAsync(Http(), "support.view", CancellationToken.None));
        Assert.Equal(SupportAdminAuthorizationCodes.AuthorizationUnavailable, unavailable.Error.Code);
    }

    [Fact]
    public async Task Support_panel_access_failure_propagates_unchanged()
    {
        var authorizer = new HostSupportAdminAuthorizer(
            new StubAdminPanelAccess(new PlatformHttpException(401, "missing", "admin.actor.missing")),
            UnavailableAuthz(),
            CurrentTenant());
        var ex = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            authorizer.RequireAuthorizedAsync(Http(), "support.view", CancellationToken.None));
        Assert.Equal(401, ex.StatusCode);
        Assert.Equal("admin.actor.missing", ex.ErrorCode);
    }

    [Fact]
    public async Task Wallet_allow_returns_actor()
    {
        var authorizer = await WalletWith(grant: "wallet.view");
        var actor = await authorizer.RequireAuthorizedAsync(Http(), "wallet.view", CancellationToken.None);
        Assert.Equal(AdminActor, actor);
    }

    [Fact]
    public async Task Wallet_deny_returns_403_with_stable_code()
    {
        var authorizer = await WalletWith(grant: "wallet.view");
        var denied = await Assert.ThrowsAsync<SemanticException>(() =>
            authorizer.RequireAuthorizedAsync(Http(), "wallet.adjust", CancellationToken.None));
        Assert.Equal(FoundationErrorCodes.AdminAuthorizationDenied, denied.Error.Code);
    }

    [Fact]
    public async Task Wallet_unavailable_returns_503_with_stable_code()
    {
        var authorizer = new HostWalletAdminAuthorizer(
            new StubAdminPanelAccess(),
            UnavailableAuthz(),
            CurrentTenant());
        var unavailable = await Assert.ThrowsAsync<SemanticException>(() =>
            authorizer.RequireAuthorizedAsync(Http(), "wallet.view", CancellationToken.None));
        Assert.Equal(WalletAdminAuthorizationCodes.AuthorizationUnavailable, unavailable.Error.Code);
    }

    [Fact]
    public async Task Wallet_panel_access_failure_propagates_unchanged()
    {
        var authorizer = new HostWalletAdminAuthorizer(
            new StubAdminPanelAccess(new PlatformHttpException(403, "denied", "admin.authorization.denied")),
            UnavailableAuthz(),
            CurrentTenant());
        var ex = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            authorizer.RequireAuthorizedAsync(Http(), "wallet.view", CancellationToken.None));
        Assert.Equal(403, ex.StatusCode);
        Assert.Equal("admin.authorization.denied", ex.ErrorCode);
    }

    private static async Task<HostSupportAdminAuthorizer> SupportWith(string grant)
    {
        var (authz, tenant) = await CapabilityAuthz(grant);
        return new HostSupportAdminAuthorizer(new StubAdminPanelAccess(), authz, tenant);
    }

    private static async Task<HostWalletAdminAuthorizer> WalletWith(string grant)
    {
        var (authz, tenant) = await CapabilityAuthz(grant);
        return new HostWalletAdminAuthorizer(new StubAdminPanelAccess(), authz, tenant);
    }

    private static async Task<(IAuthorizationService Authz, ICurrentTenant Tenant)> CapabilityAuthz(string grant)
    {
        var adapter = new InMemoryAuthorizationAdapter(
            new AuthorizationInstrumentation(),
            new InMemoryAuthorizationSecurityEventSink());
        var tenant = CurrentTenant();
        await adapter.WriteAsync(
            new AuthorizationRelationshipWrite
            {
                Subject = AuthorizationSubject.ForUser(AdminActor),
                Resource = new AuthorizationResource
                {
                    Type = AuthorizationObjectTypes.Permission,
                    Id = grant,
                },
                Relation = AuthorizationRelations.Granted,
            },
            CancellationToken.None);
        return (adapter, tenant);
    }

    private static IAuthorizationService UnavailableAuthz() =>
        new FailClosedAuthorizationAdapter("authorization.disabled", new AuthorizationInstrumentation());

    private static HttpContext Http() => new DefaultHttpContext();

    private static ICurrentTenant CurrentTenant() =>
        new StubCurrentTenant(new TenantContext(
            new TenantId("store-alpha"),
            TenantStatus.Active,
            new ConnectionReference("tenant-alpha"),
            "فروشگاه نمونه",
            null,
            null,
            "localhost",
            null));

    private sealed class StubCurrentTenant(TenantContext? current) : ICurrentTenant
    {
        public TenantContext? Current { get; } = current;
    }

    private sealed class StubAdminPanelAccess(PlatformHttpException? failure = null) : IAdminPanelAccess
    {
        public Task<Guid> RequireAuthorizedAsync(HttpRequest request, CancellationToken cancellationToken) =>
            failure is null
                ? Task.FromResult(AdminActor)
                : Task.FromException<Guid>(failure);
    }

    /// <summary>Reads the module-owned Support contributor without duplicating its descriptor list.</summary>
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
