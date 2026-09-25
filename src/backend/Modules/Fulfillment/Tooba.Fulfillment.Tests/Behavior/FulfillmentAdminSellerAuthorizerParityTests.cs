using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Security;
using Tooba.Fulfillment.Application.Commands.SellerMutateFulfillment;
using Tooba.Fulfillment.Endpoints.Admin;
using Tooba.Fulfillment.Endpoints.Seller;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-TMAR-FULFILLMENT-HOST-EVACUATION-001 — برابری معنایی احراز مدیر و پروجکشن order.handle فروشنده.
/// </summary>
public sealed class FulfillmentAdminSellerAuthorizerParityTests
{
    [Fact]
    public async Task Admin_authorizer_delegates_to_generic_platform_seam()
    {
        var actor = Guid.NewGuid();
        var authorizer = new FulfillmentAdminAuthorizer(new StubAdminAccess(actor));
        var result = await authorizer.RequireAuthorizedAsync(Handler(), CancellationToken.None);
        Assert.Equal(actor, result);
    }

    [Fact]
    public async Task Admin_authorizer_does_not_swallow_seam_failures()
    {
        var authorizer = new FulfillmentAdminAuthorizer(new StubAdminAccess(new PlatformHttpException(503, "x", "admin.tenant.missing")));
        var ex = await Assert.ThrowsAsync<PlatformHttpException>(
            () => authorizer.RequireAuthorizedAsync(Handler(), CancellationToken.None));
        Assert.Equal(503, ex.StatusCode);
        Assert.Equal("admin.tenant.missing", ex.ErrorCode);
    }

    [Fact]
    public async Task Seller_authorizer_delegates_actor_and_seller_party()
    {
        var actor = Guid.NewGuid();
        var seller = Guid.NewGuid();
        var authorizer = new FulfillmentSellerAuthorizer(new StubSellerAccess((actor, seller)), new StubEffectiveAccess([]));
        var resolved = await authorizer.RequireAuthorizedAsync(Handler(), CancellationToken.None);
        Assert.Equal(actor, resolved.ActorUserId);
        Assert.Equal(seller, resolved.SellerPartyId);
    }

    [Fact]
    public async Task Seller_global_within_owner_projects_to_has_global()
    {
        var input = await Handle(Grant("order.handle", PlatformAccessScopeKind.GlobalWithinOwner, null, false));
        Assert.True(input.HasGlobalWithinOwner);
        Assert.Empty(input.AllowedCategoryIds);
    }

    [Fact]
    public async Task Seller_category_scopes_project_distinct_ids()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var input = await Handle(
            Grant("order.handle", PlatformAccessScopeKind.Category, a, false),
            Grant("order.handle", PlatformAccessScopeKind.Category, b, false),
            Grant("order.handle", PlatformAccessScopeKind.Category, a, false));
        Assert.False(input.HasGlobalWithinOwner);
        Assert.Equal(2, input.AllowedCategoryIds.Count);
        Assert.Contains(a, input.AllowedCategoryIds);
        Assert.Contains(b, input.AllowedCategoryIds);
    }

    [Fact]
    public async Task Seller_denied_by_ceiling_is_excluded()
    {
        var hidden = Guid.NewGuid();
        var input = await Handle(
            Grant("order.handle", PlatformAccessScopeKind.GlobalWithinOwner, null, true),
            Grant("order.handle", PlatformAccessScopeKind.Category, hidden, true));
        Assert.False(input.HasGlobalWithinOwner);
        Assert.Empty(input.AllowedCategoryIds);
    }

    [Fact]
    public async Task Seller_unrelated_permission_is_ignored()
    {
        var input = await Handle(Grant("order.view", PlatformAccessScopeKind.GlobalWithinOwner, null, false));
        Assert.False(input.HasGlobalWithinOwner);
        Assert.Empty(input.AllowedCategoryIds);
    }

    [Fact]
    public async Task Seller_category_without_resource_is_ignored()
    {
        var input = await Handle(Grant("order.handle", PlatformAccessScopeKind.Category, null, false));
        Assert.Empty(input.AllowedCategoryIds);
    }

    private static Task<SellerHandlePermissionInput> Handle(params PlatformPermissionGrant[] grants) =>
        new FulfillmentSellerAuthorizer(
                new StubSellerAccess((Guid.NewGuid(), Guid.NewGuid())),
                new StubEffectiveAccess(grants))
            .GetHandlePermissionAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

    private static PlatformPermissionGrant Grant(
        string permissionId,
        PlatformAccessScopeKind scope,
        Guid? resourceId,
        bool denied) => new(permissionId, scope, resourceId, denied);

    private static HttpContext Handler() => new DefaultHttpContext();

    private sealed class StubAdminAccess : IAdminPanelAccess
    {
        private readonly object _result;

        public StubAdminAccess(Guid actor) => _result = actor;

        public StubAdminAccess(PlatformHttpException failure) => _result = failure;

        public Task<Guid> RequireAuthorizedAsync(HttpRequest request, CancellationToken cancellationToken) =>
            _result is PlatformHttpException failure
                ? Task.FromException<Guid>(failure)
                : Task.FromResult((Guid)_result);
    }

    private sealed class StubSellerAccess(( Guid Actor, Guid Seller) pair) : ISellerPanelAccess
    {
        public Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
            HttpRequest request, CancellationToken cancellationToken) =>
            Task.FromResult((pair.Actor, pair.Seller));
    }

    private sealed class StubEffectiveAccess(IReadOnlyList<PlatformPermissionGrant> grants) : IPlatformEffectiveAccessReader
    {
        public Task<IReadOnlyList<PlatformPermissionGrant>> GetEffectivePermissionsAsync(
            Guid userId,
            PlatformAccessOwnerKind ownerKind,
            Guid? ownerScopeId,
            CancellationToken cancellationToken) =>
            Task.FromResult(grants);
    }
}
