using Tooba.AccessControl.Application.Validation;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.AccessControl.Application.Composition;

/// <summary>
/// Maps <see cref="AccessControlException"/> into <see cref="Result"/> by stable Code.
/// Unknown exceptions propagate.
/// </summary>
public static class AccessControlOperation
{
    /// <summary>اجرای عمل و نگاشت شکست معنایی به <see cref="Result{T}"/>.</summary>
    /// <typeparam name="T">نوع مقدار موفقیت.</typeparam>
    /// <param name="action">عمل ناهمگام.</param>
    /// <returns>نتیجهٔ موفقیت یا شکست معنایی.</returns>
    public static async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return Result.Success(await action());
        }
        catch (AccessControlException ex)
        {
            return Result.Failure<T>(new SemanticError(ex.Code));
        }
    }

    /// <summary>اجرای عمل بدون مقدار و نگاشت شکست معنایی به <see cref="Result"/>.</summary>
    /// <param name="action">عمل ناهمگام.</param>
    /// <returns>نتیجهٔ موفقیت یا شکست معنایی.</returns>
    public static async Task<Result> ExecuteAsync(Func<Task> action)
    {
        try
        {
            await action();
            return Result.Success();
        }
        catch (AccessControlException ex)
        {
            return Result.Failure(new SemanticError(ex.Code));
        }
    }

    /// <summary>در صورت null بودن مقدار، شکست با کد پایدار missing برمی‌گرداند.</summary>
    /// <typeparam name="T">نوع مرجع.</typeparam>
    /// <param name="value">مقدار احتمالی.</param>
    /// <param name="missingCode">کد پایدار نبودن.</param>
    /// <returns>نتیجهٔ موفقیت یا شکست not-found.</returns>
    public static Result<T> NotFoundIfNull<T>(T? value, string missingCode) where T : class =>
        value is null
            ? Result.Failure<T>(new SemanticError(missingCode))
            : Result.Success(value);
}
