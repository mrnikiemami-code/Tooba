using Tooba.BuildingBlocks;

namespace Tooba.Identity.Domain.Events;

/// <summary>
/// واقعیت داخلی ثبت User. هر Domain Event قرارداد خارجی نیست.
/// </summary>
public sealed class UserRegisteredDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد ثبت را با UserId پایدار می‌سازد.
    /// </summary>
    public UserRegisteredDomainEvent(Guid userId)
    {
        UserId = userId;
        Metadata = EventMetadataFactory.ForDomain("identity.user_registered.domain");
    }

    /// <summary>
    /// User تازه ایجادشده.
    /// </summary>
    public Guid UserId { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}
