using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Wishlist.Contracts.Errors;

namespace Tooba.Wishlist.Application.Composition;

/// <summary>
/// Canonical seam mapping the module's typed faults into <see cref="Result"/> failures by stable code.
/// <para>
/// Wishlist uses two typed, code-carrying fault mechanisms and both are mapped here by their declared
/// stable code: <see cref="ContractOperationException"/> (via
/// <see cref="ContractOperationException.Code"/>, raised by the Infrastructure outbox registration) and
/// <see cref="SemanticException"/> (via <see cref="SemanticError.Code"/>, raised by the Domain aggregate
/// and the Infrastructure directory).
/// </para>
/// <para>
/// Classification is by typed code only — never by message/prose heuristics. A contract fault whose code
/// is not a declared Wishlist code, and every unknown exception, propagates untouched to the canonical
/// global exception boundary and is never hidden as a business error. Mirrors the certified
/// Support/Returns/Promotion/UserPreference <c>*Operation</c> seam so Wishlist can be extracted into an
/// isolated microservice without leaking result mapping into callers.
/// </para>
/// </summary>
public static class WishlistOperation
{
    /// <summary>Executes an operation and maps a typed Wishlist fault to <see cref="Result{T}"/>.</summary>
    /// <typeparam name="T">Success value type.</typeparam>
    /// <param name="action">Operation returning the domain value.</param>
    /// <returns>A success result or a failure keyed by the stable code.</returns>
    public static async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            return Result.Success(await action());
        }
        catch (ContractOperationException ex) when (WishlistErrorCodes.IsKnown(ex.Code))
        {
            return Result.Failure<T>(new SemanticError(ex.Code));
        }
        catch (SemanticException ex)
        {
            return Result.Failure<T>(ex.Error);
        }
    }

    /// <summary>Executes an operation and maps a typed Wishlist fault to non-generic <see cref="Result"/>.</summary>
    /// <param name="action">Operation that succeeds without a value.</param>
    /// <returns>A success result or a failure keyed by the stable code.</returns>
    public static async Task<Result> ExecuteAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            await action();
            return Result.Success();
        }
        catch (ContractOperationException ex) when (WishlistErrorCodes.IsKnown(ex.Code))
        {
            return Result.Failure(new SemanticError(ex.Code));
        }
        catch (SemanticException ex)
        {
            return Result.Failure(ex.Error);
        }
    }
}
