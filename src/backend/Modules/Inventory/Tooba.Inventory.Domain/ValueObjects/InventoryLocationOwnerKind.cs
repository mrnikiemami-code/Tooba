namespace Tooba.Inventory.Domain.ValueObjects;

/// <summary>Explicit owner of a storage location. Shared/null eligibility is forbidden.</summary>
public enum InventoryLocationOwnerKind
{
    /// <summary>Trusted SingleStore tenant or platform-owned store location.</summary>
    Store = 0,

    /// <summary>Marketplace-operated location.</summary>
    Marketplace = 1,

    /// <summary>Seller-owned location; SellerPartyId is mandatory.</summary>
    Seller = 2,
}
