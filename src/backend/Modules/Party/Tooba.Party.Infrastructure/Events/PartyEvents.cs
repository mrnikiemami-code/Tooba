using Tooba.BuildingBlocks;

namespace Tooba.Party.Infrastructure.Events;

/// <summary>
/// قرارداد Integration برقراری عضویت. نوشتن SpiceDB اینجا انجام نمی‌شود.
/// </summary>
public sealed class PartyMembershipEstablishedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// نام پایدار type map.
    /// </summary>
    public const string EventTypeName = "party.membership_established.v1";

    /// <inheritdoc />
    [System.Text.Json.Serialization.JsonIgnore]
    public EventMetadata Metadata { get; set; } = EventMetadataFactory.ForDomain(EventTypeName);

    /// <summary>
    /// عضویت منبع حقیقت Party.
    /// </summary>
    public Guid MembershipId { get; set; }

    /// <summary>
    /// اصل ورود برای تصویرسازی رابطه.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Party مقصد؛ شناسهٔ سازمان در SpiceDB از همین مقدار ساخته می‌شود.
    /// </summary>
    public Guid PartyId { get; set; }

    /// <summary>
    /// رابطهٔ کسب‌وکار برای ردیابی؛ مجوز را schema تعیین می‌کند.
    /// </summary>
    public string RelationCode { get; set; } = "";
}
