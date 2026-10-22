using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Security;
using Tooba.Shipping.Endpoints.Admin;
using Xunit;

namespace Tooba.Shipping.Tests.Admin;

/// <summary>
/// TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W1 — the Shipping admin adapter must forward the trusted,
/// platform-resolved edition/tenant context and fail closed before any permission check when that
/// context is missing, unsupported or inconsistent.
/// </summary>
public sealed class ShippingAdminAuthorizerContextTests
{
    private const string Permission = "fulfillment.manage";
    private static readonly Guid Actor = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task SingleStore_forwards_resolved_edition_and_active_tenant()
    {
        var authz = new RecordingAuthorizationService(AuthorizationDecision.Allow());
        var authorizer = Create(
            authz,
            Edition(ToobaEdition.SingleStore),
            Tenant(TenantStatus.Active, "store-alpha"));

        var actor = await authorizer.RequireAuthorizedAsync(Context(), Permission, CancellationToken.None);

        Assert.Equal(Actor, actor);
        var check = Assert.Single(authz.Checks);
        Assert.Equal(ToobaEdition.SingleStore, check.CallContext.Edition);
        Assert.Equal("store-alpha", check.CallContext.TenantId);
        Assert.Equal(AuthorizationObjectTypes.Permission, check.Resource.Type);
        Assert.Equal(Permission, check.Resource.Id);
        Assert.Equal(AuthorizationRelations.Check, check.Permission);
        Assert.Equal("user", check.Subject.Type);
        Assert.Equal(Actor.ToString("D"), check.Subject.Id);
    }

    [Fact]
    public async Task SingleStore_tenant_id_is_trimmed_not_invented()
    {
        var authz = new RecordingAuthorizationService(AuthorizationDecision.Allow());
        var authorizer = Create(authz, Edition(ToobaEdition.SingleStore), Tenant(TenantStatus.Active, "  store-alpha  "));

        await authorizer.RequireAuthorizedAsync(Context(), Permission, CancellationToken.None);

        Assert.Equal("store-alpha", Assert.Single(authz.Checks).CallContext.TenantId);
    }

