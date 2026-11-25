namespace Tooba.Offer.Domain.Aggregates;

/// <summary>
/// Append-only record of one Offer-owned change. It carries no cross-module identities beyond an optional
/// actor user id, and old/new values are recorded only when the owning command can read them reliably.
/// </summary>
public sealed class OfferHistoryEntry
{
    /// <summary>Stable history entry identifier.</summary>
    public Guid OfferHistoryEntryId { get; init; }

    /// <summary>Offer the change belongs to.</summary>
    public Guid OfferId { get; init; }

    /// <summary>Server instant at which the change was applied.</summary>
    public DateTimeOffset OccurredAt { get; init; }

    /// <summary>Stable change kind vocabulary owned by the Offer module.</summary>
    public string ChangeKind { get; init; } = string.Empty;

    /// <summary>Actor user identifier when the owning command carried one; otherwise <c>null</c>.</summary>
    public Guid? ActorUserId { get; init; }

    /// <summary>Human-readable summary of the recorded change.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>Reliable old value summary, or <c>null</c> when it was not available.</summary>
    public string? OldValue { get; init; }

    /// <summary>Reliable new value summary, or <c>null</c> when it was not available.</summary>
    public string? NewValue { get; init; }
}
