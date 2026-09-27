using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Application;

/// <summary>
/// Maps directory PlatformHttpException / exact-code InvalidOperationException into Result failures.
/// Unknown exceptions propagate to the global exception boundary.
/// </summary>
public static class ContentOperation
{
    public static async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action, string unusedFallbackCode = "")
    {
        _ = unusedFallbackCode;
        try
        {
            return Result.Success(await action());
        }
        catch (PlatformHttpException ex) when (!string.IsNullOrWhiteSpace(ex.ErrorCode))
        {
            return Result.Failure<T>(new SemanticError(ex.ErrorCode!));
        }
        catch (InvalidOperationException ex) when (ContentErrorCodes.IsKnownCode(ex.Message))
        {
            return Result.Failure<T>(new SemanticError(ex.Message));
        }
    }

    public static async Task<Result> ExecuteAsync(Func<Task> action, string unusedFallbackCode = "")
    {
        _ = unusedFallbackCode;
        try
        {
            await action();
            return Result.Success();
        }
        catch (PlatformHttpException ex) when (!string.IsNullOrWhiteSpace(ex.ErrorCode))
        {
            return Result.Failure(new SemanticError(ex.ErrorCode!));
        }
        catch (InvalidOperationException ex) when (ContentErrorCodes.IsKnownCode(ex.Message))
        {
            return Result.Failure(new SemanticError(ex.Message));
        }
    }

    public static Result<T> NotFoundIfNull<T>(T? value, string missingCode) where T : class =>
        value is null
            ? Result.Failure<T>(new SemanticError(missingCode))
            : Result.Success(value);
}
