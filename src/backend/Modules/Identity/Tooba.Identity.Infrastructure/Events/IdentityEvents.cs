using Tooba.BuildingBlocks;

namespace Tooba.Identity.Infrastructure.Events;

/// <summary>
/// قرارداد Integration ثبت User. ورود موفق از این مسیر خارج نمی‌شود.
/// </summary>
public sealed class UserRegisteredIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار type map.
    /// </summary>
    public const string EventTypeName = "identity.user_registered.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// اصل پایدار؛ PartyId نیست.
    /// </summary>
    public Guid UserId { get; set; }
}
