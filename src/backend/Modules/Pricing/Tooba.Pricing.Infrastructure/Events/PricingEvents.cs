using Tooba.BuildingBlocks;

namespace Tooba.Pricing.Infrastructure.Events;

/// <summary>
/// قرارداد Integration ایجاد قیمت نوشته‌شده. مالیات و FX داخل payload نیست.
/// </summary>
public sealed class PriceCreatedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار type map.
    /// </summary>
    public const string EventTypeName = "pricing.price_created.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// قیمت ایجادشده.
    /// </summary>
    public Guid PriceId { get; set; }

    /// <summary>
    /// Offer هدف.
    /// </summary>
    public Guid OfferId { get; set; }
}

/// <summary>
/// قرارداد فعال‌سازی قیمت پایه. قابل‌خرید بودن را اعلام نمی‌کند.
/// </summary>
public sealed class PriceActivatedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار type map.
    /// </summary>
    public const string EventTypeName = "pricing.price_activated.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// قیمت فعال‌شده.
    /// </summary>
    public Guid PriceId { get; set; }
}

/// <summary>
/// قرارداد تغییر مبلغ نوشته‌شده. نرخ تبدیل‌شده FX نیست.
/// </summary>
public sealed class PriceChangedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار type map.
    /// </summary>
    public const string EventTypeName = "pricing.price_changed.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// قیمت تغییر یافته.
    /// </summary>
    public Guid PriceId { get; set; }
}

/// <summary>
/// قرارداد خروج قیمت از انتخاب.
/// </summary>
public sealed class PriceExpiredIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار type map.
    /// </summary>
    public const string EventTypeName = "pricing.price_expired.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// قیمت منقضی.
    /// </summary>
    public Guid PriceId { get; set; }
}
