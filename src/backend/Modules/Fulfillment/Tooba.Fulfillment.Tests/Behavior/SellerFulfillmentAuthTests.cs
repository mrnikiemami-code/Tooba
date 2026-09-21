using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Ports;
using Tooba.Fulfillment.Contracts;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Fulfillment.Infrastructure.Directories;
using Tooba.Order.Contracts.Fulfillment;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

public sealed class SellerFulfillmentAuthTests
{
    [Fact]
    public async Task Unauthorized_seller_without_handle_permission_is_denied()
    {
        var auth = new SellerFulfillmentAuthorizer(new StubOrderAuthReader(null));
        var result = await auth.EnsureCanMutateAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new SellerOrderHandlePermissionSnapshot(false, []),
            CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal(FulfillmentErrorCodes.SellerOrderHandleDenied, result.Errors[0].Code);
    }

    [Fact]
    public async Task Global_within_owner_permission_is_preserved()
    {
        var auth = new SellerFulfillmentAuthorizer(new StubOrderAuthReader(null));
        var result = await auth.EnsureCanMutateAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new SellerOrderHandlePermissionSnapshot(true, []),
            CancellationToken.None);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Category_scoped_all_lines_authorization_is_preserved()
    {
        var categoryA = Guid.NewGuid();
        var categoryB = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var sellerPartyId = Guid.NewGuid();
        var snapshot = new SellerOrderAuthSnapshot(
            sellerOrderId,
            sellerPartyId,
            [
                new SellerOrderAuthLine(Guid.NewGuid(), Guid.NewGuid(), categoryA),
                new SellerOrderAuthLine(Guid.NewGuid(), Guid.NewGuid(), categoryB),
            ]);
        var auth = new SellerFulfillmentAuthorizer(new StubOrderAuthReader(snapshot));

        var allowed = await auth.EnsureCanMutateAsync(
            sellerPartyId,
            sellerOrderId,
            new SellerOrderHandlePermissionSnapshot(false, [categoryA, categoryB]),
            CancellationToken.None);
        Assert.True(allowed.IsSuccess);

        var denied = await auth.EnsureCanMutateAsync(
            sellerPartyId,
            sellerOrderId,
            new SellerOrderHandlePermissionSnapshot(false, [categoryA]),
            CancellationToken.None);
        Assert.True(denied.IsFailure);
        Assert.Equal(FulfillmentErrorCodes.SellerOrderHandleScopeDenied, denied.Errors[0].Code);
    }

    [Fact]
    public async Task Missing_seller_order_maps_to_not_found_code()
    {
        var auth = new SellerFulfillmentAuthorizer(new StubOrderAuthReader(null));
        var result = await auth.EnsureCanMutateAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new SellerOrderHandlePermissionSnapshot(false, [Guid.NewGuid()]),
            CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal(FulfillmentErrorCodes.SellerOrderMissing, result.Errors[0].Code);
    }

    private sealed class StubOrderAuthReader : ISellerOrderAuthReader
    {
        private readonly SellerOrderAuthSnapshot? _snapshot;

        public StubOrderAuthReader(SellerOrderAuthSnapshot? snapshot) => _snapshot = snapshot;

        public Task<SellerOrderAuthSnapshot?> GetForSellerAsync(
            Guid sellerOrderId,
            Guid sellerPartyId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_snapshot);
    }
}
