using System.Globalization;

namespace Tooba.Order.Contracts.Shipping;

/// <summary>
/// The reason codes shared by every reader that must decide whether a linked (resend/replacement)
/// parcel really owns its dispatch plan. ONE authority so the dispatch guard and the read
/// projections can never disagree about what "planned" means.
/// </summary>
public static class OrderShipmentDispatchPlanBindingReasonCodes
{
    /// <summary>No plan row exists for the parcel.</summary>
    public const string Missing = "order.shipment_plan.binding_missing_plan";

    /// <summary>The plan row belongs to a different parcel/attempt than the one being dispatched.</summary>
    public const string ParcelMismatch = "order.shipment_plan.binding_parcel_mismatch";

    /// <summary>The plan was confirmed for a different source parcel/decision/hold.</summary>
    public const string SourceMismatch = "order.shipment_plan.binding_source_mismatch";

    /// <summary>The plan's canonical line fingerprint does not match the physical parcel lines.</summary>
    public const string QuantityMismatch = "order.shipment_plan.binding_quantity_mismatch";

    /// <summary>The plan's accepted authoring capture is missing or no longer matches its identity.</summary>
    public const string AcceptedUnverified = "order.shipment_plan.binding_capture_unverified";

    /// <summary>The parcel does not record a method, so the plan's method identity cannot be proven for it.</summary>
    public const string MethodUnverified = "order.shipment_plan.binding_method_unverified";

    /// <summary>The plan's own selected/delivery/key facts are internally inconsistent.</summary>
    public const string Corrupt = "order.shipment_plan.binding_corrupt";
}

/// <summary>
/// The exact identity a linked attempt's plan must carry. A plan is only "for" a parcel when
/// every identity below matches the OWNING unit/parcel/decision/hold and the canonical line
/// fingerprint matches the physical parcel lines.
/// </summary>
public sealed record OrderShipmentDispatchPlanBindingCandidate(
    Guid ShipmentId,
    Guid CheckoutId,
    Guid SellerOrderId,
    Guid FulfillmentId,
    Guid ShippingGroupId,
    string AttemptKind,
    Guid? SourceShipmentId,
    Guid? DamagedResolutionId,
    Guid? ReplacementReservationId,
    string LineFingerprint,
    /// <summary>The physical parcel's own recorded shipping method (empty when the parcel records none).</summary>
    string MethodCode = "");

/// <summary>
/// The single shared comparison used by the Fulfillment dispatch guard and the read projections.
/// Returns <c>null</c> when the plan is genuinely owned by the candidate; otherwise the typed
/// reason code. Failures are closed: an absent plan or an unreadable/duplicated/conflicting row
/// is never treated as "planned".
/// </summary>
public static class OrderShipmentDispatchPlanBindingRules
{
    /// <summary>Compares one stored plan to the owning candidate.</summary>
    public static string? Validate(
        OrderShipmentDispatchPlanFact? plan,
        OrderShipmentDispatchPlanBindingCandidate? candidate)
    {
        if (candidate is null)
        {
            return OrderShipmentDispatchPlanBindingReasonCodes.ParcelMismatch;
        }

        if (plan is null)
        {
            return OrderShipmentDispatchPlanBindingReasonCodes.Missing;
        }

        if (plan.ShipmentId == Guid.Empty || plan.ShipmentId != candidate.ShipmentId)
        {
            return OrderShipmentDispatchPlanBindingReasonCodes.ParcelMismatch;
        }

        if (plan.CheckoutId != candidate.CheckoutId
            || plan.SellerOrderId != candidate.SellerOrderId
            || plan.FulfillmentId != candidate.FulfillmentId
            || plan.ShippingGroupId != candidate.ShippingGroupId
            || !string.Equals(plan.AttemptKind, candidate.AttemptKind, StringComparison.Ordinal))
        {
            return OrderShipmentDispatchPlanBindingReasonCodes.ParcelMismatch;
        }

        if (plan.SourceShipmentId != candidate.SourceShipmentId
            || plan.DamagedResolutionId != candidate.DamagedResolutionId
            || plan.ReplacementReservationId != candidate.ReplacementReservationId)
        {
            return OrderShipmentDispatchPlanBindingReasonCodes.SourceMismatch;
        }

        if (!FingerprintsMatch(plan.SourceFingerprint, candidate.LineFingerprint))
        {
            return OrderShipmentDispatchPlanBindingReasonCodes.QuantityMismatch;
        }

        // Method identity: the plan promises this parcel's method. A plan whose method cannot be checked against the
        // parcel (or disagrees with it) is not that parcel's plan.
        var planMethod = (plan.MethodCode ?? string.Empty).Trim();
        var parcelMethod = (candidate.MethodCode ?? string.Empty).Trim();
        if (parcelMethod.Length == 0)
        {
            return OrderShipmentDispatchPlanBindingReasonCodes.MethodUnverified;
        }

        if (planMethod.Length == 0
            || !string.Equals(planMethod, parcelMethod, StringComparison.OrdinalIgnoreCase))
        {
            return OrderShipmentDispatchPlanBindingReasonCodes.MethodUnverified;
        }

        if (plan.SnapshotId == Guid.Empty
            || plan.DraftId == Guid.Empty
            || plan.SourceRevision <= 0
            || plan.AreaGroupId == Guid.Empty
            || plan.RateRowId == Guid.Empty
            || string.IsNullOrWhiteSpace(plan.ServiceCode)
            || plan.CustomerReceivingDate == default
            || plan.CustomerReceivingOpenTicks >= plan.CustomerReceivingCloseTicks
            || string.IsNullOrWhiteSpace(plan.CustomerReceivingTimeZone)
            || plan.PreparationStartUtc > plan.TransferDepartureUtc
            || plan.TransferDepartureUtc > plan.HandoffDeadlineUtc)
        {
            return OrderShipmentDispatchPlanBindingReasonCodes.Corrupt;
        }

        return null;
    }

