using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Catalog.Application.StoreMenus;

/// <summary>
/// Transitional bridge: Catalog StoreMenu Directory/Domain still throw PlatformHttpException;
/// Application facades capture stable codes into Result for ApiResponseFactory.
/// </summary>
internal static class StoreMenuPlatformResult
{
    /// <summary>Runs an action and maps PlatformHttpException error codes to Result failure.</summary>
    public static async Task<Result<T>> CaptureAsync<T>(Func<Task<T>> action)
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

    /// <summary>Runs a void action and maps PlatformHttpException error codes to Result failure.</summary>
    public static async Task<Result> CaptureAsync(Func<Task> action)
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
