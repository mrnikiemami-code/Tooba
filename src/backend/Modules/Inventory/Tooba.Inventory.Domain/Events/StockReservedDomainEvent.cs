using Tooba.BuildingBlocks;
using Tooba.Inventory.Domain.ValueObjects;

namespace Tooba.Inventory.Domain.Events;

/// <summary>
/// رویداد رزرو موفق.
/// </summary>
public sealed class StockReservedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد رزرو را می‌سازد.
    /// </summary>
    public StockReservedDomainEvent(Guid reservationId, Guid stockItemId, Guid offerId, decimal quantity)
    {
        ReservationId = reservationId;
        StockItemId = stockItemId;
        OfferId = offerId;
        Quantity = quantity;
        Metadata = EventMetadataFactory.ForDomain("inventory.reserved.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// رزرو ایجادشده.
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
    /// مقدار قفل‌شده.
    /// </summary>
    public decimal Quantity { get; }
}
