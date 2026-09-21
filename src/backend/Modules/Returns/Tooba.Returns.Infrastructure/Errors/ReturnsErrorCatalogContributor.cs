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
        D(ReturnsErrorCodes.RefundDestinationInvalid, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Invalid refund destination."),
        D(ReturnsErrorCodes.RefundRetryInvalidState, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Refund retry invalid state."),
        D(ReturnsErrorCodes.RefundAlreadyStarted, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "Refund already started."),
        D(ReturnsErrorCodes.RefundAlreadyCompleted, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "Refund already completed."),
        D(ReturnsErrorCodes.RefundPaymentMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound, "Refund payment missing."),
    ];

    private static ErrorDescriptor D(
        string code,
        ErrorClassification classification,
        int status,
        string fallback) =>
        new(code, classification, status, code, ErrorSeverity.Warning, fallback);
}
