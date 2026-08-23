using Tooba.BuildingBlocks;

namespace Tooba.Inventory.Infrastructure.Events;

/// <summary>
/// قرارداد Integration اصلاح موجودی. قیمت داخل payload نیست.
/// </summary>
public sealed class InventoryAdjustedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار type map.
    /// </summary>
    public const string EventTypeName = "inventory.adjusted.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// موقعیت اصلاح‌شده.
    /// </summary>
    public Guid StockItemId { get; set; }

    /// <summary>
    /// Offer هدف.
    /// </summary>
    public Guid OfferId { get; set; }

    /// <summary>
    /// تغییر OnHand.
    /// </summary>
    public int Delta { get; set; }
}

/// <summary>
/// قرارداد رزرو موجودی. سبد خرید ساخته نمی‌شود.
/// </summary>
public sealed class InventoryReservedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار type map.
    /// </summary>
    public const string EventTypeName = "inventory.reserved.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// رزرو ایجادشده.
    /// </summary>
    public Guid ReservationId { get; set; }

    /// <summary>
    /// Offer.
    /// </summary>
    public Guid OfferId { get; set; }

    /// <summary>
    /// مقدار قفل‌شده.
    /// </summary>
    public int Quantity { get; set; }
}

/// <summary>
/// قرارداد آزادسازی رزرو.
/// </summary>
public sealed class InventoryReleasedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار type map.
    /// </summary>
    public const string EventTypeName = "inventory.released.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// رزرو آزادشده.
    /// </summary>
    public Guid ReservationId { get; set; }

    /// <summary>
    /// Offer.
    /// </summary>
    public Guid OfferId { get; set; }

    /// <summary>
    /// مقدار برگشتی.
    /// </summary>
    public int Quantity { get; set; }
}

/// <summary>
/// قرارداد مصرف رزرو.
/// </summary>
public sealed class InventoryReservationConsumedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار type map.
    /// </summary>
    public const string EventTypeName = "inventory.reservation_consumed.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// رزرو مصرف‌شده.
    /// </summary>
    public Guid ReservationId { get; set; }

    /// <summary>
    /// Offer.
    /// </summary>
    public Guid OfferId { get; set; }

    /// <summary>
    /// مقدار کسرشده از OnHand.
    /// </summary>
    public int Quantity { get; set; }
}

/// <summary>
/// قرارداد تغییر موجودی قابل‌مشاهده. به‌تنهایی قابل‌خرید بودن نیست.
/// </summary>
public sealed class InventoryAvailabilityChangedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار type map.
    /// </summary>
    public const string EventTypeName = "inventory.availability_changed.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// موقعیت.
    /// </summary>
    public Guid StockItemId { get; set; }

    /// <summary>
    /// Offer.
    /// </summary>
    public Guid OfferId { get; set; }

    /// <summary>
    /// موجودی قابل‌فروش مشتق.
    /// </summary>
    public int Available { get; set; }
}
