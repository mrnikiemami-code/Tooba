using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Payment.Application.Errors;

namespace Tooba.Payment.Endpoints.Errors;

/// <summary>Explicit Payment error catalog for ApiResponseFactory.</summary>
public sealed class PaymentErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(PaymentErrorCodes.AlreadySucceeded, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "Conflict"),
        D(PaymentErrorCodes.Missing, ErrorClassification.NotFound, StatusCodes.Status404NotFound, "Not Found"),
        D(PaymentErrorCodes.AttemptMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound, "Not Found"),
        D(PaymentErrorCodes.AdminPaymentMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound, "Not Found"),
        D(PaymentErrorCodes.AccessDenied, ErrorClassification.Forbidden, StatusCodes.Status403Forbidden, "Forbidden"),
        D(PaymentErrorCodes.GuestInvalid, ErrorClassification.Forbidden, StatusCodes.Status401Unauthorized, "Unauthorized"),
        D(PaymentErrorCodes.WalletMixedDeferred, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Bad Request"),
        D(PaymentErrorCodes.MethodUnavailable, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Bad Request"),
        D(PaymentErrorCodes.TrackingRequired, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Bad Request"),
        D(PaymentErrorCodes.ProofRequired, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Bad Request"),
        D(PaymentErrorCodes.ProofForeign, ErrorClassification.Forbidden, StatusCodes.Status403Forbidden, "Forbidden"),
        D(PaymentErrorCodes.SandboxUnavailable, ErrorClassification.Forbidden, StatusCodes.Status403Forbidden, "Forbidden"),
        D(PaymentErrorCodes.UnpaidRetryInvalid, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "Conflict"),
        D(PaymentErrorCodes.Rejected, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Bad Request"),
        D(PaymentErrorCodes.UnpaidSupplyUnavailable, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "این سفارش در حال حاضر قابل تأمین نیست."),
        // inventory.reservation.retry_limit_reached (PaymentErrorCodes.ReservationRetryLimit) is NOT
        // registered here: its natural bounded context is Order (ReservationCycleOptions /
        // ReservationCycleCoordinator), which owns the descriptor. Payment only consumes the code.
        // payment.missing / payment.rejected / payment.unpaid.supply_unavailable ARE Payment-owned:
        // Payment is the natural bounded context and the primary producer. The matching localization
        // keys are still resolved by the composed catalog contributor that owns the payment.* keyspace.
        D(PaymentErrorCodes.WebhookInvalidSignature, ErrorClassification.Forbidden, StatusCodes.Status401Unauthorized, "Unauthorized"),
        D(PaymentErrorCodes.WebhookInvalidPayload, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Bad Request"),
        D(PaymentErrorCodes.WebhookAmountMismatch, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "Conflict"),
        D(PaymentErrorCodes.WebhookAttemptMismatch, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "Conflict"),
        D(PaymentErrorCodes.WebhookProviderMismatch, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "Conflict"),
        D(PaymentErrorCodes.GatewayUnconfigured, ErrorClassification.Forbidden, StatusCodes.Status401Unauthorized, "Unauthorized"),
        D(PaymentErrorCodes.MethodNotManual, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Bad Request"),
        D(PaymentErrorCodes.ConfirmInvalidState, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Bad Request"),
        D(PaymentErrorCodes.RejectInvalidState, ErrorClassification.Business, StatusCodes.Status400BadRequest, "Bad Request"),
        // admin.authorization.denied / checkout.authentication_required are shared cross-cutting
        // codes owned by FoundationErrorCatalogContributor.
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
