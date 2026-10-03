using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Party.Application.Composition;

/// <summary>
/// Maps typed <see cref="SemanticException"/> into <see cref="Result"/> failures by stable Code.
/// Unknown exceptions propagate to the global exception boundary.
/// </summary>
public static class PartyOperation
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
}
