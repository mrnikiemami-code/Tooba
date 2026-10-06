using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.OperatorProfile.Contracts.Errors;

namespace Tooba.OperatorProfile.Application.Composition;

/// <summary>
/// Maps the module's typed faults into <see cref="Result"/> failures by stable code.
/// OperatorProfile uses two typed, code-carrying fault mechanisms and both are mapped here by their
/// declared stable code: <see cref="SemanticException"/> (via <see cref="SemanticError.Code"/>) and
/// <see cref="ContractOperationException"/> (via <see cref="ContractOperationException.Code"/>).
/// Classification is by typed code only — never by message/prose heuristics. A contract fault whose
/// code is not a declared OperatorProfile code, and every unknown exception, propagates untouched to
/// the canonical global exception boundary. Mirrors the certified Inventory/Media <c>*Operation</c>
/// seam so OperatorProfile can be extracted into an isolated microservice without leaking result
/// mapping into callers.
/// </summary>
public static class OperatorProfileOperation
{
    /// <summary>Runs an operation and maps a typed fault to <see cref="Result{T}"/>.</summary>
    public static async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            return Result.Success(await action());
        }
        catch (ContractOperationException ex) when (OperatorProfileErrorCodes.IsKnown(ex.Code))
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
        catch (ContractOperationException ex) when (OperatorProfileErrorCodes.IsKnown(ex.Code))
        {
            return Result.Failure(new SemanticError(ex.Code));
        }
        catch (SemanticException ex)
        {
            return Result.Failure(ex.Error);
        }
    }
}
