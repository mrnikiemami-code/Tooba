using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.Errors;

/// <summary>
/// Maps known Returns directory/domain stable machine codes to SemanticError.
/// Exact message match only — no Contains, no StartsWith prose heuristics; unknowns rethrow.
/// </summary>
public static class ReturnsExceptionMapper
{
    /// <summary>Converts a known Returns InvalidOperationException into a SemanticError. Unknowns rethrow.</summary>
    public static SemanticError ToSemanticError(InvalidOperationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        if (TryMapExact(exception.Message, out var error))
        {
            return error;
        }

        throw exception;
    }

    /// <summary>Runs a Returns directory action and maps only known expected failures to Result.</summary>
    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return Result.Success(await action());
        }
        catch (ContractOperationException ex) when (TryMapExact(ex.Code, out var error))
        {
            return Result.Failure<T>(error);
        }
        catch (InvalidOperationException ex) when (TryMapExact(ex.Message, out var error))
        {
            return Result.Failure<T>(error);
        }
    }

    /// <summary>Exact stable-code mapping only.</summary>
    public static bool TryMapExact(string? message, out SemanticError error)
    {
        switch (message)
        {
            case ReturnsErrorCodes.Missing:
            case "returns.request.not_found":
            case "returns.order_missing":
                error = new SemanticError(ReturnsErrorCodes.Missing);
                return true;

            case ReturnsErrorCodes.Rejected:
                error = new SemanticError(ReturnsErrorCodes.Rejected);
                return true;

            case ReturnsErrorCodes.Expired:
            case "returns.window_expired":
                error = new SemanticError(ReturnsErrorCodes.Expired);
                return true;

            case ReturnsErrorCodes.NonReturnable:
            case "returns.non_returnable":
                error = new SemanticError(ReturnsErrorCodes.NonReturnable);
                return true;

            case ReturnsErrorCodes.QuantityExceeded:
            case "returns.nothing_returnable":
            case "returns.qty.exceeds_remaining":
                error = new SemanticError(ReturnsErrorCodes.QuantityExceeded);
                return true;

            case ReturnsErrorCodes.QuantityInvalid:
            case "returns.qty.positive":
                error = new SemanticError(ReturnsErrorCodes.QuantityInvalid);
                return true;

            case ReturnsErrorCodes.NotDelivered:
            case "returns.not_delivered":
                error = new SemanticError(ReturnsErrorCodes.NotDelivered);
                return true;

            case ReturnsErrorCodes.NotPaid:
            case "returns.not_paid":
                error = new SemanticError(ReturnsErrorCodes.NotPaid);
                return true;

            case ReturnsErrorCodes.FulfillmentMissing:
            case "returns.fulfillment_missing":
                error = new SemanticError(ReturnsErrorCodes.FulfillmentMissing);
                return true;

            case ReturnsErrorCodes.Stale:
            case "fulfillment.status.transition_invalid":
                error = new SemanticError(ReturnsErrorCodes.Stale);
                return true;

            case ReturnsErrorCodes.AlreadyApproved:
                error = new SemanticError(ReturnsErrorCodes.AlreadyApproved);
                return true;

            case ReturnsErrorCodes.AlreadyRejected:
                error = new SemanticError(ReturnsErrorCodes.AlreadyRejected);
                return true;

            case ReturnsErrorCodes.LineMissing:
            case "returns.order_line.not_found":
                error = new SemanticError(ReturnsErrorCodes.LineMissing);
                return true;

            case ReturnsErrorCodes.NotOwner:
            case "returns.actor.not_owner":
                error = new SemanticError(ReturnsErrorCodes.NotOwner);
                return true;

            case ReturnsErrorCodes.IdempotencyRequired:
            case "returns.idempotency.required":
                error = new SemanticError(ReturnsErrorCodes.IdempotencyRequired);
                return true;

            case ReturnsErrorCodes.RefundDestinationInvalid:
            case "returns.refund_destination.invalid":
                error = new SemanticError(ReturnsErrorCodes.RefundDestinationInvalid);
                return true;

            case ReturnsErrorCodes.RefundRetryInvalidState:
            case "returns.retry.invalid_status":
                error = new SemanticError(ReturnsErrorCodes.RefundRetryInvalidState);
                return true;

            case ReturnsErrorCodes.RefundAlreadyStarted:
            case "returns.payment.not_succeeded":
                error = new SemanticError(ReturnsErrorCodes.RefundAlreadyStarted);
                return true;

            case ReturnsErrorCodes.RefundAlreadyCompleted:
                error = new SemanticError(ReturnsErrorCodes.RefundAlreadyCompleted);
                return true;

            case ReturnsErrorCodes.RefundPaymentMissing:
            case "returns.payment.not_found":
            case "returns.payment.reference_missing":
                error = new SemanticError(ReturnsErrorCodes.RefundPaymentMissing);
                return true;

            case "returns.outbox.unmapped_event":
                error = new SemanticError("returns.outbox.unmapped_event");
                return true;

            default:
                error = default!;
                return false;
        }
    }

    /// <summary>Parse refund destination; invalid → SemanticError.</summary>
    public static Result<RefundDestination> ParseDestination(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Success(RefundDestination.OriginalPayment);
        }

        return Enum.TryParse<RefundDestination>(value, ignoreCase: true, out var parsed)
            ? Result.Success(parsed)
            : Result.Failure<RefundDestination>(new SemanticError(ReturnsErrorCodes.RefundDestinationInvalid));
    }
}
