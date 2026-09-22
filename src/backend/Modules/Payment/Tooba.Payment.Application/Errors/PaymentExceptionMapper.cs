using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Payment.Application.Errors;

/// <summary>
/// Exact machine-code Payment exception mapping only — no Contains/StartsWith prose heuristics.
/// </summary>
public static class PaymentExceptionMapper
{
    private static readonly HashSet<string> KnownCodes = new(StringComparer.Ordinal)
    {
        PaymentErrorCodes.Missing,
        PaymentErrorCodes.Rejected,
        PaymentErrorCodes.AccessDenied,
        PaymentErrorCodes.GuestInvalid,
        PaymentErrorCodes.AlreadySucceeded,
        PaymentErrorCodes.WalletMixedDeferred,
        PaymentErrorCodes.MethodUnavailable,
        PaymentErrorCodes.TrackingRequired,
        PaymentErrorCodes.ProofRequired,
        PaymentErrorCodes.ProofForeign,
        PaymentErrorCodes.SandboxUnavailable,
        PaymentErrorCodes.UnpaidSupplyUnavailable,
        PaymentErrorCodes.UnpaidRetryInvalid,
        PaymentErrorCodes.WebhookUnauthorized,
        PaymentErrorCodes.WebhookInvalidPayload,
        PaymentErrorCodes.WebhookAmountMismatch,
        PaymentErrorCodes.WebhookAttemptMismatch,
        PaymentErrorCodes.WebhookProviderMismatch,
        PaymentErrorCodes.WebhookRejected,
        "payment.webhook.signature_invalid",
        "payment.webhook.signature_missing",
        "payment.already_paid",
        "payment.status_invalid",
        "payment.provider_invalid",
        "payment.amount_invalid",
        "payment.currency_invalid",
        "payment.idempotency_invalid",
        "payment.attempt_missing",
        "payment.method_invalid",
        "payment.manual_evidence_invalid",
        "payment.reconcile_rejected",
        "payment.deposit_confirm_rejected",
        "payment.deposit_reject_rejected",
    };

    public static bool TryMapExact(string? message, out SemanticError error)
    {
        if (message is not null && KnownCodes.Contains(message))
        {
            error = new SemanticError(message);
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

    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> action, string? publicErrorCode = null)
    {
        try
        {
            return Result.Success(await action());
        }
        catch (InvalidOperationException ex) when (
            publicErrorCode is null
                ? TryMapExact(ex.Message, out var mapped)
                : TryMapExact(ex.Message, publicErrorCode, out mapped))
        {
            return Result.Failure<T>(mapped);
        }
    }

    public static async Task<Result> TryAsync(Func<Task> action, string? publicErrorCode = null)
    {
        try
        {
            await action();
            return Result.Success();
        }
        catch (InvalidOperationException ex) when (
            publicErrorCode is null
                ? TryMapExact(ex.Message, out var mapped)
                : TryMapExact(ex.Message, publicErrorCode, out mapped))
        {
            return Result.Failure(mapped);
        }
    }
}
