using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks.Security;
using Tooba.Cart.Contracts;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Fulfillment.Endpoints.Customer;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Order.Contracts.Fulfillment;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-TMAR-FULFILLMENT-HOST-EVACUATION-001 — برابری معنایی احراز مشتری/مهمان پس از تخلیه Host.
/// </summary>
public sealed class CustomerFulfillmentAuthorizerParityTests
{
    private static readonly Guid GuestActor = StorefrontGuestActor.ActorId;

    [Fact]
    public async Task Authenticated_owner_is_allowed()
    {
        var owner = Guid.NewGuid();
        var checkoutId = Guid.NewGuid();
        var authorizer = Build(owner, ownership: Snapshot(checkoutId, owner, Guid.NewGuid()), cartOwned: false);
        Assert.Null(await authorizer.EnsureCanViewCheckoutAsync(Handler(owner), checkoutId, CancellationToken.None));
    }

    [Fact]
    public async Task Wrong_authenticated_owner_is_denied()
    {
        var other = Guid.NewGuid();
        var outsider = Guid.NewGuid();
        var checkoutId = Guid.NewGuid();
        var authorizer = Build(other, ownership: Snapshot(checkoutId, other, Guid.NewGuid()), cartOwned: true);
        var error = await authorizer.EnsureCanViewCheckoutAsync(Handler(outsider), checkoutId, CancellationToken.None);
        Assert.NotNull(error);
        Assert.Equal(FulfillmentErrorCodes.CustomerOrderMissing, error!.Code);
    }

    [Fact]
    public async Task Valid_guest_secret_is_allowed()
    {
        var checkoutId = Guid.NewGuid();
        var authorizer = Build(null, ownership: Snapshot(checkoutId, GuestActor, Guid.NewGuid()), cartOwned: true);
        Assert.Null(await authorizer.EnsureCanViewCheckoutAsync(Handler(null, "guest-secret"), checkoutId, CancellationToken.None));
    }

    [Fact]
    public async Task Invalid_guest_secret_is_denied()
    {
        var checkoutId = Guid.NewGuid();
        var authorizer = Build(null, ownership: Snapshot(checkoutId, GuestActor, Guid.NewGuid()), cartOwned: false);
        var error = await authorizer.EnsureCanViewCheckoutAsync(Handler(null, "bad-secret"), checkoutId, CancellationToken.None);
        Assert.NotNull(error);
        Assert.Equal(FulfillmentErrorCodes.CustomerOrderMissing, error!.Code);
    }

    [Fact]
    public async Task Missing_actor_and_missing_guest_secret_returns_actor_missing()
    {
        var checkoutId = Guid.NewGuid();
        var authorizer = Build(null, ownership: Snapshot(checkoutId, GuestActor, Guid.NewGuid()), cartOwned: false, environmentName: "Production");
        var error = await authorizer.EnsureCanViewCheckoutAsync(Handler(null), checkoutId, CancellationToken.None);
        Assert.NotNull(error);
        Assert.Equal(FulfillmentErrorCodes.CustomerActorMissing, error!.Code);
    }

    [Fact]
    public async Task Missing_checkout_returns_order_missing()
    {
        var actor = Guid.NewGuid();
        var authorizer = Build(actor, ownership: null, cartOwned: false);
        var error = await authorizer.EnsureCanViewCheckoutAsync(Handler(actor), Guid.NewGuid(), CancellationToken.None);
        Assert.NotNull(error);
        Assert.Equal(FulfillmentErrorCodes.CustomerOrderMissing, error!.Code);
    }

    [Fact]
    public async Task Dev_actor_header_is_honored_in_testing_environment()
    {
        var devActor = Guid.NewGuid();
        var checkoutId = Guid.NewGuid();
        var authorizer = Build(null, ownership: Snapshot(checkoutId, devActor, Guid.NewGuid()), cartOwned: false);
        var context = Handler(null);
        context.Request.Headers["X-Tooba-Dev-Actor-User-Id"] = devActor.ToString("D");
        Assert.Null(await authorizer.EnsureCanViewCheckoutAsync(context, checkoutId, CancellationToken.None));
    }

    [Fact]
    public async Task Guest_placed_by_actor_never_counts_as_actor_ownership()
    {
        var actor = GuestActor;
        var checkoutId = Guid.NewGuid();
        var authorizer = Build(actor, ownership: Snapshot(checkoutId, GuestActor, Guid.NewGuid()), cartOwned: false);
        var error = await authorizer.EnsureCanViewCheckoutAsync(Handler(actor), checkoutId, CancellationToken.None);
        Assert.NotNull(error);
        Assert.Equal(FulfillmentErrorCodes.CustomerOrderMissing, error!.Code);
    }

    private static CustomerCheckoutOwnershipSnapshot Snapshot(Guid checkoutId, Guid placedBy, Guid cartId) =>
        new(checkoutId, placedBy, cartId);

    private static FulfillmentCustomerAuthorizer Build(
        Guid? actorUserId,
        CustomerCheckoutOwnershipSnapshot? ownership,
        bool cartOwned,
        string environmentName = "Testing") =>
        new(
            new StubCurrentUser(actorUserId),
            new StubEnvironment(environmentName),
            new StubOwnership(ownership),
            new StubCartGateway(cartOwned));

    private static HttpContext Handler(Guid? actorUserId, string? guestSecret = null)
    {
        var context = new DefaultHttpContext();
        if (actorUserId is { } actor)
        {
            context.Request.Headers["X-Tooba-Dev-Actor-User-Id"] = actor.ToString("D");
        }

        if (guestSecret is not null)
        {
            context.Request.Headers["X-Tooba-Guest-Secret"] = guestSecret;
        }

        return context;
    }

    private sealed class StubCurrentUser(Guid? userId) : ICurrentAuthenticatedUser
    {
        public bool IsAuthenticated => false;

        public Guid? UserId => userId;
    }

    private sealed class StubEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;

        public string ApplicationName { get; set; } = "tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class StubOwnership(CustomerCheckoutOwnershipSnapshot? snapshot) : ICustomerCheckoutOwnershipReader
    {
        public Task<CustomerCheckoutOwnershipSnapshot?> GetAsync(Guid checkoutId, CancellationToken cancellationToken) =>
            Task.FromResult(snapshot);
    }

    private sealed class StubCartGateway(bool owned) : ICartQueryGateway
    {
        public Task<CartSnapshot?> GetCartAsync(Guid cartId, CartAccess access, CancellationToken cancellationToken) =>
            Task.FromResult(owned ? EmptyCart(cartId) : null);

        private static CartSnapshot EmptyCart(Guid cartId) => new(
            cartId,
            CartStatus.Active,
            CartAccessKind.Guest,
            null,
            "IR",
            "IRR",
            SalesChannel.Marketplace,
            null,
            CartConversionIntent.None,
            1,
            []);
    }
}
