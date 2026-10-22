using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Correlation;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Security;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Endpoints.Admin;
using Xunit;

namespace Tooba.Shipping.Tests.Admin;

/// <summary>
/// TB-SHIPPING-ADMIN-AUTHORIZED-OWNER-SCOPE-189 — the server-authorized owner-scope gate.
/// </summary>
/// <remarks>
/// The adapter under test is the production one; only the platform seams it consumes
/// (<see cref="IShippingAdminAuthorizer"/>, <see cref="ISellerPanelAccess"/>,
/// <see cref="IPlatformEffectiveAccessReader"/>, edition/tenant context) are controlled fakes.
/// That is exactly the boundary the task allows to fake; no running identity store is claimed.
/// </remarks>
[Collection(nameof(Tooba.Shipping.Tests.Observability.ShippingModuleCallTracingCollection))]
public sealed class ShippingAuthoringOwnerAuthorizerTests
{
    private static readonly Guid Actor = Guid.Parse("01900000-0000-7000-8000-00000000ff01");
    private static readonly Guid Seller = Guid.Parse("01900000-0000-7000-8000-00000000bb01");
    private static readonly Guid OtherSeller = Guid.Parse("01900000-0000-7000-8000-00000000bb02");

    [Fact]
    public async Task Admin_SingleStore_returns_Store_with_the_stable_tenant_and_no_seller()
    {
        var admin = new RecordingAdminAuthorizer();
        var seller = new RecordingSellerAccess();
        var reader = new RecordingEffectiveAccess([]);

        var scope = await Create(adminAuthorizer: admin, sellerAccess: seller, reader: reader,
                edition: Edition(ToobaEdition.SingleStore), tenant: Tenant(TenantStatus.Active, "  store-alpha  "))
            .RequireAdminOwnerAsync(Context(), ShippingAdminPermissions.View, CancellationToken.None);

        Assert.Equal(ShippingAuthoringOwnerKind.Store, scope.Kind);
        Assert.Equal("store-alpha", scope.TenantId);
        Assert.Null(scope.SellerPartyId);

        // The existing admin gate really ran with the existing catalog permission.
        Assert.Equal(ShippingAdminPermissions.View, Assert.Single(admin.Permissions));

        // The seller seams are irrelevant to the admin scope and must not be touched.
        Assert.Equal(0, seller.Calls);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task Admin_Marketplace_returns_Marketplace_without_tenant_or_seller()
    {
        var scope = await Create(
                edition: Edition(ToobaEdition.Marketplace),
                tenant: new StubCurrentTenant(null))
            .RequireAdminOwnerAsync(Context(), ShippingAdminPermissions.View, CancellationToken.None);

        Assert.Equal(ShippingAuthoringOwnerKind.Marketplace, scope.Kind);
        Assert.Null(scope.TenantId);
        Assert.Null(scope.SellerPartyId);
    }

    [Theory]
    [InlineData("null-edition")]
    [InlineData("unset")]
    [InlineData("unknown")]
    [InlineData("inactive-tenant")]
    [InlineData("missing-tenant")]
    [InlineData("marketplace-with-tenant")]
    public async Task Admin_unresolved_or_inconsistent_context_fails_closed_with_the_canonical_code(string scenario)
    {
        var edition = scenario switch
        {
            "null-edition" => new StubCurrentEdition(null),
            "unset" => Edition(ToobaEdition.Unset),
            "unknown" => new StubCurrentEdition(new EditionContext((ToobaEdition)99, "deployment-test")),
            "marketplace-with-tenant" => Edition(ToobaEdition.Marketplace),
            _ => Edition(ToobaEdition.SingleStore),
        };
        var tenant = scenario switch
        {
            "inactive-tenant" => Tenant(TenantStatus.Suspended, "store-alpha"),
            "missing-tenant" => new StubCurrentTenant(null),
            "marketplace-with-tenant" => Tenant(TenantStatus.Active, "store-alpha"),
            _ => Tenant(TenantStatus.Active, "store-alpha"),
        };

        var error = await Assert.ThrowsAsync<SemanticException>(
            () => Create(edition: edition, tenant: tenant)
                .RequireAdminOwnerAsync(Context(), ShippingAdminPermissions.View, CancellationToken.None));

        Assert.Equal(FoundationErrorCodes.AdminAuthorizationUnavailable, error.Error.Code);
    }

    [Fact]
    public async Task Seller_route_is_capability_unavailable_in_a_single_store_installation()
    {
        var seller = new RecordingSellerAccess();
        var reader = new RecordingEffectiveAccess([]);

        var error = await Assert.ThrowsAsync<SemanticException>(
            () => Create(sellerAccess: seller, reader: reader, edition: Edition(ToobaEdition.SingleStore),
                    tenant: Tenant(TenantStatus.Active, "store-alpha"))
                .RequireSellerOwnerAsync(Context(), ShippingAdminPermissions.View, CancellationToken.None));

        Assert.Equal(ShippingErrorCodes.AuthoringOwnerScopeUnavailable, error.Error.Code);
        // Before seller resolution and before any permission read: no forged Marketplace context.
        Assert.Equal(0, seller.Calls);
        Assert.Equal(0, reader.Calls);
    }

    [Theory]
    [InlineData("unset-edition")]
    [InlineData("null-edition")]
    [InlineData("marketplace-with-tenant")]
    public async Task Seller_route_keeps_the_canonical_context_failure_for_unresolved_or_inconsistent_context(
        string scenario)
    {
        var edition = scenario switch
        {
            "unset-edition" => Edition(ToobaEdition.Unset),
            "null-edition" => new StubCurrentEdition(null),
            _ => Edition(ToobaEdition.Marketplace),
        };
        var tenant = Tenant(TenantStatus.Active, "store-alpha");
        var seller = new RecordingSellerAccess();
        var reader = new RecordingEffectiveAccess([]);

        var error = await Assert.ThrowsAsync<SemanticException>(
            () => Create(sellerAccess: seller, reader: reader, edition: edition, tenant: tenant)
                .RequireSellerOwnerAsync(Context(), ShippingAdminPermissions.View, CancellationToken.None));

        Assert.Equal(FoundationErrorCodes.AdminAuthorizationUnavailable, error.Error.Code);
        Assert.Equal(0, seller.Calls);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task Seller_route_in_marketplace_with_a_whole_owner_view_grant_returns_Seller()
    {
        var seller = new RecordingSellerAccess(Actor, Seller);
        var reader = new RecordingEffectiveAccess([
            new PlatformPermissionGrant(
                ShippingAdminPermissions.View, PlatformAccessScopeKind.GlobalWithinOwner, null, false),
        ]);
        var admin = new RecordingAdminAuthorizer();

        var scope = await Create(adminAuthorizer: admin, sellerAccess: seller, reader: reader,
                edition: Edition(ToobaEdition.Marketplace), tenant: new StubCurrentTenant(null))
            .RequireSellerOwnerAsync(Context(), ShippingAdminPermissions.View, CancellationToken.None);

        Assert.Equal(ShippingAuthoringOwnerKind.Seller, scope.Kind);
        Assert.Equal(Seller, scope.SellerPartyId);
        Assert.Null(scope.TenantId);

        // The effective read is scoped to the SAME actor and the seller returned by the platform seam.
        var request = Assert.Single(reader.Requests);
        Assert.Equal(Actor, request.ActorUserId);
        Assert.Equal(PlatformAccessOwnerKind.Seller, request.OwnerKind);
        Assert.Equal(Seller, request.OwnerScopeId);

        // The admin gate is not a substitute on the seller route.
        Assert.Equal(0, admin.Calls);
    }

    [Theory]
    [InlineData("no-grant")]
    [InlineData("category-only")]
    [InlineData("product-only")]
    [InlineData("brand-only")]
    [InlineData("warehouse-only")]
    [InlineData("denied-by-ceiling")]
    [InlineData("other-permission")]
    [InlineData("other-seller-grant")]
    public async Task Seller_route_denies_anything_but_a_whole_owner_grant_for_that_permission(string scenario)
    {
        var grants = scenario switch
        {
            "no-grant" => Array.Empty<PlatformPermissionGrant>(),
            "category-only" => [Grant(PlatformAccessScopeKind.Category, Guid.NewGuid())],
            "product-only" => [Grant(PlatformAccessScopeKind.Product, Guid.NewGuid())],
            "brand-only" => [Grant(PlatformAccessScopeKind.Brand, Guid.NewGuid())],
            "warehouse-only" => [Grant(PlatformAccessScopeKind.Warehouse, Guid.NewGuid())],
            "denied-by-ceiling" => [Grant(PlatformAccessScopeKind.GlobalWithinOwner, null, deniedByCeiling: true)],
            "other-permission" => [Grant(PlatformAccessScopeKind.GlobalWithinOwner, null, permissionId: "fulfillment.manage")],
            _ => [Grant(PlatformAccessScopeKind.GlobalWithinOwner, null, permissionId: ShippingAdminPermissions.View)],
        };
        var reader = new RecordingEffectiveAccess(grants);
        if (scenario == "other-seller-grant")
        {
            // A whole-owner grant that belongs to another seller must not authorize this seller scope.
            reader = new RecordingEffectiveAccess(grants)
            {
                Selector = ownerScopeId => ownerScopeId == Seller
                    ? [Grant(PlatformAccessScopeKind.GlobalWithinOwner, null)]
                    : [],
            };
        }

        var seller = scenario == "other-seller-grant"
            ? new RecordingSellerAccess(Actor, OtherSeller)
            : new RecordingSellerAccess(Actor, Seller);

        var error = await Assert.ThrowsAsync<SemanticException>(
            () => Create(sellerAccess: seller, reader: reader, edition: Edition(ToobaEdition.Marketplace),
                    tenant: new StubCurrentTenant(null))
                .RequireSellerOwnerAsync(Context(), ShippingAdminPermissions.View, CancellationToken.None));

        Assert.Equal(ShippingErrorCodes.AuthoringOwnerScopeDenied, error.Error.Code);
        Assert.Single(reader.Requests);
    }

    [Fact]
    public async Task A_view_grant_does_not_authorize_manage_on_the_reusable_gate()
    {
        var reader = new RecordingEffectiveAccess([
            Grant(PlatformAccessScopeKind.GlobalWithinOwner, null, permissionId: ShippingAdminPermissions.View),
        ]);
        var authorizer = Create(
            sellerAccess: new RecordingSellerAccess(Actor, Seller),
            reader: reader,
            edition: Edition(ToobaEdition.Marketplace),
            tenant: new StubCurrentTenant(null));

        // The same view grant the read route uses must NOT authorize the future write permission.
        var error = await Assert.ThrowsAsync<SemanticException>(
            () => authorizer.RequireSellerOwnerAsync(Context(), ShippingAdminPermissions.Manage, CancellationToken.None));

        Assert.Equal(ShippingErrorCodes.AuthoringOwnerScopeDenied, error.Error.Code);
        Assert.Single(reader.Requests);
    }

    [Fact]
    public async Task A_seller_panel_failure_propagates_unchanged_before_the_permission_read()
    {
        var expected = new SemanticException(new SemanticError(FoundationErrorCodes.SellerAuthorizationDenied));
        var seller = new RecordingSellerAccess { Failure = expected };
        var reader = new RecordingEffectiveAccess([]);

        var thrown = await Assert.ThrowsAsync<SemanticException>(
            () => Create(sellerAccess: seller, reader: reader, edition: Edition(ToobaEdition.Marketplace),
                    tenant: new StubCurrentTenant(null))
                .RequireSellerOwnerAsync(Context(), ShippingAdminPermissions.View, CancellationToken.None));

        Assert.Same(expected, thrown);
        Assert.Equal(0, reader.Calls);
    }

    [Theory]
    [InlineData("unavailable")]
    [InlineData("cancelled")]
    public async Task A_permission_reader_failure_is_never_treated_as_allow(string scenario)
    {
        var failure = scenario == "cancelled"
            ? (Exception)new OperationCanceledException("cancelled")
            : new SemanticException(new SemanticError(FoundationErrorCodes.PlatformResolutionFailed));
        var reader = new RecordingEffectiveAccess([]) { Failure = failure };

        var thrown = await Assert.ThrowsAnyAsync<Exception>(
            () => Create(sellerAccess: new RecordingSellerAccess(Actor, Seller), reader: reader,
                    edition: Edition(ToobaEdition.Marketplace), tenant: new StubCurrentTenant(null))
                .RequireSellerOwnerAsync(Context(), ShippingAdminPermissions.View, CancellationToken.None));

        Assert.Same(failure, thrown);
    }

    [Fact]
    public async Task The_seller_permission_read_emits_one_canonical_span_without_identity_tags()
    {
        var spans = new List<Activity>();
        using var listener = CreateListener(spans);
        var correlationId = Guid.NewGuid().ToString("N");
        using var correlationScope = CorrelationIdContext.BeginScope(correlationId);
        using var parent = ToobaTelemetry.ActivitySource.StartActivity("test.parent");
        Assert.NotNull(parent);

        var reader = new RecordingEffectiveAccess([
            Grant(PlatformAccessScopeKind.GlobalWithinOwner, null),
        ]);

        await Create(sellerAccess: new RecordingSellerAccess(Actor, Seller), reader: reader,
                edition: Edition(ToobaEdition.Marketplace), tenant: new StubCurrentTenant(null))
            .RequireSellerOwnerAsync(Context(), ShippingAdminPermissions.View, CancellationToken.None);

        var span = Assert.Single(spans);
        Assert.Equal("module.Shipping.AccessControl.GetEffectivePermissions", span.OperationName);
        Assert.Equal("Shipping", Tag(span, TracingTagNames.ModuleSource));
        Assert.Equal("AccessControl", Tag(span, TracingTagNames.ModuleTarget));
        Assert.Equal("AccessControl", Tag(span, TracingTagNames.Module));
        Assert.Equal("GetEffectivePermissions", Tag(span, TracingTagNames.Operation));
        Assert.Equal(TracingRequestKinds.ModuleCall, Tag(span, TracingTagNames.RequestKind));
        Assert.Equal(correlationId, Tag(span, TracingTagNames.CorrelationId));
        Assert.Equal(ActivityStatusCode.Ok, span.Status);
        Assert.Equal(parent!.SpanId, span.ParentSpanId);
        Assert.Equal(parent.TraceId, span.TraceId);
        Assert.Same(parent, Activity.Current);

        var tagValues = span.TagObjects.Select(tag => tag.Value?.ToString() ?? string.Empty).ToArray();
        Assert.DoesNotContain(tagValues, value => value.Contains(Actor.ToString(), StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(tagValues, value => value.Contains(Seller.ToString(), StringComparison.OrdinalIgnoreCase));
        Assert.Empty(span.Events);
    }

    [Fact]
    public async Task A_permission_reader_failure_records_type_only_on_the_span()
    {
        var spans = new List<Activity>();
        using var listener = CreateListener(spans);
        const string Sentinel = "do-not-leak-this-message";
        var reader = new RecordingEffectiveAccess([])
        {
            Failure = new InvalidOperationException(Sentinel),
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Create(sellerAccess: new RecordingSellerAccess(Actor, Seller), reader: reader,
                    edition: Edition(ToobaEdition.Marketplace), tenant: new StubCurrentTenant(null))
                .RequireSellerOwnerAsync(Context(), ShippingAdminPermissions.View, CancellationToken.None));

        var span = Assert.Single(spans);
        Assert.Equal(typeof(InvalidOperationException).FullName, Tag(span, TracingTagNames.ExceptionType));
        var tagValues = span.TagObjects.Select(tag => tag.Value?.ToString() ?? string.Empty).ToArray();
        Assert.DoesNotContain(tagValues, value => value.Contains(Sentinel, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("X-Tooba-Seller-Party-Id", "01900000-0000-7000-8000-00000000bb02")]
    [InlineData("X-Tooba-Owner-Kind", "Seller")]
    [InlineData("X-Tooba-Tenant", "attacker-tenant")]
    public async Task Request_supplied_owner_mode_or_tenant_cannot_replace_the_resolved_scope(
        string header, string value)
    {
        var seller = new RecordingSellerAccess(Actor, Seller);
        var context = Context();
        context.Request.Headers[header] = value;
        context.Request.QueryString =
            new QueryString("?ownerKind=Seller&sellerId=" + OtherSeller + "&mode=marketplace&tenant=other");

        var scope = await Create(sellerAccess: seller, reader: new RecordingEffectiveAccess([
                Grant(PlatformAccessScopeKind.GlobalWithinOwner, null),
            ]), edition: Edition(ToobaEdition.Marketplace), tenant: new StubCurrentTenant(null))
            .RequireSellerOwnerAsync(context, ShippingAdminPermissions.View, CancellationToken.None);

        // Only the canonical platform seam decides the seller; the browser never does.
        Assert.Equal(Seller, scope.SellerPartyId);
        Assert.NotEqual(OtherSeller, scope.SellerPartyId);
    }

    [Fact]
    public async Task Admin_scope_ignores_request_supplied_owner_mode_and_tenant()
    {
        var context = Context();
        context.Request.Headers["X-Tooba-Edition"] = "Marketplace";
        context.Request.QueryString = new QueryString("?ownerKind=Seller&edition=Marketplace&tenant=attacker");

        var scope = await Create(edition: Edition(ToobaEdition.SingleStore),
                tenant: Tenant(TenantStatus.Active, "store-alpha"))
            .RequireAdminOwnerAsync(context, ShippingAdminPermissions.View, CancellationToken.None);

        Assert.Equal(ShippingAuthoringOwnerKind.Store, scope.Kind);
        Assert.Equal("store-alpha", scope.TenantId);
    }

    [Fact]
    public void The_production_adapter_never_parses_or_trusts_transport_owner_input()
    {
        var source = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "backend",
            "Modules",
            "Shipping",
            "Tooba.Shipping.Endpoints",
            "Admin",
            "ShippingAuthoringOwnerAuthorizer.cs"));

        Assert.DoesNotContain("Request.Headers", source, StringComparison.Ordinal);
        Assert.DoesNotContain("X-Tooba-Seller", source, StringComparison.Ordinal);
        Assert.DoesNotContain("QueryString", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IConfiguration", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SellerPartyId.ToString", source, StringComparison.Ordinal);

        // The real canonical seams and the exact whole-owner grant semantics are used.
        Assert.Contains("ISellerPanelAccess", source, StringComparison.Ordinal);
        Assert.Contains("IPlatformEffectiveAccessReader", source, StringComparison.Ordinal);
        Assert.Contains("PlatformAccessOwnerKind.Seller", source, StringComparison.Ordinal);
        Assert.Contains("PlatformAccessScopeKind.GlobalWithinOwner", source, StringComparison.Ordinal);
        Assert.Contains("DeniedByCeiling", source, StringComparison.Ordinal);
        Assert.Contains("IModuleCallTracer", source, StringComparison.Ordinal);
        Assert.Contains("ShippingErrorCodes.AuthoringOwnerScopeDenied", source, StringComparison.Ordinal);
        Assert.Contains("ShippingErrorCodes.AuthoringOwnerScopeUnavailable", source, StringComparison.Ordinal);

        // The owner scope is always built by its factories; no ad-hoc tenant/seller combination.
        Assert.DoesNotContain("new ShippingAuthoringOwnerScope", source, StringComparison.Ordinal);
        Assert.Contains("ShippingAuthoringOwnerScope.ForStore", source, StringComparison.Ordinal);
        Assert.Contains("ShippingAuthoringOwnerScope.ForMarketplace", source, StringComparison.Ordinal);
        Assert.Contains("ShippingAuthoringOwnerScope.ForSeller", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SingleStore_seller_gate_does_not_use_a_marketplace_shortcut()
    {
        var source = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "backend",
            "Modules",
            "Shipping",
            "Tooba.Shipping.Endpoints",
            "Admin",
            "ShippingAuthoringOwnerAuthorizer.cs"));

        // The 404 is emitted for any edition that is not Marketplace — before the seller seam.
        var marketplaceCheck = source.IndexOf("installation.Edition != ToobaEdition.Marketplace", StringComparison.Ordinal);
        var sellerCall = source.IndexOf("sellerAccess.RequireAuthorizedAsync", StringComparison.Ordinal);
        Assert.True(marketplaceCheck >= 0 && marketplaceCheck < sellerCall);
        Assert.DoesNotContain("ToobaEdition.SingleStore => ShippingAuthoringOwnerScope.ForSeller", source, StringComparison.Ordinal);
    }

    private static ShippingAuthoringOwnerAuthorizer Create(
        IShippingAdminAuthorizer? adminAuthorizer = null,
        ISellerPanelAccess? sellerAccess = null,
        IPlatformEffectiveAccessReader? reader = null,
        ICurrentEdition? edition = null,
        ICurrentTenant? tenant = null) =>
        new(
            adminAuthorizer ?? new RecordingAdminAuthorizer(),
            sellerAccess ?? new RecordingSellerAccess(),
            reader ?? new RecordingEffectiveAccess([]),
            edition ?? Edition(ToobaEdition.SingleStore),
            tenant ?? Tenant(TenantStatus.Active, "store-alpha"),
            new ModuleCallTracer());

    private static PlatformPermissionGrant Grant(
        PlatformAccessScopeKind scope,
        Guid? resourceId,
        bool deniedByCeiling = false,
        string permissionId = ShippingAdminPermissions.View) =>
        new(permissionId, scope, resourceId, deniedByCeiling);

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

    private static DefaultHttpContext Context() => new()
    {
        Request = { Method = "GET", Path = "/v1/admin/shipping-services/authoring-owner" },
    };

    private static ActivityListener CreateListener(List<Activity> sink)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ToobaTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.OperationName.StartsWith("module.Shipping.AccessControl.", StringComparison.Ordinal))
                {
                    lock (sink)
                    {
                        sink.Add(activity);
                    }
                }
            },
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    private static string? Tag(Activity activity, string name) =>
        activity.TagObjects.SingleOrDefault(tag => tag.Key == name).Value?.ToString();

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    private sealed class RecordingAdminAuthorizer : IShippingAdminAuthorizer
    {
        public List<string> Permissions { get; } = [];

        public int Calls => Permissions.Count;

        public Task<Guid> RequireAuthorizedAsync(
            HttpContext context, string permission, CancellationToken cancellationToken)
        {
            Permissions.Add(permission);
            return Task.FromResult(Actor);
        }
    }

    private sealed class RecordingSellerAccess(Guid actor, Guid seller) : ISellerPanelAccess
    {
        public RecordingSellerAccess() : this(Actor, Seller)
        {
        }

        public int Calls { get; private set; }

        public Exception? Failure { get; init; }

        public Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
            HttpRequest request, CancellationToken cancellationToken)
        {
            Calls++;
            return Failure is null
                ? Task.FromResult((actor, seller))
                : Task.FromException<(Guid, Guid)>(Failure);
        }
    }

    private sealed class RecordingEffectiveAccess(IReadOnlyList<PlatformPermissionGrant> grants)
        : IPlatformEffectiveAccessReader
    {
        public int Calls => Requests.Count;

        public List<EffectiveRequest> Requests { get; } = [];

        public Exception? Failure { get; init; }

        public Task<IReadOnlyList<PlatformPermissionGrant>> GetEffectivePermissionsAsync(
            Guid userId,
            PlatformAccessOwnerKind ownerKind,
            Guid? ownerScopeId,
            CancellationToken cancellationToken)
        {
            Requests.Add(new EffectiveRequest(userId, ownerKind, ownerScopeId));
            if (Failure is not null)
            {
                return Task.FromException<IReadOnlyList<PlatformPermissionGrant>>(Failure);
            }

            return Task.FromResult(GrantsFor(ownerScopeId));
        }

        /// <summary>Optional per-owner grant selector; defaults to the same grants for every owner.</summary>
        public Func<Guid?, IReadOnlyList<PlatformPermissionGrant>>? Selector { get; init; }

        private IReadOnlyList<PlatformPermissionGrant> GrantsFor(Guid? ownerScopeId) =>
            Selector is null ? grants : Selector(ownerScopeId);

        public sealed record EffectiveRequest(
            Guid ActorUserId,
            PlatformAccessOwnerKind OwnerKind,
            Guid? OwnerScopeId);
    }

    private sealed class StubCurrentEdition(EditionContext? current) : ICurrentEdition
    {
        public EditionContext? Current { get; } = current;
    }

    private sealed class StubCurrentTenant(TenantContext? current) : ICurrentTenant
    {
        public TenantContext? Current { get; } = current;
    }
}
