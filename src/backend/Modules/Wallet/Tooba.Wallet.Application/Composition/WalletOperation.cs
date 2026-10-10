using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Wallet.Contracts.Errors;

namespace Tooba.Wallet.Application.Composition;

/// <summary>
/// Canonical seam mapping the module's typed faults into <see cref="Result"/> failures by stable code.
/// <para>
/// Wallet uses two typed, code-carrying fault mechanisms and both are mapped here by their declared
/// stable code: <see cref="ContractOperationException"/> (via
/// <see cref="ContractOperationException.Code"/>, raised by the Domain aggregates and the
/// Infrastructure directory) and <see cref="SemanticException"/> (via
/// <see cref="SemanticError.Code"/>).
/// </para>
/// <para>
/// Classification is by typed code only — never by message/prose heuristics. A domain/directory
/// invariant is a <c>Result</c>-oriented identity, not a fresh HTTP code: to preserve the existing
/// response contract, every known invariant is mapped onto the stable public outcome code of that
/// operation, exactly as the retired message mapper behaved. A contract fault whose code is not a
/// declared Wallet code, and every unknown exception, propagates untouched to the canonical global
/// exception boundary and is never hidden as a business error. Mirrors the certified
/// Support/Returns/Promotion <c>*Operation</c> seam so Wallet can be extracted into an isolated
/// microservice without leaking result mapping into callers.
/// </para>
/// </summary>
public static class WalletOperation
{
    /// <summary>Runs an operation and maps a typed Wallet fault to <see cref="Result{T}"/>.</summary>
    /// <typeparam name="T">Success value type.</typeparam>
    /// <param name="action">Operation returning the domain value.</param>
    /// <param name="publicOutcomeCode">
    /// Stable public outcome code for this use case; known Wallet invariants map onto it so the
    /// existing response shape is preserved. When <c>null</c> the typed code itself is surfaced.
    /// </param>
    /// <returns>A success result or a failure keyed by the stable code.</returns>
    public static async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action, string? publicOutcomeCode = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            return Result.Success(await action());
        }
        catch (ContractOperationException ex) when (WalletErrorCodes.IsKnown(ex.Code))
        {
            return Result.Failure<T>(new SemanticError(ResolveOutcomeCode(ex.Code, publicOutcomeCode)));
        }
        catch (SemanticException ex)
        {
            return Result.Failure<T>(ex.Error);
        }
    }

    /// <summary>Runs an operation without a value and maps a typed Wallet fault to <see cref="Result"/>.</summary>
    /// <param name="action">Operation that succeeds without a value.</param>
    /// <param name="publicOutcomeCode">Stable public outcome code for this use case (optional).</param>
    /// <returns>A success result or a failure keyed by the stable code.</returns>
    public static async Task<Result> ExecuteAsync(Func<Task> action, string? publicOutcomeCode = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            await action();
            return Result.Success();
        }
        catch (ContractOperationException ex) when (WalletErrorCodes.IsKnown(ex.Code))
        {
            return Result.Failure(new SemanticError(ResolveOutcomeCode(ex.Code, publicOutcomeCode)));
        }
        catch (SemanticException ex)
        {
            return Result.Failure(ex.Error);
        }
    }

    /// <summary>
    /// Converts a missing reference into a typed failure with the "not found" code.
    /// The code comes from Contracts so error identity ownership stays in the right place.
    /// </summary>
    /// <typeparam name="T">Reference value type.</typeparam>
    /// <param name="value">Value that may be <c>null</c>.</param>
    /// <param name="missingCode">Stable not-found code.</param>
    /// <returns>A success result with the value or a failure with the supplied code.</returns>
    public static Result<T> NotFoundIfNull<T>(T? value, string missingCode) where T : class =>
        value is null
            ? Result.Failure<T>(new SemanticError(missingCode))
            : Result.Success(value);

    /// <summary>
    /// Converts a typed Wallet fault into a <see cref="SemanticError"/> by stable code. An unknown
    /// code is rethrown so it reaches the canonical global exception boundary and is never hidden as
    /// a business error.
    /// </summary>
    /// <param name="exception">Typed contract fault.</param>
    /// <param name="publicOutcomeCode">Stable public outcome code for this use case (optional).</param>
    /// <returns>A semantic error with the stable code.</returns>
    /// <exception cref="ContractOperationException">When the code is not a declared Wallet code.</exception>
    public static SemanticError ToSemanticError(ContractOperationException exception, string? publicOutcomeCode = null)
    {
        ArgumentNullException.ThrowIfNull(exception);
        if (WalletErrorCodes.IsKnown(exception.Code))
        {
            return new SemanticError(ResolveOutcomeCode(exception.Code, publicOutcomeCode));
        }

        throw exception;
    }

    /// <summary>
    /// Chooses the public outcome code: a client-observable typed code is surfaced as-is; otherwise the
    /// stable public outcome code of the operation replaces it so no new HTTP code is added for a
    /// domain invariant.
    /// </summary>
    /// <param name="typedCode">Thrown typed code.</param>
    /// <param name="publicOutcomeCode">Declared public outcome code of the use case.</param>
    /// <returns>The final code carried by the <see cref="SemanticError"/>.</returns>
    private static string ResolveOutcomeCode(string typedCode, string? publicOutcomeCode) =>
        WalletErrorCodes.IsHttpReachable(typedCode)
            ? typedCode
            : publicOutcomeCode ?? typedCode;
}
