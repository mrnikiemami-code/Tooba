using Microsoft.AspNetCore.Http;
using Tooba.AccessControl.Infrastructure.Authorization;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Security;
using Tooba.Host.Admin.Access.Authorizers;
using Tooba.Order.Endpoints.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-CANON-004 — the Host Order admin authorizer must delegate the panel gate
/// through <see cref="IAdminPanelAccess"/> and evaluate permissions with the neutral BuildingBlocks
/// authorization abstraction, failing closed (503) when the authorization engine is unavailable.
/// </summary>
public sealed class HostAdminCanon004GuardTests
{
    private const string AuthorizerFile = "HostOrderAdminAuthorizer.cs";

    private static readonly Guid AdminActor = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaa0004");

    [Fact]
    public void Order_authorizer_depends_on_platform_seam_and_neutral_authorization()
    {
        var text = ReadAdmin(AuthorizerFile);
        Assert.Contains("IAdminPanelAccess adminAccess", text, StringComparison.Ordinal);
        Assert.Contains("IAuthorizationService authz", text, StringComparison.Ordinal);
        Assert.Contains("ICurrentTenant tenant", text, StringComparison.Ordinal);
        Assert.Contains("adminAccess.RequireAuthorizedAsync(context.Request,", text, StringComparison.Ordinal);
        Assert.Contains("AuthorizationDecisionKind.Allow", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Order_authorizer_has_no_accesscontrol_or_service_locator_coupling()
    {
        var text = ReadAdmin(AuthorizerFile);
        Assert.DoesNotContain("AdminPanelAccess.RequireAuthorizedAsync", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.AccessControl.Application", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.AccessControl.Domain", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IAccessControlDirectory", text, StringComparison.Ordinal);
        Assert.DoesNotContain("RequestServices", text, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Order_authorizer_fails_closed_on_unavailable_with_distinct_503_code()
    {
        var text = ReadAdmin(AuthorizerFile);
        Assert.DoesNotContain("fail-open", text, StringComparison.Ordinal);
        Assert.Contains("AuthorizationDecisionKind.Unavailable", text, StringComparison.Ordinal);
        Assert.Contains("StatusCodes.Status503ServiceUnavailable", text, StringComparison.Ordinal);
        Assert.Contains("OrderErrorCodes.AuthorizationUnavailable", text, StringComparison.Ordinal);
        Assert.Contains("OrderErrorCodes.OperationDenied", text, StringComparison.Ordinal);
        Assert.NotEqual(OrderErrorCodes.OperationDenied, OrderErrorCodes.AuthorizationUnavailable);
    }

    [Fact]
    public void Order_module_catalog_owns_the_unavailability_descriptor()
    {
        Assert.Equal("order.operation.denied", OrderErrorCodes.OperationDenied);
        Assert.Equal("order.authorization.unavailable", OrderErrorCodes.AuthorizationUnavailable);

        var catalog = File.ReadAllText(RepoFile(
            "src/backend/Modules/Order/Tooba.Order.Endpoints/Errors/OrderErrorCatalogContributor.cs"));
        Assert.Contains("OrderErrorCodes.AuthorizationUnavailable", catalog, StringComparison.Ordinal);

        var en = File.ReadAllText(RepoFile(
            "src/backend/Modules/Order/Tooba.Order.Endpoints/Resources/OrderErrors.resx"));
        Assert.Contains("order.authorization.unavailable", en, StringComparison.Ordinal);
        var fa = File.ReadAllText(RepoFile(
            "src/backend/Modules/Order/Tooba.Order.Endpoints/Resources/OrderErrors.fa.resx"));
        Assert.Contains("order.authorization.unavailable", fa, StringComparison.Ordinal);
    }

    [Fact]
    public void Order_authorizer_is_registered_as_the_order_admin_boundary()
    {
        var program = File.ReadAllText(RepoFile("src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("AddScoped<IOrderAdminAuthorizer, HostOrderAdminAuthorizer>", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_admin_count_remains_platform_floor_18()
    {
        var admin = RepoFile("src/backend/Host/Tooba.Host/Admin");
        Assert.Equal(18, Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories).Length);
    }

    [Fact]
    public void Effective_access_reader_remains_out_of_scope()
    {
        var reader = ReadAdmin("HostOrderAdminEffectiveAccessReader.cs");
        Assert.Contains("Tooba.Order.Contracts.Admin.Operations", reader, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Order.Application", reader, StringComparison.Ordinal);
        Assert.Contains("IPlatformEffectiveAccessReader", reader, StringComparison.Ordinal);
        Assert.Contains("GetEffectivePermissionsAsync", reader, StringComparison.Ordinal);
    }

    [Fact]
    public void Canon003_guard_surface_is_preserved()
    {
        Assert.True(File.Exists(RepoFile(
            "src/backend/Host/Tooba.Host.Tests/Architecture/HostAdminCanon003GuardTests.cs")));
        var support = ReadAdmin("HostSupportAdminAuthorizer.cs");
        Assert.Contains("IAdminPanelAccess", support, StringComparison.Ordinal);
        Assert.Contains("AuthorizationDecisionKind.Unavailable", support, StringComparison.Ordinal);
        var wallet = ReadAdmin("HostWalletAdminAuthorizer.cs");
        Assert.Contains("IAdminPanelAccess", wallet, StringComparison.Ordinal);
        Assert.Contains("AuthorizationDecisionKind.Unavailable", wallet, StringComparison.Ordinal);
    }

    // ---- behavior ----

    [Fact]
    public async Task Allow_returns_actor()
    {
        var authorizer = await AuthorizerWith(grant: "order.view");
        var actor = await authorizer.RequirePermissionAsync(Http(), "order.view", CancellationToken.None);
        Assert.Equal(AdminActor, actor);
    }

    [Fact]
    public async Task Deny_throws_403_with_stable_order_code()
    {
        var authorizer = await AuthorizerWith(grant: "order.view");
        var denied = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            authorizer.RequirePermissionAsync(Http(), "order.handle", CancellationToken.None));
        Assert.Equal(403, denied.StatusCode);
        Assert.Equal(OrderErrorCodes.OperationDenied, denied.ErrorCode);
    }

    [Fact]
    public async Task Unavailable_throws_503_with_stable_order_code()
    {
        var authorizer = new HostOrderAdminAuthorizer(
            new StubAdminPanelAccess(),
            UnavailableAuthz(),
            CurrentTenant());
        var unavailable = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            authorizer.RequirePermissionAsync(Http(), "order.view", CancellationToken.None));
        Assert.Equal(503, unavailable.StatusCode);
        Assert.Equal(OrderErrorCodes.AuthorizationUnavailable, unavailable.ErrorCode);
    }

    [Fact]
    public async Task RequireAdmin_delegates_panel_gate_without_capability_check()
    {
        var authorizer = new HostOrderAdminAuthorizer(
            new StubAdminPanelAccess(), UnavailableAuthz(), CurrentTenant());
        var actor = await authorizer.RequireAdminAsync(Http(), CancellationToken.None);
        Assert.Equal(AdminActor, actor);
    }

    [Fact]
    public async Task Panel_access_failure_propagates_unchanged()
    {
        var authorizer = new HostOrderAdminAuthorizer(
            new StubAdminPanelAccess(new PlatformHttpException(401, "missing", "admin.actor.missing")),
            UnavailableAuthz(),
            CurrentTenant());
        var ex = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            authorizer.RequirePermissionAsync(Http(), "order.view", CancellationToken.None));
        Assert.Equal(401, ex.StatusCode);
        Assert.Equal("admin.actor.missing", ex.ErrorCode);
    }

    [Fact]
    public async Task Missing_permission_id_is_rejected_before_panel_gate()
    {
        var authorizer = await AuthorizerWith(grant: "order.view");
        await Assert.ThrowsAsync<ArgumentException>(() =>
            authorizer.RequirePermissionAsync(Http(), "  ", CancellationToken.None));
    }

    private static async Task<HostOrderAdminAuthorizer> AuthorizerWith(string grant)
    {
        var (authz, tenant) = await CapabilityAuthz(grant);
        return new HostOrderAdminAuthorizer(new StubAdminPanelAccess(), authz, tenant);
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
