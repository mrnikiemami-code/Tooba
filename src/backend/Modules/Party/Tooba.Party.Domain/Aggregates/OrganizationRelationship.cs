using Tooba.BuildingBlocks;
using Tooba.Party.Domain.Enums;
using Tooba.Party.Domain.Events;

namespace Tooba.Party.Domain.Aggregates;

/// <summary>
/// رابطهٔ typed سازمان‌به‌سازمان برای ساختارهای آینده. قوانین کامل B2B اینجا پیاده نمی‌شود.
/// </summary>
public sealed class OrganizationRelationship
{
    /// <summary>
    /// کلید رابطه.
    /// </summary>
    public Guid RelationshipId { get; init; }

    /// <summary>
    /// Party مبدأ.
    /// </summary>
    public Guid FromPartyId { get; init; }

    /// <summary>
    /// Party مقصد.
    /// </summary>
    public Guid ToPartyId { get; init; }

    /// <summary>
    /// کد گسترش‌پذیر رابطه؛ فهرست بستهٔ SellerOnly نیست.
    /// </summary>
    public string RelationCode { get; init; } = "";

    /// <summary>
    /// وضعیت رابطه در منبع حقیقت Party.
    /// </summary>
    public MembershipStatus Status { get; set; }

    /// <summary>
    /// زمان ایجاد.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// رابطهٔ سازمانی را بدون اجرای workflow فروشنده/آژانس ثبت می‌کند.
    /// </summary>
    public static OrganizationRelationship Connect(Guid fromPartyId, Guid toPartyId, string relationCode, DateTimeOffset now)
    {
        if (fromPartyId == Guid.Empty || toPartyId == Guid.Empty || fromPartyId == toPartyId)
        {
            throw new ArgumentException("رابطهٔ سازمانی باید دو Party متمایز داشته باشد.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(relationCode);
        return new OrganizationRelationship
        {
            RelationshipId = UuidV7.New(),
            FromPartyId = fromPartyId,
            ToPartyId = toPartyId,
            RelationCode = relationCode.Trim().ToLowerInvariant(),
            Status = MembershipStatus.Active,
            CreatedAt = now,
        };
    }
}
