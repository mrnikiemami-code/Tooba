namespace Tooba.Returns.Application.ReturnRequests.Models;

/// <summary>
/// TB-279: one owning DELIVERED slice as the Fulfillment contract publishes it (a line, a quantity and the instant it
/// was delivered).
/// </summary>
public sealed record ReturnDeliverySliceSource(
    Guid OrderLineId,
    decimal Quantity,
    DateTimeOffset DeliveredAt);

/// <summary>
/// TB-279: the ONE per-delivery allocation arithmetic. Eligibility, creation, correction/capacity release and the
/// read deadline all use these functions, so an old return can never consume a newer delivery's window and two sibling
/// cases can never claim the same slice quantity twice.
/// <para>
/// A slice is identified by its owning line and its delivery instant because the Fulfillment contract publishes no
/// slice id; two deliveries of the same line at the same instant share one deadline and are therefore merged into one
/// honest slice instead of being counted twice.
/// </para>
/// </summary>
public static class ReturnAllocationPolicy
{
    /// <summary>The stable identity of one owning delivered slice.</summary>
    public static string SliceKeyOf(Guid orderLineId, DateTimeOffset deliveredAt) =>
        $"{orderLineId:N}:{deliveredAt.UtcDateTime.Ticks}";

    /// <summary>
    /// Describes every slice of one line with the quantity a NEW claim may still take from it.
    /// <para>
    /// <paramref name="allocatedElsewhere"/> is the sum of durable, still-active allocations of OTHER cases on that same
    /// slice key. <paramref name="legacyUnallocated"/> is the quantity of this line already occupied by active cases
    /// that predate per-delivery allocation; it is never guessed onto a specific slice, so it is subtracted from the
    /// OLDEST in-window slice first (the conservative direction: a legacy return cannot appear to free a newer batch).
    /// </para>
    /// </summary>
    public static IReadOnlyList<ReturnLineDeliveryAvailability> Describe(
        Guid orderLineId,
        IReadOnlyList<ReturnDeliverySliceSource> slices,
        int windowDays,
        DateTimeOffset now,
        IReadOnlyDictionary<string, decimal> allocatedElsewhere,
        decimal legacyUnallocated = 0m)
    {
        ArgumentNullException.ThrowIfNull(slices);
        ArgumentNullException.ThrowIfNull(allocatedElsewhere);

        var days = windowDays > 0 ? windowDays : 7;
        var merged = slices
            .Where(x => x.Quantity > 0m)
            .GroupBy(x => SliceKeyOf(orderLineId, x.DeliveredAt), StringComparer.Ordinal)
            .Select(group => new
            {
                Key = group.Key,
                DeliveredAt = group.Min(x => x.DeliveredAt),
                Quantity = group.Sum(x => x.Quantity),
            })
            .OrderBy(x => x.DeliveredAt)
            .ToArray();

        // TB-279-R1 (F5): a legacy (pre-allocation) active case occupies this line without naming a delivery. When MORE
        // THAN ONE delivery could be its origin, the leftover capacity of each slice is genuinely unknowable, so every
        // slice of the line is flagged ambiguous and a NEW claim is refused instead of guessing an origin.
        var ambiguousLegacy = Math.Max(0m, legacyUnallocated) > 0m
            && merged.Count(x => IsInWindow(x.DeliveredAt, days, now)) > 1;

        var availability = new List<ReturnLineDeliveryAvailability>(merged.Length);
        var remainingLegacy = Math.Max(0m, legacyUnallocated);
        foreach (var slice in merged)
        {
            allocatedElsewhere.TryGetValue(slice.Key, out var allocated);
            var available = Math.Max(0m, slice.Quantity - allocated);
            if (remainingLegacy > 0m)
            {
                var taken = Math.Min(remainingLegacy, available);
                available -= taken;
                remainingLegacy -= taken;
            }

            availability.Add(new ReturnLineDeliveryAvailability(
                slice.Key,
                slice.DeliveredAt,
                slice.Quantity,
                available,
                IsInWindow(slice.DeliveredAt, days, now),
                ambiguousLegacy));
        }

        return availability;
    }

    /// <summary>
    /// TB-279-R1 (F5): true when this line's per-delivery capacity cannot be attributed, so no new claim may bind a
    /// slice. A single-delivery line stays answerable (there is exactly one possible origin).
    /// </summary>
    public static bool HasAmbiguousLegacy(IReadOnlyList<ReturnLineDeliveryAvailability> slices) =>
        slices.Count > 0 && slices.Any(x => x.LegacyAmbiguous);

    /// <summary>True while this slice's own window is still open at the owning server instant.</summary>
    public static bool IsInWindow(DateTimeOffset deliveredAt, int windowDays, DateTimeOffset now) =>
        now <= deliveredAt.AddDays(windowDays > 0 ? windowDays : 7);

    /// <summary>
    /// Binds exactly <paramref name="requested"/> units to eligible slices, OLDEST deadline first, so the newest
    /// delivered batch keeps its full window for later returns. Returns <c>null</c> when the eligible quantity is not
    /// enough — the caller refuses with its own typed code instead of borrowing another delivery's capacity.
    /// </summary>
    public static IReadOnlyList<(string SliceKey, DateTimeOffset DeliveredAt, decimal Quantity)>? Allocate(
        IReadOnlyList<ReturnLineDeliveryAvailability> slices,
        decimal requested)
    {
        ArgumentNullException.ThrowIfNull(slices);
        if (requested <= 0m)
        {
            return null;
        }

        // TB-279-R1 (F5): a line whose leftover capacity cannot be attributed to one delivery (an active legacy claim
        // plus more than one in-window delivery) is never allocated by guessing an origin, even when the summed
        // capacity looks sufficient. The caller answers the typed ambiguity instead.
        if (HasAmbiguousLegacy(slices))
        {
            return null;
        }

        var remaining = requested;
        var allocations = new List<(string, DateTimeOffset, decimal)>();
        foreach (var slice in slices.Where(x => x.IsInWindow).OrderBy(x => x.DeliveredAt))
        {
            if (remaining <= 0m)
            {
                break;
            }

            if (slice.AvailableQuantity <= 0m)
            {
                continue;
            }

            var take = Math.Min(slice.AvailableQuantity, remaining);
            allocations.Add((slice.SliceKey, slice.DeliveredAt, take));
            remaining -= take;
        }

        return remaining > 0m ? null : allocations;
    }
}
