using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Fulfillment.Application.Errors;

/// <summary>
/// Maps the Fulfillment boundary's typed expected failures to <see cref="SemanticError"/>:
/// <see cref="ContractOperationException"/> (stable <c>Code</c>) and the Domain's
/// <see cref="InvalidOperationException"/> carrying a stable <c>nameof(FulfillmentErrorCodes.X)</c> value.
/// Classification is by typed code only — never by prose heuristics — and unknown exceptions rethrow
/// so they reach the canonical global exception boundary as unexpected failures.
/// </summary>
public static class FulfillmentExceptionMapper
{
    /// <summary>Converts a known Fulfillment exception into a SemanticError. Unknowns rethrow.</summary>
    public static SemanticError ToSemanticError(InvalidOperationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        if (FulfillmentErrors.IsKnown(exception.Message))
        {
            return new SemanticError(exception.Message);
        }

        throw exception;
    }

    /// <summary>Runs a Fulfillment directory action and maps only known expected failures to Result.</summary>
    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return Result.Success(await action());
        }
        catch (ContractOperationException ex) when (FulfillmentErrors.IsKnown(ex.Code))
        {
            return Result.Failure<T>(new SemanticError(ex.Code));
        }
        catch (InvalidOperationException ex) when (FulfillmentErrors.IsKnown(ex.Message))
        {
            return Result.Failure<T>(new SemanticError(ex.Message));
        }
    }

    /// <summary>Runs a void Fulfillment action and maps only known expected failures to Result.</summary>
    public static async Task<Result> TryAsync(Func<Task> action)
    {
        try
        {
            await action();
            return Result.Success();
        }
        catch (ContractOperationException ex) when (FulfillmentErrors.IsKnown(ex.Code))
        {
            return Result.Failure(new SemanticError(ex.Code));
        }
        catch (InvalidOperationException ex) when (FulfillmentErrors.IsKnown(ex.Message))
        {
            return Result.Failure(new SemanticError(ex.Message));
        }
    }
}
