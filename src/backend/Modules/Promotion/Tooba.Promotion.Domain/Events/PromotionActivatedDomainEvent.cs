using Tooba.BuildingBlocks;

namespace Tooba.Promotion.Domain.Events;

/// <summary>
/// فعال‌سازی پروموشن.
/// </summary>
public sealed class PromotionActivatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را می‌سازد.
    /// </summary>
    public PromotionActivatedDomainEvent(Guid promotionId)
    {
        PromotionId = promotionId;
        Metadata = EventMetadataFactory.ForDomain("promotion.activated.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// پروموشن.
    /// </summary>
    public Guid PromotionId { get; }
}
