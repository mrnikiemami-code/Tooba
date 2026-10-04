using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Cart.Application.Composition;

/// <summary>
/// Maps the module's typed <see cref="SemanticException"/> faults into <see cref="Result"/> failures by
/// stable Code. Unknown exceptions propagate untouched to the global exception boundary.
/// No message parsing, no code alias table, no heuristics.
/// </summary>
public static class CartOperation
{
    /// <summary>Runs an operation and maps a semantic fault to <see cref="Result{T}"/>.</summary>
    public static async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            return Result.Success(await action());
        }
        catch (SemanticException ex)
        {
            return Result.Failure<T>(ex.Error);
        }
    }

    /// <summary>Runs an operation without a value and maps a semantic fault to <see cref="Result"/>.</summary>
    public static async Task<Result> ExecuteAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            await action();
            return Result.Success();
        }
        catch (SemanticException ex)
        {
            return Result.Failure(ex.Error);
        }
    }
}
