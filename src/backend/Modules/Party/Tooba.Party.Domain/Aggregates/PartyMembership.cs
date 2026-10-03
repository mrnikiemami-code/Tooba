using Tooba.BuildingBlocks;
using Tooba.Party.Domain.Enums;
using Tooba.Party.Domain.Events;

namespace Tooba.Party.Domain.Aggregates;

/// <summary>
/// عضویت User در Party/سازمان. Membership برابر Authorization نیست و ستون Role نهایی ندارد.
/// </summary>
public sealed class PartyMembership : IHasDomainEvents
{
    private readonly DomainEventCollector _domainEvents = new();

    /// <summary>
    /// شناسهٔ پایدار عضویت.
    /// </summary>
    public Guid MembershipId { get; init; }

    /// <summary>
    /// اصل ورود مبهم.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Party/سازمان مقصد عضویت.
    /// </summary>
    public Guid PartyId { get; init; }

    /// <summary>
    /// وضعیت عضویت در منبع حقیقت Party.
    /// </summary>
    public MembershipStatus Status { get; set; }

    /// <summary>
    /// رابطهٔ کسب‌وکار (مثلاً member). مجوز view/edit اینجا ذخیره نمی‌شود.
    /// </summary>
    public string RelationCode { get; init; } = MembershipRelationCodes.Member;

    /// <summary>
    /// زمان ایجاد عضویت.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.Events;

    /// <summary>
    /// عضویت فعال می‌سازد و رویداد تصویرسازی مجوز را صف می‌کند، بدون تماس شبکه SpiceDB.
    /// </summary>
    public static PartyMembership Establish(Guid userId, Guid partyId, string relationCode, DateTimeOffset now)
    {
        if (userId == Guid.Empty || partyId == Guid.Empty)
        {
            throw new ArgumentException("UserId و PartyId عضویت نباید تهی باشند.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(relationCode);
        var membership = new PartyMembership
        {
            MembershipId = UuidV7.New(),
            UserId = userId,
            PartyId = partyId,
            Status = MembershipStatus.Active,
            RelationCode = relationCode.Trim().ToLowerInvariant(),
            CreatedAt = now,
        };
        membership._domainEvents.Add(new PartyMembershipEstablishedDomainEvent(membership));
        return membership;
    }

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();
}
