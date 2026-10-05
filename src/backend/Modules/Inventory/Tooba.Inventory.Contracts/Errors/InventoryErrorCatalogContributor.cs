using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;

namespace Tooba.Inventory.Contracts.Errors;

/// <summary>
/// Inventory-owned error catalog. Registers one descriptor per Inventory-emitted machine code so the
/// canonical <c>SafeErrorMapper</c> classifies them instead of falling back to a generic 400.
/// <para>
/// Inventory is <c>INTERNAL_ONLY</c> (no HTTP surface); the descriptors still pin the semantics for
/// the module's consumer seams (Offer seller stock write, Order supply/recovery, Payment unpaid
/// supply mapping, Returns restock). Classification is by stable machine code only.
/// </para>
/// <para>
/// <c>inventory.reservation.retry_limit_reached</c> is deliberately absent: it is declared and owned by
/// Order (and intentionally not re-registered by Payment), so Inventory must not claim its descriptor.
/// </para>
/// </summary>
public sealed class InventoryErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(InventoryErrorCodes.QuantityInvalid, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Quantity is invalid."),
        D(InventoryErrorCodes.LocationIdRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Location id is required."),
        D(InventoryErrorCodes.LocationCodeInvalid, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Location code is invalid."),
        D(InventoryErrorCodes.LocationNameRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Location name is required."),
        D(InventoryErrorCodes.LocationNotFound, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Storage location was not found."),
        D(InventoryErrorCodes.PositionIdsRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Stock position ids are required."),
        D(InventoryErrorCodes.PositionQuantityInvalid, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Stock position quantities are invalid."),
        D(InventoryErrorCodes.PositionNotFound, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Stock position was not found."),
        D(InventoryErrorCodes.PositionIdRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Stock position id is required."),
        D(InventoryErrorCodes.PositionIdUnknown, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "Stock position is unknown."),
        D(InventoryErrorCodes.ReservationIdRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Reservation id is required."),
        D(InventoryErrorCodes.ReservationQuantityInvalid, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Reservation quantity is invalid."),
        D(InventoryErrorCodes.ReservationExpiryInvalid, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Reservation expiry is invalid."),
        D(InventoryErrorCodes.ReservationReviewExpiryInvalid, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Manual review expiry is invalid."),
        D(InventoryErrorCodes.ReservationNotActive, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "Reservation is no longer active."),
        D(InventoryErrorCodes.ReservationNotFound, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Reservation was not found."),
        D(InventoryErrorCodes.ReservationConflict, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "Reservation conflicts with an existing reservation."),
        D(InventoryErrorCodes.ReservationReleaseMismatch, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "Reservation release does not match the held quantity."),
        D(InventoryErrorCodes.ReservationConsumeMismatch, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "Reservation consumption does not match the held quantity."),
        D(InventoryErrorCodes.CatalogVariantMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Catalog variant was not found."),
        D(InventoryErrorCodes.AdjustmentQuantityInvalid, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Adjustment quantity is invalid."),
        D(InventoryErrorCodes.AdjustmentQuantityExceeds, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "Adjustment exceeds the available quantity."),
        D(InventoryErrorCodes.AdjustmentKindUnknown, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Adjustment kind is unknown."),
        D(InventoryErrorCodes.AdjustmentReasonRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Adjustment reason is required."),
        D(InventoryErrorCodes.SupplyUnavailable, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "Stock is not available."),
        D(InventoryErrorCodes.SupplyModeInvalid, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Supply mode is invalid."),
        D(InventoryErrorCodes.ManualReviewUnavailable, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "Stock is unavailable for manual payment review."),
        D(InventoryErrorCodes.ReturnRestockInvalid, ErrorClassification.Conflict, StatusCodes.Status409Conflict,
            "Return restock request is invalid."),
        D(InventoryErrorCodes.OutboxUnmappedEventType, ErrorClassification.Platform, StatusCodes.Status500InternalServerError,
            "Integration event type is not registered."),
    ];

    private static ErrorDescriptor D(
        string code,
        ErrorClassification classification,
        int status,
        string fallback) =>
        new(
            Code: code,
            Classification: classification,
            HttpStatus: status,
            LocalizationKey: code,
            Severity: ErrorSeverity.Warning,
            SafeTitleFallback: fallback);
}
