using Tooba.BuildingBlocks;
using Tooba.Party.Domain.Enums;
using Tooba.Party.Domain.Events;

namespace Tooba.Party.Domain.Aggregates;

/// <summary>
/// پیوند صریح UserId مبهم Identity به Party. ستون PartyId روی UserAccount گذاشته نمی‌شود و FK بین‌ماژولی نیست.
/// </summary>
public sealed class UserPartyLink
{
    /// <summary>
    /// کلید پیوند.
    /// </summary>
    public Guid LinkId { get; init; }

    /// <summary>
    /// اصل ورود به‌صورت Guid مات. جدول identity.users اینجا join نمی‌شود.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Party مقصد پیوند، معمولاً Person.
    /// </summary>
    public Guid PartyId { get; init; }

    /// <summary>
    /// زمان ایجاد پیوند.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// پیوند User به Party را در مالکیت همین ماژول می‌سازد.
    /// </summary>
    public static UserPartyLink Bind(Guid userId, Guid partyId, DateTimeOffset now)
    {
        if (userId == Guid.Empty || partyId == Guid.Empty)
        {
            throw new ArgumentException("UserId و PartyId باید پایدار و غیرتهی باشند.");
        }

        return new UserPartyLink
        {
            LinkId = UuidV7.New(),
            UserId = userId,
            PartyId = partyId,
            CreatedAt = now,
        };
    }
}
