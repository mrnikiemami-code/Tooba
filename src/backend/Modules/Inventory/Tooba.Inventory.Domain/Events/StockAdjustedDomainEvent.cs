using Tooba.BuildingBlocks;
using Tooba.Inventory.Domain.ValueObjects;

namespace Tooba.Inventory.Domain.Events;

/// <summary>
/// رویداد اصلاح موجودی. قیمت را عوض نمی‌کند.
/// </summary>
public sealed class StockAdjustedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد اصلاح را می‌سازد.
    /// </summary>
    public StockAdjustedDomainEvent(Guid stockItemId, Guid offerId, StockAdjustmentKind kind, decimal delta, string reason)
    {
        StockItemId = stockItemId;
        OfferId = offerId;
        Kind = kind;
        Delta = delta;
        Reason = reason;
        Metadata = EventMetadataFactory.ForDomain("inventory.adjusted.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// موقعیت اصلاح‌شده.
    /// </summary>
    public Guid StockItemId { get; }

    /// <summary>
    /// Offer هدف.
    /// </summary>
    public Guid OfferId { get; }

    /// <summary>
    /// گونهٔ اصلاح.
    /// </summary>
    public StockAdjustmentKind Kind { get; }

    /// <summary>
    /// تغییر OnHand.
    /// </summary>
    public decimal Delta { get; }

    /// <summary>
    /// دلیل عملیاتی.
    /// </summary>
    public string Reason { get; }
}
