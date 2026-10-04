using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.AddressBook.Application.Composition;

/// <summary>
/// Maps the module's typed <see cref="SemanticException"/> faults into <see cref="Result"/> failures by
/// stable Code. Unknown exceptions propagate untouched to the global exception boundary.
/// </summary>
public static class AddressBookOperation
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

    /// <summary>Returns a stable-code failure when the value is null, otherwise success.</summary>
    public static Result<T> NotFoundIfNull<T>(T? value, string missingCode) where T : class =>
        value is null
            ? Result.Failure<T>(new SemanticError(missingCode))
            : Result.Success(value);
}
