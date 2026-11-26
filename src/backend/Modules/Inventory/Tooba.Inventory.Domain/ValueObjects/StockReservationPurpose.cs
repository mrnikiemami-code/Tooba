namespace Tooba.Inventory.Domain.ValueObjects;

/// <summary>Allocation accounting purpose, assigned only by an Inventory-owned use case.</summary>
public enum StockReservationPurpose
{
    /// <summary>A new commercial sale; holds and consumption use the seller's allocation.</summary>
    NewSale = 0,
    /// <summary>Supply of an existing paid order, not a new sale authorization.</summary>
    PaidOrderRecovery = 1,
    /// <summary>Replacement of an already sold item; physical stock only, never a second sale.</summary>
    DamagedReplacement = 2,
}
