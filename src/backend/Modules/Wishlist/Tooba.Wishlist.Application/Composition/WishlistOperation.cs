using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Wishlist.Application.Composition;

/// <summary>
/// Maps typed <see cref="SemanticException"/> into <see cref="Result"/> failures by stable Code.
/// Unknown exceptions propagate to the global exception boundary.
/// </summary>
public static class WishlistOperation
{
    /// <summary>Executes an operation and maps semantic faults to <see cref="Result{T}"/>.</summary>
    public static async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return Result.Success(await action());
        }
        catch (SemanticException ex)
        {
            return Result.Failure<T>(ex.Error);
        }
    }

    /// <summary>Executes an operation and maps semantic faults to non-generic <see cref="Result"/>.</summary>
    public static async Task<Result> ExecuteAsync(Func<Task> action)
    {
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
