using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Tax.Contracts.Errors;

namespace Tooba.Tax.Application.Composition;

/// <summary>
/// Maps the module's typed faults into <see cref="Result"/> failures by stable code.
/// Tax uses two typed, code-carrying fault mechanisms and both are mapped here by their declared
/// stable code: <see cref="SemanticException"/> (via <see cref="SemanticError.Code"/>, raised by the
/// Domain aggregates) and <see cref="ContractOperationException"/> (via
/// <see cref="ContractOperationException.Code"/>, raised by the Infrastructure directory and the outbox
/// registration). Classification is by typed code only — never by message/prose heuristics. A contract
/// fault whose code is not a declared Tax code, and every unknown exception, propagates untouched to the
/// canonical global exception boundary. Mirrors the certified Inventory/Pricing/Media/Party/Payment
/// <c>*Operation</c> seam so Tax can be extracted into an isolated microservice without leaking result
/// mapping into callers.
/// </summary>
public static class TaxOperation
{
    /// <summary>Runs an operation and maps a typed fault to <see cref="Result{T}"/>.</summary>
    public static async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            return Result.Success(await action());
        }
        catch (ContractOperationException ex) when (TaxErrorCodes.IsKnown(ex.Code))
        {
            return Result.Failure<T>(new SemanticError(ex.Code));
        }
        catch (SemanticException ex)
        {
            return Result.Failure<T>(ex.Error);
        }
    }

    /// <summary>Runs an operation without a value and maps a typed fault to <see cref="Result"/>.</summary>
    public static async Task<Result> ExecuteAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            await action();
            return Result.Success();
        }
        catch (ContractOperationException ex) when (TaxErrorCodes.IsKnown(ex.Code))
        {
            return Result.Failure(new SemanticError(ex.Code));
        }
        catch (SemanticException ex)
        {
            return Result.Failure(ex.Error);
        }
    }
}
