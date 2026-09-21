using Tooba.BuildingBlocks;

namespace Tooba.Promotion.Domain.Events;

/// <summary>
/// ایجاد پروموشن.
/// </summary>
public sealed class PromotionCreatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را می‌سازد.
    /// </summary>
    public PromotionCreatedDomainEvent(Guid promotionId)
    {
        PromotionId = promotionId;
        Metadata = EventMetadataFactory.ForDomain("promotion.created.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// پروموشن.
    /// </summary>
    public Guid PromotionId { get; }
}