    /// <summary>
    /// Builds the canonical line fingerprint <c>lineId=quantity;lineId=quantity</c> sorted by line
    /// identity so two different orderings of the same basket never look like two different plans.
    /// </summary>
    public static string BuildLineFingerprint(IEnumerable<(Guid OrderLineId, decimal Quantity)> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var canonical = lines
            .GroupBy(x => x.OrderLineId)
            .Select(group => (Id: group.Key, Quantity: group.Aggregate(0m, (sum, x) => sum + x.Quantity)))
            .Where(x => x.Quantity != 0m)
            .OrderBy(x => x.Id.ToString("N"), StringComparer.Ordinal)
            .Select(x => string.Concat(x.Id.ToString("N"), "=", FormatQuantity(x.Quantity)));

        return string.Join(";", canonical);
    }

    /// <summary>Parses a stored fingerprint back into its canonical line quantities.</summary>
    public static IReadOnlyList<(Guid OrderLineId, decimal Quantity)> ParseLineFingerprint(string? fingerprint)
    {
        if (string.IsNullOrWhiteSpace(fingerprint))
        {
            return Array.Empty<(Guid, decimal)>();
        }

        var parsed = new List<(Guid, decimal)>();
        foreach (var entry in fingerprint.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = entry.IndexOf('=', StringComparison.Ordinal);
            if (separator <= 0 || separator == entry.Length - 1)
            {
                continue;
            }

            if (!Guid.TryParseExact(entry.AsSpan(0, separator), "N", out var lineId))
            {
                continue;
            }

            if (!decimal.TryParse(
                    entry.AsSpan(separator + 1),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var quantity))
            {
                continue;
            }

            parsed.Add((lineId, quantity));
        }

        return parsed;
    }

    /// <summary>True when two fingerprints describe the same lines/quantities.</summary>
    public static bool FingerprintsMatch(string? left, string? right)
        => string.Equals(
            NormalizeFingerprint(left),
            NormalizeFingerprint(right),
            StringComparison.Ordinal);

    private static string NormalizeFingerprint(string? fingerprint)
    {
        if (string.IsNullOrWhiteSpace(fingerprint))
        {
            return string.Empty;
        }

        var entries = ParseLineFingerprint(fingerprint);
        return entries.Count == 0
            ? fingerprint.Trim()
            : BuildLineFingerprint(entries);
    }

    private static string FormatQuantity(decimal value)
        => value.ToString("0.############################", CultureInfo.InvariantCulture);
}
