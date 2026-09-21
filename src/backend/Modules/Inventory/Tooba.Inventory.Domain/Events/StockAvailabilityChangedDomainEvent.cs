using Tooba.BuildingBlocks;
using Tooba.Inventory.Domain.ValueObjects;

namespace Tooba.Inventory.Domain.Events;

/// <summary>
/// رویداد تغییر موجودی قابل‌مشاهده. به‌تنهایی قابل‌خرید بودن نیست.
/// </summary>
public sealed class StockAvailabilityChangedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد تغییر موجودی را می‌سازد.
    /// </summary>
    public StockAvailabilityChangedDomainEvent(Guid stockItemId, Guid offerId, decimal onHand, decimal reserved, decimal available)
    {
        StockItemId = stockItemId;
        OfferId = offerId;
        OnHand = onHand;
        Reserved = reserved;
        Available = available;
        Metadata = EventMetadataFactory.ForDomain("inventory.availability_changed.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// موقعیت.
    /// </summary>
    public Guid StockItemId { get; }

    /// <summary>
    /// Offer.
    /// </summary>
    public Guid OfferId { get; }

    /// <summary>
    /// موجودی فیزیکی.
    /// </summary>
    public decimal OnHand { get; }

    /// <summary>
    /// مقدار رزرو.
    /// </summary>
    public decimal Reserved { get; }

    /// <summary>
    /// موجودی قابل‌فروش مشتق.
    /// </summary>
    public decimal Available { get; }
}
