using Tooba.BuildingBlocks;
using Tooba.Party.Domain.Aggregates;

namespace Tooba.Party.Domain.Events;

/// <summary>
/// واقعیت دامنهٔ برقراری عضویت. تماس SpiceDB نیست؛ ترجمه به Integration فقط از Outbox است.
/// </summary>
public sealed class PartyMembershipEstablishedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را از عضویت persistشونده می‌سازد.
    /// </summary>
    public PartyMembershipEstablishedDomainEvent(PartyMembership membership)
    {
        ArgumentNullException.ThrowIfNull(membership);
        MembershipId = membership.MembershipId;
        UserId = membership.UserId;
        PartyId = membership.PartyId;
        RelationCode = membership.RelationCode;
        Metadata = EventMetadataFactory.ForDomain("party.membership_established.domain");
    }

    /// <summary>
    /// عضویت منبع حقیقت.
    /// </summary>
    public Guid MembershipId { get; }

    /// <summary>
    /// اصل ورود برای تصویرسازی بعدی.
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Party مقصد تصویرسازی.
    /// </summary>
    public Guid PartyId { get; }

    /// <summary>
    /// رابطهٔ کسب‌وکار؛ handler مجوز را از schema SpiceDB می‌گیرد نه از این رشته به‌عنوان Role.
    /// </summary>
    public string RelationCode { get; }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }
}
