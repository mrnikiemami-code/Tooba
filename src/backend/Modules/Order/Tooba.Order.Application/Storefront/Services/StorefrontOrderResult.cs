using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Storefront;

using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;

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
        catch (SemanticException exception)
        {
            return Result.Failure<T>(exception.Error);
        }
        catch (ContractOperationException exception) when (TryMapCheckoutCode(exception.Code, out var code))
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
    /// Maps already-typed fault codes from Order checkout/domain producers (not localized text).
    /// </summary>
    private static bool TryMapCheckoutCode(string code, out string mapped)
    {
        if (code.StartsWith("inventory.", StringComparison.Ordinal))
        {
            mapped = StorefrontOrderErrors.CheckoutInventoryUnavailable;
            return true;
        }

        if (string.Equals(code, "PRICE_CHANGED", StringComparison.Ordinal)
            || string.Equals(code, "PROMOTION_CHANGED", StringComparison.Ordinal))
        {
            mapped = StorefrontOrderErrors.CheckoutPriceChanged;
            return true;
        }

        if (code.StartsWith("TAX_", StringComparison.Ordinal))
        {
            mapped = StorefrontOrderErrors.CheckoutTaxUnavailable;
            return true;
        }

        if (code.StartsWith("checkout.", StringComparison.Ordinal)
            || code.StartsWith("shipping.", StringComparison.Ordinal)
            || code.StartsWith("order.", StringComparison.Ordinal)
            || code.StartsWith("pending.", StringComparison.Ordinal)
            || code.StartsWith("payment.", StringComparison.Ordinal))
        {
            mapped = code;
            return true;
        }

        mapped = string.Empty;
        return false;
    }
}
