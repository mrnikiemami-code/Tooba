using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Cart.Application.Errors;

namespace Tooba.Cart.Application.Errors;

/// <summary>Maps known Cart directory/domain exception messages to stable SemanticError codes.</summary>
public static class CartExceptionMapper
{
    /// <summary>Converts a known Cart <see cref="InvalidOperationException"/> into a SemanticError.</summary>
    public static SemanticError ToSemanticError(InvalidOperationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var text = exception.Message ?? string.Empty;

        if (text.Contains("checkout.authentication_required", StringComparison.Ordinal)
            || text.Equals(CartErrorCodes.AuthenticationRequired, StringComparison.Ordinal))
        {
            return new SemanticError(CartErrorCodes.AuthenticationRequired);
        }

        if (text.Contains("cart.missing", StringComparison.Ordinal)
            || text.Contains("پیدا نشد", StringComparison.Ordinal))
        {
            return new SemanticError(CartErrorCodes.Missing);
        }

        if (text.Contains("cart.guest_secret.invalid", StringComparison.Ordinal)
            || text.Contains("cart.access.denied", StringComparison.Ordinal)
            || text.Contains("راز", StringComparison.Ordinal)
            || text.Contains("مجوز", StringComparison.Ordinal))
        {
            return new SemanticError(CartErrorCodes.GuestInvalid);
        }

        if (text.Contains("cart.version.stale", StringComparison.Ordinal)
            || text.Contains("cart.version.conflict", StringComparison.Ordinal)
            || text.Contains("کهنه", StringComparison.Ordinal)
            || text.Contains("همزمان", StringComparison.Ordinal))
        {
            return new SemanticError(CartErrorCodes.VersionConflict);
        }

        if (text.Contains("cart.expired", StringComparison.Ordinal)
            || text.Contains("منقضی", StringComparison.Ordinal))
        {
            return new SemanticError(CartErrorCodes.Expired);
        }

        if (text.Contains("Held", StringComparison.Ordinal)
            || text.Contains("رزرو", StringComparison.Ordinal)
            || text.Contains("آزادسازی", StringComparison.Ordinal)
            || text.Contains("cart.inventory.stale", StringComparison.Ordinal))
        {
            return new SemanticError(CartErrorCodes.InventoryStale);
        }

        if (text.Contains("موجودی", StringComparison.Ordinal)
            || text.Contains("cart.inventory.insufficient", StringComparison.Ordinal)
            || text.Contains("cart.inventory.missing", StringComparison.Ordinal))
        {
            return new SemanticError(CartErrorCodes.InventoryInsufficient);
        }

        if (text.Contains("تعداد", StringComparison.Ordinal)
            || text.Contains("cart.line.quantity", StringComparison.Ordinal)
            || text.Contains("quantity_policy", StringComparison.Ordinal)
            || text.Contains("min_quantity", StringComparison.Ordinal)
            || text.Contains("max_quantity", StringComparison.Ordinal))
        {
            return new SemanticError(CartErrorCodes.QuantityInvalid);
        }

        if (text.Contains("cart.line.missing", StringComparison.Ordinal))
        {
            return new SemanticError(CartErrorCodes.LineMissing);
        }

        if (text.Contains("Offer", StringComparison.Ordinal)
            || text.Contains("غیرفعال", StringComparison.Ordinal)
            || text.Contains("cart.offer.", StringComparison.Ordinal))
        {
            return new SemanticError(CartErrorCodes.OfferUnavailable);
        }

        if (text.Contains("فقط سبد Active", StringComparison.Ordinal)
            || text.Contains("قابل جهش خط", StringComparison.Ordinal)
            || text.Contains("cart.line.requires_active", StringComparison.Ordinal)
            || text.Contains("cart.converted", StringComparison.Ordinal))
        {
            return new SemanticError(CartErrorCodes.Rejected);
        }

        return new SemanticError(CartErrorCodes.Rejected);
    }

    /// <summary>Runs a Cart directory action and maps known failures to Result.</summary>
    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return Result.Success(await action());
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<T>(ToSemanticError(ex));
        }
    }
}
