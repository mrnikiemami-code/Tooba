using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Payment.Application.Errors;

/// <summary>
/// Maps known Payment directory/domain stable machine codes to SemanticError.
/// Exact message match only — no Contains, no StartsWith prose heuristics; unknowns rethrow.
/// </summary>
public static class PaymentExceptionMapper
{
    private static readonly HashSet<string> KnownCodes = new(StringComparer.Ordinal)
    {
        PaymentErrorCodes.AlreadySucceeded,
        PaymentErrorCodes.Missing,
        PaymentErrorCodes.AttemptMissing,
        PaymentErrorCodes.AccessDenied,
        PaymentErrorCodes.GuestInvalid,
        PaymentErrorCodes.WalletMixedDeferred,
        PaymentErrorCodes.MethodUnavailable,
        PaymentErrorCodes.TrackingRequired,
        PaymentErrorCodes.ProofRequired,
        PaymentErrorCodes.ProofForeign,
        PaymentErrorCodes.SandboxUnavailable,
        PaymentErrorCodes.UnpaidSupplyUnavailable,
        PaymentErrorCodes.UnpaidRetryInvalid,
        PaymentErrorCodes.ReservationRetryLimit,
        PaymentErrorCodes.Rejected,
        PaymentErrorCodes.WebhookInvalidSignature,
        PaymentErrorCodes.WebhookInvalidPayload,
        PaymentErrorCodes.WebhookAmountMismatch,
        PaymentErrorCodes.WebhookAttemptMismatch,
        PaymentErrorCodes.WebhookProviderMismatch,
        PaymentErrorCodes.GatewayUnconfigured,
        PaymentErrorCodes.AdminPaymentMissing,
        PaymentErrorCodes.MethodNotManual,
        PaymentErrorCodes.ConfirmInvalidState,
        PaymentErrorCodes.RejectInvalidState,
        PaymentErrorCodes.CheckoutAuthenticationRequired,
        "payment.not_found",
        "payment.tracking_reference.required",
        "payment.unpaid.retry.invalid_state",
        "inventory.supply.unavailable",
    };

    private static readonly Dictionary<string, string> PublicAliases = new(StringComparer.Ordinal)
    {
        ["payment.not_found"] = PaymentErrorCodes.Missing,
        ["payment.tracking_reference.required"] = PaymentErrorCodes.TrackingRequired,
        ["payment.unpaid.retry.invalid_state"] = PaymentErrorCodes.UnpaidRetryInvalid,
        ["inventory.supply.unavailable"] = PaymentErrorCodes.UnpaidSupplyUnavailable,
    };

    public static bool TryMapExact(string? message, out SemanticError error)
    {
        if (message is not null && KnownCodes.Contains(message))
        {
            var code = PublicAliases.TryGetValue(message, out var alias) ? alias : message;
            error = new SemanticError(code);
            return true;
        }

        error = default!;
        return false;
    }

    public static bool TryMapExact(string? message, string publicErrorCode, out SemanticError error)
    {
        if (message is not null && KnownCodes.Contains(message))
        {
            error = new SemanticError(publicErrorCode);
            return true;
        }

        error = default!;
        return false;
    }

    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return Result.Success(await action());
        }
        catch (InvalidOperationException ex) when (TryMapExact(ex.Message, out var mapped))
        {
            return Result.Failure<T>(mapped);
        }
    }

    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> action, string publicErrorCode)
    {
        try
        {
            return Result.Success(await action());
        }
        catch (InvalidOperationException ex) when (TryMapExact(ex.Message, publicErrorCode, out var mapped))
        {
            return Result.Failure<T>(mapped);
        }
    }
}
