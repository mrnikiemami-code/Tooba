using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Cart.Application.Errors;

/// <summary>
/// Maps known Cart domain/directory stable machine codes to SemanticError.
/// Exact message match only — no Contains, no prose heuristics; unknowns rethrow.
/// </summary>
public static class CartExceptionMapper
{
    /// <summary>
    /// Converts a known Cart <see cref="InvalidOperationException"/> into a SemanticError.
    /// Unknown messages are rethrown (not swallowed into <see cref="CartErrorCodes.Rejected"/>).
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

    /// <summary>Runs a Cart directory action and maps only known expected failures to Result.</summary>
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
        // Exact Ordinal match on stable machine codes emitted by Domain/Directory/handlers.
        switch (message)
        {
            case CartErrorCodes.Missing:
                error = new SemanticError(CartErrorCodes.Missing);
                return true;

            case CartErrorCodes.GuestInvalid:
            case "cart.guest_secret.invalid":
                error = new SemanticError(CartErrorCodes.GuestInvalid);
                return true;

            case CartErrorCodes.AccessDenied:
                error = new SemanticError(CartErrorCodes.AccessDenied);
                return true;

            case CartErrorCodes.VersionConflict:
            case "cart.version.stale":
                error = new SemanticError(CartErrorCodes.VersionConflict);
                return true;

            case CartErrorCodes.Expired:
                error = new SemanticError(CartErrorCodes.Expired);
                return true;

            case CartErrorCodes.QuantityInvalid:
            case "cart.line.quantity_positive":
            case "cart.line.quantity_ceiling":
            case "cart.quantity_policy.missing":
            case "offer.min_quantity.not_met":
            case "offer.max_quantity.exceeded":
                error = new SemanticError(CartErrorCodes.QuantityInvalid);
                return true;

            case CartErrorCodes.LineMissing:
                error = new SemanticError(CartErrorCodes.LineMissing);
                return true;

            case CartErrorCodes.OfferUnavailable:
            case "cart.offer.missing":
            case "cart.offer.inactive":
            case "cart.offer.channel_mismatch":
                error = new SemanticError(CartErrorCodes.OfferUnavailable);
                return true;

            case CartErrorCodes.InventoryInsufficient:
            case "cart.inventory.missing":
                error = new SemanticError(CartErrorCodes.InventoryInsufficient);
                return true;

            case CartErrorCodes.InventoryStale:
                error = new SemanticError(CartErrorCodes.InventoryStale);
                return true;

            case CartErrorCodes.Rejected:
            case "cart.line.requires_active":
            case "cart.converted.not_expirable":
                error = new SemanticError(CartErrorCodes.Rejected);
                return true;

            case CartErrorCodes.AuthenticationRequired:
                error = new SemanticError(CartErrorCodes.AuthenticationRequired);
                return true;

            default:
                error = default!;
                return false;
        }
    }
}
