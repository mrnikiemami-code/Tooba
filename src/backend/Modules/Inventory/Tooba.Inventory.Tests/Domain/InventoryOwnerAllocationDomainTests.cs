using Tooba.BuildingBlocks;
using Tooba.Inventory.Domain.Aggregates;
using Tooba.Inventory.Domain.ValueObjects;
using Xunit;

namespace Tooba.Inventory.Tests.Domain;

public sealed class InventoryOwnerAllocationDomainTests
{
    [Fact]
    public void Seller_owned_location_requires_a_seller_and_store_rejects_one()
    {
        var seller = Guid.NewGuid();
        var owned = InventoryLocation.Create(Guid.NewGuid(), "S-A", "Seller A", DateTimeOffset.UtcNow,
            InventoryLocationOwnerKind.Seller, seller);
        Assert.Equal(InventoryLocationOwnerKind.Seller, owned.OwnerKind);
        Assert.Equal(seller, owned.SellerPartyId);
        Assert.Throws<ContractOperationException>(() => InventoryLocation.Create(
            Guid.NewGuid(), "S-B", "Seller B", DateTimeOffset.UtcNow, InventoryLocationOwnerKind.Seller, null));
        Assert.Throws<ContractOperationException>(() => InventoryLocation.Create(
            Guid.NewGuid(), "S-C", "Seller C", DateTimeOffset.UtcNow, InventoryLocationOwnerKind.Store, seller));
    }

    [Fact]
    public void Allocation_cannot_be_below_held_and_stops_new_sale_availability()
    {
        var allocation = OfferAllocation.Create(Guid.NewGuid(), 20m, Guid.NewGuid(), DateTimeOffset.UtcNow);
        allocation.Sync(20m, 15m, Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Equal(5m, allocation.Quantity - allocation.Held);
        Assert.Throws<ArgumentOutOfRangeException>(() => allocation.SetQuantity(14m, Guid.NewGuid(), DateTimeOffset.UtcNow));
        allocation.SetQuantity(15m, Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Equal(0m, allocation.Quantity - allocation.Held);
    }
}
