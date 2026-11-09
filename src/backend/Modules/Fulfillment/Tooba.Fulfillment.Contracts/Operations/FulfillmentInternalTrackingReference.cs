namespace Tooba.Fulfillment.Contracts.Operations;

/// <summary>
/// TB-257: the ONE owning form of the internal (non-provider) parcel tracking reference.
/// </summary>
/// <remarks>
/// <para>
/// The reference is derived from the WHOLE shipment identity, never from a time-ordered prefix. The previous
/// suggestion used the first eight hex characters of the UUIDv7 shipment id, i.e. one 32-bit bucket that only
/// changes about every 65 seconds; two parcels created in the same minute therefore received the SAME suggested
/// code, the second assignment collided with the real database unique index
/// (<c>ix_shipments_tracking_reference</c>) and the operator saw a duplicate error that regenerating could not
/// fix — the very same deterministic value came back.
/// </para>
/// <para>
/// This is an internal operator reference, not a postal/carrier waybill. Provider identifiers issued by a carrier
/// adapter are stored separately and are never overwritten or re-derived here.
/// </para>
/// <para>
/// <see cref="ForShipment"/> is deterministic for its target: re-submitting the same parcel yields the same
/// candidate (so a retry, double click or lost response reconciles the existing assignment instead of replacing a
/// real code), while a different parcel always gets a different candidate. An allocation attempt that still
/// collides — for example because somebody typed this parcel's candidate onto another parcel — moves to
/// <c>attempt + 1</c>, bounded by <see cref="MaxAllocationAttempts"/>.
/// </para>
/// </remarks>
public static class FulfillmentInternalTrackingReference
{
    /// <summary>Internal reference prefix that keeps operator codes distinct from provider waybills.</summary>
    public const string Prefix = "TRK-";

    /// <summary>Column ceiling of <c>shipments.tracking_reference</c>.</summary>
    public const int MaxLength = 128;

    /// <summary>Bounded fresh-candidate attempts during automatic allocation.</summary>
    public const int MaxAllocationAttempts = 5;

    /// <summary>
    /// Candidate reference for a parcel. <paramref name="attempt"/> zero is the canonical suggestion shown to the
    /// operator; higher attempts are the bounded collision retries of the owning assignment command.
    /// </summary>
    public static string ForShipment(Guid shipmentId, int attempt = 0)
    {
        var core = shipmentId.ToString("N").ToUpperInvariant();
        var candidate = attempt <= 0
            ? $"{Prefix}{core}"
            : $"{Prefix}{core}-{attempt.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        return candidate.Length <= MaxLength ? candidate : candidate[..MaxLength];
    }

    /// <summary>True when the supplied value is exactly the canonical suggestion of this parcel.</summary>
    public static bool IsCanonicalSuggestion(string? candidate, Guid shipmentId)
        => !string.IsNullOrWhiteSpace(candidate)
           && string.Equals(candidate.Trim(), ForShipment(shipmentId), StringComparison.OrdinalIgnoreCase);
}