    [Fact]
    public async Task Marketplace_forwards_marketplace_with_null_tenant()
    {
        var authz = new RecordingAuthorizationService(AuthorizationDecision.Allow());
        var authorizer = Create(authz, Edition(ToobaEdition.Marketplace), new StubCurrentTenant(null));

        var actor = await authorizer.RequireAuthorizedAsync(Context(), Permission, CancellationToken.None);

        Assert.Equal(Actor, actor);
        var check = Assert.Single(authz.Checks);
        Assert.Equal(ToobaEdition.Marketplace, check.CallContext.Edition);
        Assert.Null(check.CallContext.TenantId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Unset")]
    [InlineData("Unknown")]
    public async Task Unresolved_unset_or_unknown_edition_fails_closed_before_authorization(string? editionCase)
    {
        var authz = new RecordingAuthorizationService(AuthorizationDecision.Allow());
        ICurrentEdition edition = editionCase switch
        {
            null => new StubCurrentEdition(null),
            "Unset" => Edition(ToobaEdition.Unset),
            _ => new StubCurrentEdition(new EditionContext((ToobaEdition)99, "deployment-test")),
        };
        var authorizer = Create(authz, edition, Tenant(TenantStatus.Active, "store-alpha"));

        var error = await Assert.ThrowsAsync<SemanticException>(
            () => authorizer.RequireAuthorizedAsync(Context(), Permission, CancellationToken.None));

        Assert.Equal(FoundationErrorCodes.AdminAuthorizationUnavailable, error.Error.Code);
        Assert.Empty(authz.Checks);
    }

    [Theory]
    [InlineData(true, "")]
    [InlineData(true, "   ")]
    [InlineData(false, "")]
    public async Task SingleStore_without_a_usable_active_tenant_fails_closed(bool present, string tenantId)
    {
        var authz = new RecordingAuthorizationService(AuthorizationDecision.Allow());
        var authorizer = Create(
            authz,
            Edition(ToobaEdition.SingleStore),
            present ? Tenant(TenantStatus.Active, tenantId) : new StubCurrentTenant(null));

        var error = await Assert.ThrowsAsync<SemanticException>(
            () => authorizer.RequireAuthorizedAsync(Context(), Permission, CancellationToken.None));

        Assert.Equal(FoundationErrorCodes.AdminAuthorizationUnavailable, error.Error.Code);
        Assert.Empty(authz.Checks);
    }

    [Theory]
    [InlineData(TenantStatus.Disabled)]
    [InlineData(TenantStatus.Suspended)]
    public async Task SingleStore_inactive_tenant_fails_closed(TenantStatus status)
    {
        var authz = new RecordingAuthorizationService(AuthorizationDecision.Allow());
        var authorizer = Create(authz, Edition(ToobaEdition.SingleStore), Tenant(status, "store-alpha"));

        var error = await Assert.ThrowsAsync<SemanticException>(
            () => authorizer.RequireAuthorizedAsync(Context(), Permission, CancellationToken.None));

        Assert.Equal(FoundationErrorCodes.AdminAuthorizationUnavailable, error.Error.Code);
        Assert.Empty(authz.Checks);
    }

    [Fact]
    public async Task Marketplace_with_a_leftover_tenant_fails_closed()
    {
        var authz = new RecordingAuthorizationService(AuthorizationDecision.Allow());
        var authorizer = Create(
            authz,
            Edition(ToobaEdition.Marketplace),
            Tenant(TenantStatus.Active, "store-alpha"));

        var error = await Assert.ThrowsAsync<SemanticException>(
            () => authorizer.RequireAuthorizedAsync(Context(), Permission, CancellationToken.None));

        Assert.Equal(FoundationErrorCodes.AdminAuthorizationUnavailable, error.Error.Code);
        Assert.Empty(authz.Checks);
    }

    [Fact]
    public async Task Deny_stays_denied_and_unavailable_is_never_allow()
    {
        var deny = new ShippingAdminAuthorizer(
            new StubAdminPanelAccess(),
            new RecordingAuthorizationService(AuthorizationDecision.Deny()),
            Edition(ToobaEdition.SingleStore),
            Tenant(TenantStatus.Active, "store-alpha"));

        var denied = await Assert.ThrowsAsync<SemanticException>(
            () => deny.RequireAuthorizedAsync(Context(), Permission, CancellationToken.None));
        Assert.Equal(FoundationErrorCodes.AdminAuthorizationDenied, denied.Error.Code);

        var unavailable = new ShippingAdminAuthorizer(
            new StubAdminPanelAccess(),
            new RecordingAuthorizationService(AuthorizationDecision.Unavailable("spicedb_down")),
            Edition(ToobaEdition.SingleStore),
            Tenant(TenantStatus.Active, "store-alpha"));

        var blocked = await Assert.ThrowsAsync<SemanticException>(
            () => unavailable.RequireAuthorizedAsync(Context(), Permission, CancellationToken.None));
        Assert.Equal(FoundationErrorCodes.AdminAuthorizationUnavailable, blocked.Error.Code);
    }

    [Fact]
    public async Task Panel_gate_rejection_is_not_swallowed_and_skips_capability_check()
    {
        var authz = new RecordingAuthorizationService(AuthorizationDecision.Allow());
        var gate = new StubAdminPanelAccess { Failure = new SemanticException(new SemanticError(FoundationErrorCodes.AdminActorMissing)) };
        var authorizer = new ShippingAdminAuthorizer(
            gate,
            authz,
            Edition(ToobaEdition.SingleStore),
            Tenant(TenantStatus.Active, "store-alpha"));

        var error = await Assert.ThrowsAsync<SemanticException>(
            () => authorizer.RequireAuthorizedAsync(Context(), Permission, CancellationToken.None));

        Assert.Equal(FoundationErrorCodes.AdminActorMissing, error.Error.Code);
        Assert.Empty(authz.Checks);
    }

    [Fact]
    public async Task Cancellation_is_not_swallowed()
    {
        var authorizer = new ShippingAdminAuthorizer(
            new StubAdminPanelAccess(),
            new RecordingAuthorizationService(new OperationCanceledException("cancelled")),
            Edition(ToobaEdition.SingleStore),
            Tenant(TenantStatus.Active, "store-alpha"));

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => authorizer.RequireAuthorizedAsync(Context(), Permission, CancellationToken.None));
    }

    [Theory]
    [InlineData("x-tooba-edition", "Marketplace")]
    [InlineData("x-tooba-tenant", "attacker-tenant")]
    [InlineData("edition", "SingleStore")]
    [InlineData("tenantId", "attacker-tenant")]
    public async Task Request_supplied_mode_or_tenant_never_overrides_the_trusted_context(string header, string value)
    {
        var authz = new RecordingAuthorizationService(AuthorizationDecision.Allow());
        var authorizer = Create(authz, Edition(ToobaEdition.SingleStore), Tenant(TenantStatus.Active, "store-alpha"));
        var context = Context();
        context.Request.Headers[header] = value;
        context.Request.QueryString = new QueryString($"?edition=Marketplace&tenantId=attacker-tenant");

        await authorizer.RequireAuthorizedAsync(context, Permission, CancellationToken.None);

        var check = Assert.Single(authz.Checks);
        Assert.Equal(ToobaEdition.SingleStore, check.CallContext.Edition);
        Assert.Equal("store-alpha", check.CallContext.TenantId);
    }

    [Fact]
    public void Adapter_never_parses_raw_request_or_configuration_for_mode_and_tenant()
    {
        var source = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "backend",
            "Modules",
            "Shipping",
            "Tooba.Shipping.Endpoints",
            "Admin",
            "ShippingAdminAuthorizer.cs"));

