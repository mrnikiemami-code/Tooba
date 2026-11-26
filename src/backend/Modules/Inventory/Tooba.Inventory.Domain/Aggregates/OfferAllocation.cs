namespace Tooba.Inventory.Domain.Aggregates;

/// <summary>
/// Inventory-owned explicit new-sale allocation for one Offer. Quantity includes held units;
/// new-sale availability is Quantity - Held. No stock quantity is stored on Offer.
/// </summary>
public sealed class OfferAllocation
{
    /// <summary>Offer identity; one shared allocation across all eligible positions.</summary>
    public Guid OfferId { get; init; }

    /// <summary>Explicit offered quantity, including held units.</summary>
    public decimal Quantity { get; private set; }

    /// <summary>Units currently held by new-sale reservations.</summary>
    public decimal Held { get; private set; }

    /// <summary>Owner CAS revision.</summary>
    public Guid Revision { get; private set; }

    /// <summary>Last change instant.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Creates an allocation row.</summary>
    public static OfferAllocation Create(Guid offerId, decimal quantity, Guid revision, DateTimeOffset now)
    {
        if (offerId == Guid.Empty || quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        return new OfferAllocation { OfferId = offerId, Quantity = quantity, Held = 0, Revision = revision, UpdatedAt = now };
    }

    /// <summary>Sets the explicit offered quantity no lower than protected holds.</summary>
    public void SetQuantity(decimal quantity, Guid revision, DateTimeOffset now)
    {
        if (quantity < Held)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        Quantity = quantity;
        Revision = revision;
        UpdatedAt = now;
    }

    /// <summary>Rehydrates runtime state after an atomic SQL update.</summary>
    public void Sync(decimal quantity, decimal held, Guid revision, DateTimeOffset now)
    {
        if (quantity < 0 || held < 0 || held > quantity)
            throw new ArgumentOutOfRangeException(nameof(held));
        Quantity = quantity;
        Held = held;
        Revision = revision;
        UpdatedAt = now;
    }
}
