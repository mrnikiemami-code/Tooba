using Tooba.BuildingBlocks;

namespace Tooba.Cart.Infrastructure.Events;

/// <summary>
/// قرارداد Integration ایجاد سبد. سفارش ساخته نمی‌شود.
/// </summary>
public sealed class CartCreatedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار type map.
    /// </summary>
    public const string EventTypeName = "cart.created.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// سبد ایجادشده.
    /// </summary>
    public Guid CartId { get; set; }
}

/// <summary>
/// قرارداد افزودن خط Offer.
/// </summary>
public sealed class CartLineAddedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار type map.
    /// </summary>
    public const string EventTypeName = "cart.line_added.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// سبد.
    /// </summary>
    public Guid CartId { get; set; }

    /// <summary>
    /// خط.
    /// </summary>
    public Guid LineId { get; set; }

    /// <summary>
    /// Offer.
    /// </summary>
    public Guid OfferId { get; set; }

    /// <summary>
    /// تعداد.
    /// </summary>
    public int Quantity { get; set; }
}

/// <summary>
/// قرارداد تغییر تعداد خط.
/// </summary>
public sealed class CartLineChangedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار type map.
    /// </summary>
    public const string EventTypeName = "cart.line_changed.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// سبد.
    /// </summary>
    public Guid CartId { get; set; }

    /// <summary>
    /// خط.
    /// </summary>
    public Guid LineId { get; set; }

    /// <summary>
    /// Offer.
    /// </summary>
    public Guid OfferId { get; set; }

    /// <summary>
    /// تعداد جدید.
    /// </summary>
    public int Quantity { get; set; }
}

/// <summary>
/// قرارداد حذف خط.
/// </summary>
public sealed class CartLineRemovedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار type map.
    /// </summary>
    public const string EventTypeName = "cart.line_removed.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// سبد.
    /// </summary>
    public Guid CartId { get; set; }

    /// <summary>
    /// خط.
    /// </summary>
    public Guid LineId { get; set; }

    /// <summary>
    /// Offer.
    /// </summary>
    public Guid OfferId { get; set; }
}

/// <summary>
/// قرارداد انقضای سبد.
/// </summary>
public sealed class CartExpiredIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار type map.
    /// </summary>
    public const string EventTypeName = "cart.expired.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// سبد منقضی یا رهاشده.
    /// </summary>
    public Guid CartId { get; set; }
}

/// <summary>
/// قرارداد درز تبدیل بدون Order.
/// </summary>
public sealed class CartConvertedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار type map.
    /// </summary>
    public const string EventTypeName = "cart.converted.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// سبد تبدیل‌شده.
    /// </summary>
    public Guid CartId { get; set; }
}
