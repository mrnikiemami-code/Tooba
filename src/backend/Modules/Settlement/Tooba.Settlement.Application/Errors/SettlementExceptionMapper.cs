using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Settlement.Application.Errors;

/// <summary>
/// Maps known Settlement domain/directory stable machine codes to SemanticError.
/// Exact message match only — no Contains, no StartsWith prose heuristics; unknowns rethrow.
/// </summary>
public static class SettlementExceptionMapper
{
    /// <summary>
    /// Converts a known Settlement <see cref="InvalidOperationException"/> into a SemanticError.
    /// Unknown messages are rethrown (not swallowed into <see cref="SettlementErrorCodes.PayoutRejected"/>).
    /// </summary>
    public static SemanticError ToSemanticError(InvalidOperationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        if (TryMapExact(exception.Message, out var error))
        {
            return error;
        }

        throw exception;
    }

    /// <summary>Runs a Settlement directory action and maps only known expected failures to Result.</summary>
    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return Result.Success(await action());
        }
        catch (InvalidOperationException ex) when (TryMapExact(ex.Message, out var error))
        {
            return Result.Failure<T>(error);
        }
    }

    /// <summary>Exact stable-code mapping only. Returns false for unknown / unexpected messages.</summary>
    public static bool TryMapExact(string? message, out SemanticError error)
    {
        switch (message)
        {
            case SettlementErrorCodes.AccountMissing:
                error = new SemanticError(SettlementErrorCodes.AccountMissing);
                return true;

            case SettlementErrorCodes.PayoutInvalidAmount:
            case SettlementErrorCodes.AmountInvalid:
                error = new SemanticError(SettlementErrorCodes.PayoutInvalidAmount);
                return true;

            case SettlementErrorCodes.IdempotencyRequired:
                error = new SemanticError(SettlementErrorCodes.IdempotencyRequired);
                return true;

            case SettlementErrorCodes.PayoutMissing:
                error = new SemanticError(SettlementErrorCodes.PayoutMissing);
                return true;

            case SettlementErrorCodes.PayoutInvalidState:
                error = new SemanticError(SettlementErrorCodes.PayoutInvalidState);
                return true;

            case SettlementErrorCodes.GatewayUnconfigured:
                error = new SemanticError(SettlementErrorCodes.GatewayUnconfigured);
                return true;

            case SettlementErrorCodes.PayoutRejected:
                error = new SemanticError(SettlementErrorCodes.PayoutRejected);
                return true;

            case SettlementErrorCodes.AccrualPaymentMissing:
                error = new SemanticError(SettlementErrorCodes.AccrualPaymentMissing);
                return true;

            case SettlementErrorCodes.AccrualPaymentNotSucceeded:
                error = new SemanticError(SettlementErrorCodes.AccrualPaymentNotSucceeded);
                return true;

            case SettlementErrorCodes.AccrualOrderMissing:
                error = new SemanticError(SettlementErrorCodes.AccrualOrderMissing);
                return true;

            case SettlementErrorCodes.AccrualOrderNotPaid:
                error = new SemanticError(SettlementErrorCodes.AccrualOrderNotPaid);
                return true;

            case SettlementErrorCodes.RefundMissing:
                error = new SemanticError(SettlementErrorCodes.RefundMissing);
                return true;

            case SettlementErrorCodes.RefundMismatch:
                error = new SemanticError(SettlementErrorCodes.RefundMismatch);
                return true;

            case SettlementErrorCodes.OutboxUnmapped:
                error = new SemanticError(SettlementErrorCodes.OutboxUnmapped);
                return true;

            case "settlement.unconfirm.payout_completed":
            case "settlement.cancel.payout_completed":
            case "settlement.restore.payout_completed":
                error = new SemanticError(message);
                return true;

            default:
                error = default!;
                return false;
        }
    }
}
