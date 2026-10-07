using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.ProductQnA.Contracts.Errors;

namespace Tooba.ProductQnA.Application.Composition;

/// <summary>
/// Maps the module's typed faults into <see cref="Result"/> failures by their declared stable code.
/// <see cref="ContractOperationException"/> is the Domain/Infrastructure expected-failure fault and is
/// mapped only when its code is a declared ProductQnA code; an uncatalogued contract code and every
/// unknown exception propagate untouched to the canonical global exception boundary.
/// <see cref="SemanticException"/> stays mapped for the Application/Infrastructure guard sites.
/// Classification is by typed code only — never by message/prose heuristics.
/// </summary>
public static class ProductQnAOperation
{
    /// <summary>Executes an operation and maps typed faults to <see cref="Result{T}"/>.</summary>
    public static async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            return Result.Success(await action());
        }
        catch (ContractOperationException ex) when (ProductQnAErrorCodes.IsKnown(ex.Code))
        {
            return Result.Failure<T>(new SemanticError(ex.Code));
        }
        catch (SemanticException ex)
        {
            return Result.Failure<T>(ex.Error);
        }
    }
}
