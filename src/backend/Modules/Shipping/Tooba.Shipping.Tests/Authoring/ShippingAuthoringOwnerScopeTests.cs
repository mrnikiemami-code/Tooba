using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Queries;
using Tooba.Shipping.Contracts.Errors;
using Xunit;

namespace Tooba.Shipping.Tests.Authoring;

/// <summary>
/// TB-SHIPPING-ADMIN-AUTHORIZED-OWNER-SCOPE-189 — the internal immutable owner-scope model and the
/// ONE shared read that both routes dispatch.
/// </summary>
public sealed class ShippingAuthoringOwnerScopeTests
{
    private static readonly Guid Seller = Guid.Parse("01900000-0000-7000-8000-00000000bb01");

    [Fact]
    public void Store_scope_carries_the_stable_tenant_and_no_seller()
    {
        var scope = ShippingAuthoringOwnerScope.ForStore("  store-alpha  ");

        Assert.Equal(ShippingAuthoringOwnerKind.Store, scope.Kind);
        Assert.Equal("store-alpha", scope.TenantId);
        Assert.Null(scope.SellerPartyId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Store_scope_requires_the_actual_tenant_id(string tenantId)
    {
        Assert.Throws<ArgumentException>(() => ShippingAuthoringOwnerScope.ForStore(tenantId));
    }

    [Fact]
    public void Marketplace_scope_has_neither_tenant_nor_seller()
    {
        var scope = ShippingAuthoringOwnerScope.ForMarketplace();

        Assert.Equal(ShippingAuthoringOwnerKind.Marketplace, scope.Kind);
        Assert.Null(scope.TenantId);
        Assert.Null(scope.SellerPartyId);
    }

    [Fact]
    public void Seller_scope_requires_a_non_empty_party_and_has_no_tenant()
    {
        var scope = ShippingAuthoringOwnerScope.ForSeller(Seller);

        Assert.Equal(ShippingAuthoringOwnerKind.Seller, scope.Kind);
        Assert.Equal(Seller, scope.SellerPartyId);
        Assert.Null(scope.TenantId);

        Assert.Throws<ArgumentException>(() => ShippingAuthoringOwnerScope.ForSeller(Guid.Empty));
    }

    [Fact]
    public void The_model_has_no_actor_mode_label_or_mutable_combination()
    {
        // Owner identity is deliberately separate from the actor: no ActorUserId and no mode/label.
        var properties = typeof(ShippingAuthoringOwnerScope)
            .GetProperties()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "Kind", "SellerPartyId", "TenantId" }, properties);
        Assert.All(typeof(ShippingAuthoringOwnerScope).GetProperties(), property => Assert.False(property.CanWrite));
    }

    [Fact]
    public async Task The_shared_handler_projects_each_scope_without_reading_foreign_state()
    {
        var handler = new GetShippingAuthoringOwnerHandler();

        var store = await handler.Handle(
            new GetShippingAuthoringOwnerQuery(ShippingAuthoringOwnerScope.ForStore("store-alpha")),
            CancellationToken.None);
        var marketplace = await handler.Handle(
            new GetShippingAuthoringOwnerQuery(ShippingAuthoringOwnerScope.ForMarketplace()),
            CancellationToken.None);
        var seller = await handler.Handle(
            new GetShippingAuthoringOwnerQuery(ShippingAuthoringOwnerScope.ForSeller(Seller)),
            CancellationToken.None);

        Assert.True(store.IsSuccess);
        Assert.Equal(new ShippingAuthoringOwnerView("Store", null), store.Value);
        Assert.Equal(new ShippingAuthoringOwnerView("Marketplace", null), marketplace.Value);
        Assert.Equal(new ShippingAuthoringOwnerView("Seller", Seller), seller.Value);
    }

    [Fact]
    public async Task The_compact_view_never_exposes_the_tenant_or_a_foreign_seller()
    {
        var handler = new GetShippingAuthoringOwnerHandler();
        var view = (await handler.Handle(
            new GetShippingAuthoringOwnerQuery(ShippingAuthoringOwnerScope.ForStore("store-alpha")),
            CancellationToken.None)).Value;

        // Response shape is only owner kind (+ seller id for the authorized seller); the internal
        // stable tenant id and every connection/security detail stay server-side.
        var serialized = System.Text.Json.JsonSerializer.Serialize(view);
        Assert.Equal("{\"OwnerKind\":\"Store\",\"SellerPartyId\":null}", serialized);
        Assert.DoesNotContain("store-alpha", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("Tenant", serialized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_missing_scope_is_a_programming_error_not_a_default_owner()
    {
        var handler = new GetShippingAuthoringOwnerHandler();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.Handle(new GetShippingAuthoringOwnerQuery(null!), CancellationToken.None));
    }

    [Fact]
    public async Task Caller_cancellation_is_preserved()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var handler = new GetShippingAuthoringOwnerHandler();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => handler.Handle(
                new GetShippingAuthoringOwnerQuery(ShippingAuthoringOwnerScope.ForMarketplace()),
                cancellation.Token));
    }

    [Fact]
    public void The_owner_scope_is_not_a_reusable_credential()
    {
        // A scope is data about a resolved owner, not an authorization token: no verification,
        // signature, expiry, serialization contract or public setters exist to be replayed.
        var publicSurface = typeof(ShippingAuthoringOwnerScope)
            .GetMethods()
            .Where(method => method.DeclaringType == typeof(ShippingAuthoringOwnerScope))
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .ToArray();
        Assert.DoesNotContain(publicSurface, name => name.Contains("Verify", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(publicSurface, name => name.Contains("Token", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(publicSurface, name => name.Contains("Grant", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(
            new[] { "ForMarketplace", "ForSeller", "ForStore" },
            publicSurface.OrderBy(name => name, StringComparer.Ordinal).ToArray());
        Assert.Null(typeof(ShippingAuthoringOwnerScope).GetProperty(nameof(ShippingAuthoringOwnerScope.TenantId))!.SetMethod);
    }

    [Fact]
    public void Owner_scope_codes_are_declared_with_the_shipping_keyspace()
    {
        Assert.Equal("shipping.authoring.owner_scope_denied", ShippingErrorCodes.AuthoringOwnerScopeDenied);
        Assert.Equal("shipping.authoring.owner_scope_unavailable", ShippingErrorCodes.AuthoringOwnerScopeUnavailable);
        Assert.NotEqual(ShippingErrorCodes.AuthoringOwnerScopeDenied, ShippingErrorCodes.AuthoringConfigurationUnavailable);
    }
}
