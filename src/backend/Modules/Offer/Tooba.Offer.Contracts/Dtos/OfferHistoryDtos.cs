namespace Tooba.Offer.Contracts.Dtos;

/// <summary>One recorded Offer change for the authorized history read.</summary>
/// <remarks>Kind is the stable presentation key. Summary is legacy/invariant audit data, not localized display text; render the kind through the consumer's existing resource catalog.</remarks>
public sealed record OfferHistoryItem(
    Guid OfferId,
    DateTimeOffset OccurredAt,
    string Kind,
    Guid? ActorUserId,
    string Summary,
    string? OldValue,
    string? NewValue,
    Guid? EventId = null,
    Guid? TargetId = null,
    Guid? LocationId = null);

/// <summary>Bounded page of recorded Offer changes ordered by occurrence descending.</summary>
public sealed record OfferHistoryPage(
    IReadOnlyList<OfferHistoryItem> Items,
    int TotalCount,
    int Page,
    int PageSize);
