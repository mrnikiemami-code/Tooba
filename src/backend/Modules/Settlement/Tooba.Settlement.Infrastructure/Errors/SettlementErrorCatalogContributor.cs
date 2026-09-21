using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Settlement.Application.Errors;

namespace Tooba.Settlement.Infrastructure.Errors;

/// <summary>کاتالوگ صریح کدهای خطای Settlement.</summary>
public sealed class SettlementErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(SettlementErrorCodes.AccountMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Settlement account was not found."),
        D(SettlementErrorCodes.PayoutRejected, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Payout was rejected."),
        D(SettlementErrorCodes.PayoutInvalidAmount, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Payout amount is invalid."),
        D(SettlementErrorCodes.PayoutMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Payout request was not found."),
        D(SettlementErrorCodes.PayoutInvalidState, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Payout state is invalid for this operation."),
        D(SettlementErrorCodes.IdempotencyRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Idempotency key is required."),
        D(SettlementErrorCodes.AmountInvalid, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Amount must be positive."),
        D(SettlementErrorCodes.AccrualPaymentMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Payment for accrual was not found."),
        D(SettlementErrorCodes.AccrualPaymentNotSucceeded, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Accrual requires a succeeded payment."),
        D(SettlementErrorCodes.AccrualOrderMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Order for accrual was not found."),
        D(SettlementErrorCodes.AccrualOrderNotPaid, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Accrual requires a paid order."),
        D(SettlementErrorCodes.RefundMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Refund snapshot was not found."),
        D(SettlementErrorCodes.RefundMismatch, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Refund snapshot does not match the event."),
        D(SettlementErrorCodes.GatewayUnconfigured, ErrorClassification.Business, StatusCodes.Status503ServiceUnavailable,
            "Payout gateway is unconfigured."),
        D(SettlementErrorCodes.OutboxUnmapped, ErrorClassification.Business, StatusCodes.Status500InternalServerError,
            "Settlement outbox event type is unmapped."),
        D("settlement.unconfirm.payout_completed", ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Cannot unconfirm accrual after completed payout."),
        D("settlement.cancel.payout_completed", ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Cannot cancel accrual after completed payout."),
        D("settlement.restore.payout_completed", ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Cannot restore after completed payout."),
    ];

    private static ErrorDescriptor D(
        string code,
        ErrorClassification classification,
        int status,
        string fallback) =>
        new(code, classification, status, code, ErrorSeverity.Warning, fallback);
}
