using Tooba.BuildingBlocks;
using Tooba.Party.Domain.Enums;
using Tooba.Party.Domain.Events;

namespace Tooba.Party.Domain.Aggregates;

/// <summary>
/// قابلیت تجاری جدا از نوع Party و جدا از مجوز SpiceDB.
/// </summary>
public sealed class PartyCapability
{
    /// <summary>
    /// کلید ردیف قابلیت.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// سازمان مالک قابلیت. FK به Identity نیست.
    /// </summary>
    public Guid PartyId { get; init; }

    /// <summary>
    /// کد گسترش‌پذیر مثل seller/agency؛ ماتریس مجوز نیست.
    /// </summary>
    public string CapabilityCode { get; init; } = "";

    /// <summary>
    /// زمان اعطای قابلیت.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}
