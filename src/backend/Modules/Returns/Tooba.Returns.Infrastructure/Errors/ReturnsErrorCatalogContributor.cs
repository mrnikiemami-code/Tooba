using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Returns.Contracts;
using Tooba.Returns.Contracts.Errors;

namespace Tooba.Returns.Infrastructure.Errors;

/// <summary>کاتالوگ صریح کدهای خطای Returns.</summary>
public sealed class ReturnsErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(ReturnsErrorCodes.Missing, ErrorClassification.NotFound, StatusCodes.Status404NotFound, "Return was not found."),
        D(ReturnsErrorCodes.Rejected, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Return was rejected."),
        D(ReturnsErrorCodes.Expired, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Return window expired."),
        D(ReturnsErrorCodes.NonReturnable, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Item is not returnable."),
        D(ReturnsErrorCodes.QuantityExceeded, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Return quantity exceeded."),
        D(ReturnsErrorCodes.QuantityInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Return quantity is invalid."),
        D(ReturnsErrorCodes.NotDelivered, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Order is not delivered."),
        D(ReturnsErrorCodes.NotPaid, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Order is not paid."),
        D(ReturnsErrorCodes.FulfillmentMissing, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Fulfillment missing for return."),
        D(ReturnsErrorCodes.Stale, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "Return status changed."),
        D(ReturnsErrorCodes.AlreadyApproved, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "Return already approved."),
        D(ReturnsErrorCodes.AlreadyRejected, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "Return already rejected."),
        D(ReturnsErrorCodes.LineMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound, "Order line missing."),
        D(ReturnsErrorCodes.NotOwner, ErrorClassification.Forbidden, StatusCodes.Status403Forbidden, "Not return owner."),
        D(ReturnsErrorCodes.IdempotencyRequired, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Idempotency key required."),
        D(ReturnsErrorCodes.IdempotencyConflict, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "The idempotency key belongs to a different return request."),
        D(ReturnsErrorCodes.DuplicateLine, ErrorClassification.Business, StatusCodes.Status400BadRequest, "An order line was submitted more than once."),
        D(ReturnsErrorCodes.QuantityPrecision, ErrorClassification.Business, StatusCodes.Status400BadRequest, "The return quantity has more precision than the stored quantity supports."),
        D(ReturnsErrorCodes.QuantityUnitInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest, "A return quantity must land exactly on the owning unit's precision, and that precision must be answered by the owning order line."),
        D(ReturnsErrorCodes.RefundDestinationInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Invalid refund destination."),
        D(ReturnsErrorCodes.RefundRetryInvalidState, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Refund retry invalid state."),
        D(ReturnsErrorCodes.RefundAlreadyStarted, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "Refund already started."),
        D(ReturnsErrorCodes.RefundAlreadyCompleted, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "Refund already completed."),
        D(ReturnsErrorCodes.RefundPaymentMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound, "Refund payment missing."),
        D(ReturnsErrorCodes.RecoveryKindInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest, "The requested refund recovery kind is missing or unknown."),
        D(ReturnsErrorCodes.RecoveryAttemptMismatch, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "The expected refund attempt does not match the stored attempt of this return."),
        D(ReturnsErrorCodes.RecoveryStateMismatch, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "The requested recovery does not apply to the stored state of this return."),
        D(ReturnsErrorCodes.RecoveryReceivingRequired, ErrorClassification.Business, StatusCodes.Status400BadRequest, "The received units have no recorded receipt/inspection, so their physical disposition cannot be executed."),
        D(ReturnsErrorCodes.RecoveryAttemptUnbound, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "No single stored refund attempt is bound to this money phase."),
        D(ReturnsErrorCodes.RecoveryGatewayUnavailable, ErrorClassification.Business, StatusCodes.Status503ServiceUnavailable, "The refund adapter is not configured, so the stored attempt cannot be sent; it stays pending."),
        D(ReturnsErrorCodes.RecoveryEvidenceConflict, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "The stored decision, refund attempt or closure facts of this return contradict each other, so no recovery can be named without guessing."),
        D(ReturnsErrorCodes.RecoveryTransactionOwned, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "Recovery cannot run inside an open caller-owned database transaction, so nothing was sent or written."),
        D(ReturnsErrorCodes.RecoveryStockBindingRequired, ErrorClassification.Business, StatusCodes.Status400BadRequest, "The inspected usable quantity has no owning stock binding, so no saleable restock can be confirmed; it stays pending."),
        D(ReturnsErrorCodes.RecoveryPhysicalResolutionRequired, ErrorClassification.Business, StatusCodes.Status400BadRequest, "The rejected/cancelled return physically received goods that still have no recorded disposition."),
        D(ReturnsErrorCodes.ReceiptOccurrenceRequired, ErrorClassification.Business, StatusCodes.Status400BadRequest, "A customer receipt must name the reviewed occurrence it belongs to."),
        D(ReturnsErrorCodes.ReceiptLineInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest, "A receipt line is not a valid inspected quantity of this return."),
        D(ReturnsErrorCodes.ReceiptOverReceived, ErrorClassification.Business, StatusCodes.Status400BadRequest, "The submitted receipt would receive more units than this return requested."),
        D(ReturnsErrorCodes.ReceiptConflict, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "The occurrence key was already recorded with a different receiving intent."),
        D(ReturnsErrorCodes.ReceiptOriginInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest, "A courier-origin case reuses the receipt owned by fulfillment and cannot record a second one here."),
        D(ReturnsErrorCodes.ReceiptStateInvalid, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "The return is not open for a new physical receipt."),
        D(ReturnsErrorCodes.ReceiptActorRequired, ErrorClassification.Business, StatusCodes.Status400BadRequest, "A customer receipt must name the recording operator."),
        D(ReturnsErrorCodes.ReceiptUnitQuantityInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest, "An inspected quantity must land exactly on the owning unit's precision, and that precision must be answered by the owning order line."),
        D(ReturnsErrorCodes.ReceivingIncomplete, ErrorClassification.Business, StatusCodes.Status400BadRequest, "The return has no complete recorded receiving/inspection coverage, so no approval, refund or restock can start."),
        D(ReturnsErrorCodes.RejectionReasonRequired, ErrorClassification.Business, StatusCodes.Status400BadRequest, "A new rejection must state a meaningful reason."),
        D(ReturnsErrorCodes.DecisionReasonConflict, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "The recorded rejection already states a different reason."),
        D(ReturnsErrorCodes.FirstRefundNotLawful, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "Only an approved return whose refund was never started can begin its first refund."),
    ];

    private static ErrorDescriptor D(
        string code,
        ErrorClassification classification,
        int status,
        string fallback) =>
        new(code, classification, status, code, ErrorSeverity.Warning, fallback);
}
