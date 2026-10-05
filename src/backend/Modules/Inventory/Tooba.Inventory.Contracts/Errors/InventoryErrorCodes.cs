namespace Tooba.Inventory.Contracts.Errors;

/// <summary>
/// Stable semantic error codes owned by Inventory. Values are the machine codes emitted by the
/// Inventory Domain/Application/Infrastructure and mapped by the canonical composed error catalog.
/// The strings are part of the module boundary and are consumed by Order/Payment/Returns; they must
/// never be renamed or repurposed.
/// </summary>
public static class InventoryErrorCodes
{
    private static readonly HashSet<string> KnownCodes = new(StringComparer.Ordinal)
    {
        QuantityInvalid,
        LocationIdRequired,
        LocationCodeInvalid,
        LocationNameRequired,
        LocationNotFound,
        PositionIdsRequired,
        PositionQuantityInvalid,
        PositionNotFound,
        PositionIdRequired,
        PositionIdUnknown,
        ReservationIdRequired,
        ReservationQuantityInvalid,
        ReservationExpiryInvalid,
        ReservationReviewExpiryInvalid,
        ReservationNotActive,
        ReservationNotFound,
        ReservationConflict,
        ReservationReleaseMismatch,
        ReservationConsumeMismatch,
        CatalogVariantMissing,
        AdjustmentQuantityInvalid,
        AdjustmentQuantityExceeds,
        AdjustmentReasonRequired,
        AdjustmentKindUnknown,
        SupplyUnavailable,
        SupplyModeInvalid,
        ManualReviewUnavailable,
        ReturnRestockInvalid,
        OutboxUnmappedEventType,
    };

    /// <summary>
    /// True when <paramref name="code"/> is a stable code declared by this Inventory catalog.
    /// Used by the module composition seam so Inventory faults map to <c>Result</c> while codes owned
    /// by another module (for example <c>order.restore.*</c>) propagate untouched.
    /// </summary>
    public static bool IsKnown(string? code) =>
        !string.IsNullOrWhiteSpace(code) && KnownCodes.Contains(code);

    /// <summary>The on-hand/requested quantity is invalid.</summary>
    public const string QuantityInvalid = "inventory.quantity.invalid";

    /// <summary>A storage location id is required.</summary>
    public const string LocationIdRequired = "inventory.location.id_required";

    /// <summary>The storage location code is invalid.</summary>
    public const string LocationCodeInvalid = "inventory.location.code_invalid";

    /// <summary>The storage location name is required.</summary>
    public const string LocationNameRequired = "inventory.location.name_required";

    /// <summary>The requested storage location does not exist or is not active.</summary>
    public const string LocationNotFound = "inventory.location.not_found";

    /// <summary>Stock position identifiers are required.</summary>
    public const string PositionIdsRequired = "inventory.position.ids_required";

    /// <summary>The stock position quantities are illegal.</summary>
    public const string PositionQuantityInvalid = "inventory.position.quantity_invalid";

    /// <summary>The requested stock position does not exist.</summary>
    public const string PositionNotFound = "inventory.position.not_found";

    /// <summary>A stock position id is required.</summary>
    public const string PositionIdRequired = "inventory.position.id_required";

    /// <summary>The referenced stock position is unknown to the inventory directory.</summary>
    public const string PositionIdUnknown = "inventory.position.id_unknown";

    /// <summary>A reservation id is required.</summary>
    public const string ReservationIdRequired = "inventory.reservation.id_required";

    /// <summary>The reservation quantity is invalid.</summary>
    public const string ReservationQuantityInvalid = "inventory.reservation.quantity_invalid";

    /// <summary>The reservation expiry is invalid.</summary>
    public const string ReservationExpiryInvalid = "inventory.reservation.expiry_invalid";

    /// <summary>The manual-review reservation expiry is invalid.</summary>
    public const string ReservationReviewExpiryInvalid = "inventory.reservation.review_expiry_invalid";

    /// <summary>The reservation is no longer active (Released/Consumed).</summary>
    public const string ReservationNotActive = "inventory.reservation.not_active";

    /// <summary>The reservation does not exist.</summary>
    public const string ReservationNotFound = "inventory.reservation.not_found";

    /// <summary>The reservation idempotency key conflicts with an existing reservation.</summary>
    public const string ReservationConflict = "inventory.reservation.conflict";

    /// <summary>The released quantity does not match the held quantity.</summary>
    public const string ReservationReleaseMismatch = "inventory.reservation.release_mismatch";

    /// <summary>The consumed quantity does not match the held quantity.</summary>
    public const string ReservationConsumeMismatch = "inventory.reservation.consume_mismatch";

    /// <summary>The catalog variant referenced by the offer is missing.</summary>
    public const string CatalogVariantMissing = "inventory.catalog_variant.missing";

    /// <summary>The adjustment quantity is invalid.</summary>
    public const string AdjustmentQuantityInvalid = "inventory.adjustment.quantity_invalid";

    /// <summary>The adjustment would push on-hand above the reserved quantity.</summary>
    public const string AdjustmentQuantityExceeds = "inventory.adjustment.quantity_exceeds";

    /// <summary>The adjustment kind is unknown.</summary>
    public const string AdjustmentKindUnknown = "inventory.adjustment.kind_unknown";

    /// <summary>An adjustment reason is required.</summary>
    public const string AdjustmentReasonRequired = "inventory.adjustment.reason_required";

    /// <summary>The requested stock is not available to reserve.</summary>
    public const string SupplyUnavailable = "inventory.supply.unavailable";

    /// <summary>The order-supply mode is invalid.</summary>
    public const string SupplyModeInvalid = "inventory.supply.mode_invalid";

    /// <summary>The reservation could not be promoted/reacquired for manual payment review.</summary>
    public const string ManualReviewUnavailable = "inventory.manual_review.unavailable";

    /// <summary>The return-restock request is not usable (quantity, idempotency key or reservation state).</summary>
    public const string ReturnRestockInvalid = "inventory.return_restock.invalid";

    /// <summary>The outbox registration has no mapping for the given integration event type.</summary>
    public const string OutboxUnmappedEventType = "inventory.outbox.unmapped_event_type";
}
