using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Storefront;

namespace Tooba.Order.Application.Storefront.Services;

/// <summary>Maps typed storefront faults to Result without Host message classifiers.</summary>
public static class StorefrontOrderResult
{
    public static async Task<Result<T>> ExecuteAsync<T>(
        Func<Task<T>> action,
        ICheckoutAbuseGate? abuse = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Result.Success(await action());
        }
        catch (CheckoutAbuseLimitException exception)
        {
            if (abuse is not null)
            {
                await abuse.RecordBlockAsync(exception, cancellationToken);
            }

            return Result.Failure<T>(new SemanticError(
                exception.ErrorCode,
                new Dictionary<string, string?>
                {
                    ["currentCount"] = exception.CurrentCount.ToString(),
                    ["maxCount"] = exception.MaxCount.ToString(),
                    ["nextAvailableAt"] = exception.NextAvailableAt?.ToString("O"),
                }));
        }
        catch (StorefrontOrderException exception)
        {
            return Result.Failure<T>(new SemanticError(exception.Code));
        }
        catch (InvalidOperationException exception) when (TryMapCheckoutDirectoryCode(exception.Message, out var code))
        {
            return Result.Failure<T>(new SemanticError(code));
        }
    }

    public static async Task<Result<object>> ExecuteObjectAsync(
        Func<Task<object>> action,
        CancellationToken cancellationToken = default)
    {
        var result = await ExecuteAsync(action, abuse: null, cancellationToken);
        return result.IsSuccess
            ? Result.Success<object>(result.Value!)
            : Result.Failure<object>(result.Errors);
    }

    /// <summary>
    /// Maps already-typed fault tokens from Order checkout directory (not localized text).
    /// </summary>
    private static bool TryMapCheckoutDirectoryCode(string message, out string code)
    {
        var token = message.Split(':', 2)[0].Trim();
        if (token.StartsWith("inventory.", StringComparison.Ordinal)
            || string.Equals(token, "inventory.supply.unavailable", StringComparison.Ordinal))
        {
            code = StorefrontOrderErrors.CheckoutInventoryUnavailable;
            return true;
        }

        if (string.Equals(token, "PRICE_CHANGED", StringComparison.Ordinal)
            || string.Equals(token, "PROMOTION_CHANGED", StringComparison.Ordinal))
        {
            code = StorefrontOrderErrors.CheckoutPriceChanged;
            return true;
        }

        if (token.StartsWith("TAX_", StringComparison.Ordinal))
        {
            code = StorefrontOrderErrors.CheckoutTaxUnavailable;
            return true;
        }

        if (token.StartsWith("checkout.", StringComparison.Ordinal)
            || token.StartsWith("shipping.", StringComparison.Ordinal)
            || token.StartsWith("order.", StringComparison.Ordinal)
            || token.StartsWith("pending.", StringComparison.Ordinal)
            || token.StartsWith("payment.", StringComparison.Ordinal))
        {
            code = token;
            return true;
        }

        code = string.Empty;
        return false;
    }
}
