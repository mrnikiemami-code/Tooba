using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.ReturnRequests.Models;


/// <summary>
/// TB-279: one owning DELIVERED slice of a line, with the exact quantity this case may still claim from it. A slice is
/// identified by its owning line and the instant it was delivered (the Fulfillment contract publishes no slice id), and
/// the quantity is the slice quantity MINUS everything already durably allocated on that same slice.
/// </summary>
public sealed record ReturnLineDeliveryAvailability(
    string SliceKey,
    DateTimeOffset DeliveredAt,
    decimal SliceQuantity,
    decimal AvailableQuantity,
    bool IsInWindow,
    /// <summary>
    /// TB-279-R1 (F5): true when an ACTIVE case that predates per-delivery allocation occupies this line and more than
    /// one delivery could be its origin. The line's leftover capacity is then unknowable, so a NEW claim is refused with
    /// <c>return.allocation.legacy_ambiguous</c> instead of binding units to a guessed slice.
    /// </summary>
    bool LegacyAmbiguous = false);

/// <summary>
/// eligibility خط مرجوعی برای یک OrderLine.
/// <para>
/// TB-278: <see cref="ReturnableUntil"/> is the LAST instant this exact line may still be returned, derived from the
/// line's own delivery slice(s) and its own stored policy window — never from the aggregate order status or the
/// browser clock. <see cref="BlockerCode"/> is the ONE stable reason this line cannot fund a new customer selection
/// (<c>null</c> means it can). It is a read fact only: the owning evaluator still computes it, and the same rules
/// remain final for the write path.
/// </para>
/// <para>
/// TB-279: <see cref="Slices"/> publishes the per-delivery availability the same calculation used, so creation can bind
/// a new claim to the exact slices instead of re-deriving (or over-claiming) capacity in a second place.
/// </para>
/// </summary>
public sealed record ReturnLineEligibility(
    Guid OrderLineId,
    decimal DeliveredQuantity,
    decimal AlreadyReturnedQuantity,
    decimal RemainingReturnableQuantity,
    DateTimeOffset? ReturnableUntil = null,
    string? BlockerCode = null,
    IReadOnlyList<ReturnLineDeliveryAvailability>? Slices = null);
