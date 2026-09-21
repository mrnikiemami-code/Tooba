using Tooba.BuildingBlocks;
using Tooba.Inventory.Domain.ValueObjects;
using Tooba.Inventory.Domain.Events;

namespace Tooba.Inventory.Domain.Aggregates;

/// <summary>
/// موقعیت موجودی یک Offer در یک محل. حقیقت موجودی روی Product نیست.
/// </summary>
public sealed class StockPosition : IHasDomainEvents
{
    private readonly DomainEventCollector _domainEvents = new();

    /// <summary>
    /// شناسهٔ پایدار موقعیت.
    /// </summary>
    public Guid StockItemId { get; init; }

    /// <summary>
    /// Offer هدف. FK به schema offer نیست.
    /// </summary>
    public Guid OfferId { get; init; }

    /// <summary>
    /// گونهٔ Catalog برای جستجوی توصیفی. کلید فروشنده نیست.
    /// </summary>
    public Guid CatalogVariantId { get; init; }

    /// <summary>
    /// محل نگهداری.
    /// </summary>
    public Guid LocationId { get; init; }

    /// <summary>
    /// موجودی فیزیکی. اعشار شناور نیست.
    /// </summary>
    public decimal OnHand { get; private set; }

    /// <summary>
    /// مقدار قفل‌شده برای رزروهای Held.
    /// </summary>
    public decimal Reserved { get; private set; }

    /// <summary>
    /// زمان ایجاد UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// زمان آخرین تغییر UTC.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// موجودی قابل‌فروش مشتق؛ ستون جدا ذخیره نمی‌شود تا منحرف نشود.
    /// </summary>
    public decimal Available => OnHand - Reserved;

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.Events;

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// موقعیت خالی می‌سازد. قابل‌خرید بودن Offer را اعلام نمی‌کند.
    /// </summary>
    public static StockPosition Open(
        Guid stockItemId,
        Guid offerId,
        Guid catalogVariantId,
        Guid locationId,
        DateTimeOffset now)
    {
        if (stockItemId == Guid.Empty || offerId == Guid.Empty || catalogVariantId == Guid.Empty || locationId == Guid.Empty)
        {
            throw new InvalidOperationException("inventory.position.ids_required");
        }

        var position = new StockPosition
        {
            StockItemId = stockItemId,
            OfferId = offerId,
            CatalogVariantId = catalogVariantId,
            LocationId = locationId,
            OnHand = 0,
            Reserved = 0,
            CreatedAt = now,
            UpdatedAt = now,
        };
        position._domainEvents.Add(new StockAvailabilityChangedDomainEvent(position.StockItemId, position.OfferId, 0, 0, 0));
        return position;
    }

    /// <summary>
    /// پس از به‌روزرسانی اتمی پایگاه، مقادیر خوانده‌شده را با رویداد هم‌تراز می‌کند.
    /// </summary>
    public void SyncQuantities(decimal onHand, decimal reserved, DateTimeOffset now)
    {
        EnsureLegal(onHand, reserved);
        OnHand = onHand;
        Reserved = reserved;
        UpdatedAt = now;
        _domainEvents.Add(new StockAvailabilityChangedDomainEvent(StockItemId, OfferId, OnHand, Reserved, Available));
    }

    /// <summary>
    /// رویداد اصلاح را ثبت می‌کند. مقدار را جداگانه با SQL اتمی عوض می‌کنند.
    /// </summary>
    public void RecordAdjustment(StockAdjustmentKind kind, decimal delta, string reason)
    {
        _domainEvents.Add(new StockAdjustedDomainEvent(StockItemId, OfferId, kind, delta, reason));
    }

    /// <summary>
    /// رویداد رزرو موفق را ثبت می‌کند.
    /// </summary>
    public void RecordReserved(Guid reservationId, decimal quantity)
    {
        _domainEvents.Add(new StockReservedDomainEvent(reservationId, StockItemId, OfferId, quantity));
    }

    /// <summary>
    /// رویداد آزادسازی رزرو را ثبت می‌کند.
    /// </summary>
    public void RecordReleased(Guid reservationId, decimal quantity)
    {
        _domainEvents.Add(new StockReleasedDomainEvent(reservationId, StockItemId, OfferId, quantity));
    }

    /// <summary>
    /// رویداد مصرف رزرو را ثبت می‌کند.
    /// </summary>
    public void RecordConsumed(Guid reservationId, decimal quantity)
    {
        _domainEvents.Add(new StockReservationConsumedDomainEvent(reservationId, StockItemId, OfferId, quantity));
    }

    /// <summary>
    /// حالت غیرممکن موجودی را رد می‌کند.
    /// </summary>
    public static void EnsureLegal(decimal onHand, decimal reserved)
    {
        if (onHand < 0 || reserved < 0 || reserved > onHand)
        {
            throw new InvalidOperationException("inventory.position.quantity_invalid");
        }
    }
}
