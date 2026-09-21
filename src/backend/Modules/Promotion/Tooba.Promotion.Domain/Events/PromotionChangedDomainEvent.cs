using Tooba.BuildingBlocks;

namespace Tooba.Promotion.Domain.Events;

/// <summary>
/// تغییر تعریف. تصویر سفارش را عوض نمی‌کند.
/// </summary>
public sealed class PromotionChangedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را می‌سازد.
    /// </summary>
    public PromotionChangedDomainEvent(Guid promotionId)
    {
        PromotionId = promotionId;
        Metadata = EventMetadataFactory.ForDomain("promotion.changed.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// پروموشن.
    /// </summary>
    public Guid PromotionId { get; }
}
