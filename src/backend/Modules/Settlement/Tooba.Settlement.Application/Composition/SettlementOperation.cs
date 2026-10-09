using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Settlement.Contracts.Errors;

namespace Tooba.Settlement.Application.Composition;

/// <summary>
/// Maps the module's typed faults into <see cref="Result"/> failures by stable code.
/// Settlement uses two typed, code-carrying fault mechanisms and both are mapped here by their declared
/// stable code: <see cref="ContractOperationException"/> (via <see cref="ContractOperationException.Code"/>,
/// raised by the Domain aggregates and the Infrastructure directory) and <see cref="SemanticException"/>
/// (via <see cref="SemanticError.Code"/>). Classification is by typed code only — never by message/prose
/// heuristics. A contract fault whose code is not a declared Settlement code, and every unknown exception,
/// propagates untouched to the canonical global exception boundary. Mirrors the certified
/// Returns/Payment/Inventory/Media/Party <c>*Operation</c> seam so Settlement can be extracted into an
/// isolated microservice without leaking result mapping into callers.
/// </summary>
public static class SettlementOperation
{
    /// <summary>Runs an operation and maps a typed fault to <see cref="Result{T}"/>.</summary>
    public static async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            return Result.Success(await action());
        }
        catch (ContractOperationException ex) when (SettlementErrorCodes.IsKnown(ex.Code))
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
        catch (ContractOperationException ex) when (SettlementErrorCodes.IsKnown(ex.Code))
        {
            return Result.Failure(new SemanticError(ex.Code));
        }
        catch (SemanticException ex)
        {
            return Result.Failure(ex.Error);
        }
    }

    /// <summary>
    /// Converts a typed Settlement fault into a <see cref="SemanticError"/> by stable code. Unknown codes
    /// are rethrown so they reach the canonical global exception boundary instead of being swallowed as a
    /// business failure.
    /// </summary>
    public static SemanticError ToSemanticError(ContractOperationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        if (SettlementErrorCodes.IsKnown(exception.Code))
        {
            return new SemanticError(exception.Code);
        }

        throw exception;
    }
}