        Assert.DoesNotContain("Request.Headers", source, StringComparison.Ordinal);
        Assert.DoesNotContain("QueryString", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IConfiguration", source, StringComparison.Ordinal);
        // The permission check receives the resolved context; it is never rebuilt inline from a literal.
        Assert.DoesNotContain("CallContext = new AuthorizationCallContext", source, StringComparison.Ordinal);
        Assert.Contains("CallContext = callContext", source, StringComparison.Ordinal);
        Assert.DoesNotContain("\"unknown\"", source, StringComparison.Ordinal);
        Assert.Contains("ICurrentEdition", source, StringComparison.Ordinal);
        Assert.Contains("ICurrentTenant", source, StringComparison.Ordinal);
        Assert.Contains("FoundationErrorCodes.AdminAuthorizationUnavailable", source, StringComparison.Ordinal);
    }

    private static ShippingAdminAuthorizer Create(
        IAuthorizationService authz,
        ICurrentEdition edition,
        ICurrentTenant tenant) =>
        new(new StubAdminPanelAccess(), authz, edition, tenant);

    private static ICurrentEdition Edition(ToobaEdition edition) =>
        new StubCurrentEdition(new EditionContext(edition, "deployment-test"));

    private static ICurrentTenant Tenant(TenantStatus status, string tenantId) =>
        new StubCurrentTenant(new TenantContext(
            new TenantId(tenantId),
            status,
            new ConnectionReference("store-db"),
            "Test Store",
            null,
            null,
            "store.test",
            null));

    private static DefaultHttpContext Context() => new() { Request = { Method = "POST", Path = "/v1/admin/shipping-services/x/rates" } };

    private static string RepoRoot()
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

        throw new InvalidOperationException("repo root not found");
    }

    private sealed class StubAdminPanelAccess : IAdminPanelAccess
    {
        public Exception? Failure { get; init; }

        public Task<Guid> RequireAuthorizedAsync(HttpRequest request, CancellationToken cancellationToken) =>
            Failure is null ? Task.FromResult(Actor) : Task.FromException<Guid>(Failure);
    }

    private sealed class StubCurrentEdition(EditionContext? current) : ICurrentEdition
    {
        public EditionContext? Current { get; } = current;
    }

    private sealed class StubCurrentTenant(TenantContext? current) : ICurrentTenant
    {
        public TenantContext? Current { get; } = current;
    }

    private sealed class RecordingAuthorizationService : IAuthorizationService
    {
        private readonly AuthorizationDecision? _decision;
        private readonly Exception? _failure;

        public RecordingAuthorizationService(AuthorizationDecision decision) => _decision = decision;

        public RecordingAuthorizationService(Exception failure) => _failure = failure;

        public List<AuthorizationCheck> Checks { get; } = [];

        public Task<AuthorizationDecision> CanAsync(AuthorizationCheck check, CancellationToken cancellationToken)
        {
            Checks.Add(check);
            if (_failure is not null)
            {
                return Task.FromException<AuthorizationDecision>(_failure);
            }

            return Task.FromResult(_decision!);
        }
    }
}
