using Tooba.BuildingBlocks;

namespace Tooba.Promotion.Domain.Events;

/// <summary>
/// انقضای پروموشن.
/// </summary>
public sealed class PromotionExpiredDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را می‌سازد.
    /// </summary>
    public PromotionExpiredDomainEvent(Guid promotionId)
    {
        PromotionId = promotionId;
        Metadata = EventMetadataFactory.ForDomain("promotion.expired.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// پروموشن.
    /// </summary>
    public Guid PromotionId { get; }
}
