using Xunit;

namespace Tooba.Offer.Tests.Architecture;

/// <summary>P1-A frozen wire and owner-boundary source guards.</summary>
public sealed class OfferBackendOwnerWireGuardTests
{
    [Fact]
    public void Frozen_wire_contract_is_exposed_without_frontend_scope_change()
    {
        var dto = Read("src/backend/Modules/Offer/Tooba.Offer.Contracts/Dtos/SellerOfferPanelDtos.cs");
        Assert.Contains("OfferRevisions", dto, StringComparison.Ordinal);
        Assert.Contains("CanEditOfferedQuantity", dto, StringComparison.Ordinal);
        Assert.Contains("PriceRevision", dto, StringComparison.Ordinal);
        Assert.Contains("OfferedQuantity", dto, StringComparison.Ordinal);
        var sellerEndpoints = Read("src/backend/Modules/Offer/Tooba.Offer.Endpoints/Seller/OfferSellerEndpoints.cs");
        var adminEndpoints = Read("src/backend/Modules/Offer/Tooba.Offer.Endpoints/Admin/OfferAdminEndpoints.cs");
        Assert.Contains("/offers/{offerId:guid}/offered-quantity", sellerEndpoints, StringComparison.Ordinal);
        Assert.Contains("/offers/{offerId:guid}/offered-quantity", adminEndpoints, StringComparison.Ordinal);
    }

    [Fact]
    public void Owner_mutations_do_not_save_a_second_offer_version_or_fabricate_a_default_warehouse()
    {
        var price = Read("src/backend/Modules/Offer/Tooba.Offer.Application/Offers/Commands/SetOfferPrice/SetOfferPriceCommand.cs");
        var inventory = Read("src/backend/Modules/Offer/Tooba.Offer.Application/Offers/Commands/SetOfferInventory/SetOfferInventoryCommand.cs");
        Assert.DoesNotContain("store.SaveChangesAsync", price, StringComparison.Ordinal);
        Assert.DoesNotContain("history.AppendAsync", price, StringComparison.Ordinal);
        Assert.DoesNotContain("store.SaveChangesAsync", inventory, StringComparison.Ordinal);
        Assert.DoesNotContain("history.AppendAsync", inventory, StringComparison.Ordinal);
        var sellerWrite = Read("src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/Directories/InventoryDirectory.SellerWrite.cs");
        Assert.DoesNotContain("SELLER-DEFAULT", sellerWrite, StringComparison.Ordinal);
        Assert.Contains("OwnerKind == InventoryLocationOwnerKind.Seller", sellerWrite, StringComparison.Ordinal);
    }

    [Fact]
    public void Allocation_reservation_is_shared_by_offer_and_uses_owner_sql()
    {
        var directory = Read("src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/Directories/InventoryDirectory.cs");
        Assert.Contains("x.Quantity - x.Held >= quantity", directory, StringComparison.Ordinal);
        Assert.Contains("SetProperty(x => x.Held, x => x.Held + quantity)", directory, StringComparison.Ordinal);
        Assert.Contains("SetProperty(x => x.Quantity, x => x.Quantity - reservation.Quantity)", directory, StringComparison.Ordinal);
    }

    private static string Read(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null
               && !Directory.Exists(Path.Combine(dir.FullName, ".git"))
               && !File.Exists(Path.Combine(dir.FullName, ".git"))
               && !File.Exists(Path.Combine(dir.FullName, "docs", "ai", "TOOBA-PIPELINE-PROTOCOL.md"))) dir = dir.Parent;
        return File.ReadAllText(Path.Combine(dir!.FullName, relative));
    }
}
