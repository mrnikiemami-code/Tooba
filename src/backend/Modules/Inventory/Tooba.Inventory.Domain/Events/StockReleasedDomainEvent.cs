using Tooba.BuildingBlocks;
using Tooba.Inventory.Domain.ValueObjects;

namespace Tooba.Inventory.Domain.Events;

/// <summary>
/// رویداد آزادسازی رزرو.
/// </summary>
public sealed class StockReleasedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد آزادسازی را می‌سازد.
    /// </summary>
    public StockReleasedDomainEvent(Guid reservationId, Guid stockItemId, Guid offerId, decimal quantity)
    {
        ReservationId = reservationId;
        StockItemId = stockItemId;
        OfferId = offerId;
        Quantity = quantity;
        Metadata = EventMetadataFactory.ForDomain("inventory.released.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// رزرو آزادشده.
    /// </summary>
    public Guid ReservationId { get; }

    /// <summary>
    /// موقعیت.
    /// </summary>
    public Guid StockItemId { get; }

    /// <summary>
    /// Offer.
    /// </summary>
    public Guid OfferId { get; }

    /// <summary>
    /// مقدار برگشتی.
    /// </summary>
    public decimal Quantity { get; }
}
