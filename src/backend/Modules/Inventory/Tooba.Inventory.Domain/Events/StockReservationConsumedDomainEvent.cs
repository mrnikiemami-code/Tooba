using Tooba.BuildingBlocks;
using Tooba.Inventory.Domain.ValueObjects;

namespace Tooba.Inventory.Domain.Events;

/// <summary>
/// رویداد مصرف رزرو.
/// </summary>
public sealed class StockReservationConsumedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد مصرف را می‌سازد.
    /// </summary>
    public StockReservationConsumedDomainEvent(Guid reservationId, Guid stockItemId, Guid offerId, decimal quantity)
    {
        ReservationId = reservationId;
        StockItemId = stockItemId;
        OfferId = offerId;
        Quantity = quantity;
        Metadata = EventMetadataFactory.ForDomain("inventory.reservation_consumed.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// رزرو مصرف‌شده.
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
    /// مقدار کسرشده از OnHand.
    /// </summary>
    public decimal Quantity { get; }
}
