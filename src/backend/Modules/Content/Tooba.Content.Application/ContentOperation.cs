using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Content.Application;

/// <summary>
/// Converts typed <see cref="ContractOperationException"/> into <see cref="Result"/> failures by stable Code.
/// Unknown exceptions propagate to the global exception boundary.
/// </summary>
public static class ContentOperation
{
    /// <summary>Executes an operation and maps typed contract faults to <see cref="Result{T}"/>.</summary>
    public static async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action, string? unusedFallbackCode = null)
    {
        _ = unusedFallbackCode;
        try
        {
            return Result.Success(await action());
        }
        catch (ContractOperationException ex)
        {
            return Result.Failure<T>(new SemanticError(ex.Code));
        }
    }

    /// <summary>Executes an operation and maps typed contract faults to <see cref="Result"/>.</summary>
    public static async Task<Result> ExecuteAsync(Func<Task> action, string? unusedFallbackCode = null)
    {
        _ = unusedFallbackCode;
        try
        {
            await action();
            return Result.Success();
        }
        catch (ContractOperationException ex)
        {
            return Result.Failure(new SemanticError(ex.Code));
        }
    }

    public static Result<T> NotFoundIfNull<T>(T? value, string missingCode) where T : class =>
        value is null
            ? Result.Failure<T>(new SemanticError(missingCode))
            : Result.Success(value);
}
