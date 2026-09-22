using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Wallet.Application.Errors;

/// <summary>
/// Maps known Wallet directory/domain stable machine codes to SemanticError.
/// Exact message match only — no Contains, no StartsWith prose heuristics; unknowns rethrow.
/// </summary>
public static class WalletExceptionMapper
{
    private static readonly HashSet<string> KnownCodes = new(StringComparer.Ordinal)
    {
        "wallet.giftcard.ids_required",
        "wallet.giftcard.amount_positive",
        "wallet.giftcard.issuer_required",
        "wallet.giftcard.expiry_future",
        "wallet.idempotency_required",
        "wallet.giftcard.amounts_invalid",
        "wallet.giftcard.not_revocable",
        "wallet.giftcard.redeem_amount_invalid",
        "wallet.giftcard.revoked",
        "wallet.giftcard.fully_redeemed",
        "wallet.giftcard.expired",
        "wallet.giftcard.status_invalid",
        "wallet.giftcard.zero_remaining",
        "wallet.giftcard.code_required",
        "wallet.giftcard.code_length",
        "wallet.idempotency_invalid",
        "wallet.account.ids_required",
        "wallet.ids_required",
        "wallet.currency_required",
        "wallet.currency_invalid",
        "wallet.giftcard.status_parse",
        "wallet.adjustment.direction_invalid",
        "wallet.ledger.ids_required",
        "wallet.ledger.amount_positive",
        "wallet.ledger.source_type_invalid",
        "wallet.metadata_too_long",
        "wallet.giftcard.redemption_ids",
        "wallet.giftcard.redemption_amount",
        "wallet.giftcard.redemption_id",
        "wallet.outbox.unmapped_event_type",
        "wallet.rejected.SWRlbXBv",
        "wallet.rejected.2KjYp9iy",
        "wallet.rejected.2qnYryDa",
        "wallet.rejected.2KfYsdiy",
        "wallet.rejected.2K3Ys9in",
        "wallet.rejected.2qnYp9ix",
        "wallet.rejected.2K_ZhNuM",
        "wallet.rejected.2YXZiNis",
        "wallet.rejected.2YfZiNuM",
        "wallet.rejected.2YXYqNmE",
        "wallet.rejected.2qnZhNuM",
    };

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

    public static SemanticError ToSemanticError(InvalidOperationException exception, string publicErrorCode)
    {
        ArgumentNullException.ThrowIfNull(exception);
        if (TryMapExact(exception.Message, publicErrorCode, out var error))
            return error;
        throw exception;
    }

    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> action, string publicErrorCode)
    {
        try
        {
            return Result.Success(await action());
        }
        catch (InvalidOperationException ex) when (TryMapExact(ex.Message, publicErrorCode, out var error))
        {
            return Result.Failure<T>(error);
        }
    }
}
