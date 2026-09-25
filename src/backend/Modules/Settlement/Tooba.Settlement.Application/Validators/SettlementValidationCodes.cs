namespace Tooba.Settlement.Application.Validators;

/// <summary>
/// Stable semantic error codes owned by the Settlement transport-validation boundary.
/// These are primitive/syntactic shape codes only; business/payout policy codes stay in
/// <see cref="Errors.SettlementErrorCodes"/>.
/// </summary>
public static class SettlementValidationCodes
{
    /// <summary>Payout amount must be greater than zero.</summary>
    public const string PayoutAmountPositive = "settlement.validation.payout_amount_positive";

    /// <summary>Idempotency key must not be blank.</summary>
    public const string IdempotencyKeyShape = "settlement.validation.idempotency_key_shape";

    /// <summary>Payout request identifier must not be empty.</summary>
    public const string PayoutRequestIdRequired = "settlement.validation.payout_request_id_required";

    /// <summary>Admin payout grid request body must be supplied.</summary>
    public const string GridRequestRequired = "settlement.validation.grid_request_required";
}
