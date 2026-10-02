using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Story.Application.Stories.Composition;

/// <summary>
/// Maps typed <see cref="SemanticException"/> into <see cref="Result"/> failures by stable Code.
/// Unknown exceptions propagate to the global exception boundary.
/// </summary>
public static class StoryOperation
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

    /// <summary>Converts a missing entity into a typed Result failure.</summary>
    public static Result<T> NotFoundIfNull<T>(T? value, string missingCode) where T : class =>
        value is null
            ? Result.Failure<T>(new SemanticError(missingCode))
            : Result.Success(value);
}
