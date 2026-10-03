using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Media.Application.Composition;

/// <summary>
/// Maps Media Infrastructure <see cref="PlatformHttpException"/> faults into Result by ErrorCode.
/// Unknown exceptions propagate.
/// </summary>
public static class MediaOperation
{
    /// <summary>Runs an action and maps platform Media faults to <see cref="Result{T}"/>.</summary>
    public static async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return Result.Success(await action());
        }
        catch (PlatformHttpException ex) when (!string.IsNullOrWhiteSpace(ex.ErrorCode))
        {
            return Result.Failure<T>(new SemanticError(ex.ErrorCode!));
        }
    }

    /// <summary>Runs an action and maps platform Media faults to <see cref="Result"/>.</summary>
    public static async Task<Result> ExecuteAsync(Func<Task> action)
    {
        try
        {
            await action();
            return Result.Success();
        }
        catch (PlatformHttpException ex) when (!string.IsNullOrWhiteSpace(ex.ErrorCode))
        {
            return Result.Failure(new SemanticError(ex.ErrorCode!));
        }
    }
}
