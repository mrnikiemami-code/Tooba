using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.CustomerProfile.Application.Composition;

/// <summary>
/// Converts typed <see cref="ContractOperationException"/> faults into <see cref="Result"/> failures by
/// stable Code. Unknown exceptions propagate untouched to the global exception boundary.
/// </summary>
public static class CustomerProfileOperation
{
    /// <summary>Runs an operation and maps a typed contract fault to <see cref="Result{T}"/>.</summary>
    public static async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            return Result.Success(await action());
        }
        catch (ContractOperationException ex)
        {
            return Result.Failure<T>(new SemanticError(ex.Code));
        }
    }

    /// <summary>Runs an operation without a value and maps a typed contract fault to <see cref="Result"/>.</summary>
    public static async Task<Result> ExecuteAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
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

    /// <summary>Returns a stable-code failure when the value is null, otherwise success.</summary>
    public static Result<T> NotFoundIfNull<T>(T? value, string missingCode) where T : class =>
        value is null
            ? Result.Failure<T>(new SemanticError(missingCode))
            : Result.Success(value);
}
