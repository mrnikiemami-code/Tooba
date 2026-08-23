using Tooba.BuildingBlocks;
using Tooba.Promotion.Domain;

namespace Tooba.Promotion.Infrastructure.Events;

/// <summary>
/// ایجاد پروموشن.
/// </summary>
public sealed class PromotionCreatedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار.
    /// </summary>
    public const string EventTypeName = "promotion.created.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// پروموشن.
    /// </summary>
    public Guid PromotionId { get; set; }
}

/// <summary>
/// فعال‌سازی پروموشن.
/// </summary>
public sealed class PromotionActivatedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار.
    /// </summary>
    public const string EventTypeName = "promotion.activated.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// پروموشن.
    /// </summary>
    public Guid PromotionId { get; set; }
}

/// <summary>
/// تغییر تعریف.
/// </summary>
public sealed class PromotionChangedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار.
    /// </summary>
    public const string EventTypeName = "promotion.changed.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// پروموشن.
    /// </summary>
    public Guid PromotionId { get; set; }
}

/// <summary>
/// انقضای پروموشن.
/// </summary>
public sealed class PromotionExpiredIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار.
    /// </summary>
    public const string EventTypeName = "promotion.expired.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// پروموشن.
    /// </summary>
    public Guid PromotionId { get; set; }
}
