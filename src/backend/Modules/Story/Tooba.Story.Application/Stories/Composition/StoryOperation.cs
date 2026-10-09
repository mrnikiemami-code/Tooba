using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Story.Application.Stories.Composition;

/// <summary>
/// استثنای معناییِ نوع‌دار <see cref="SemanticException"/> را بر اساس کد پایدار به شکست <see cref="Result"/> نگاشت می‌کند.
/// استثناهای ناشناخته عمداً گرفته نمی‌شوند و به مرز استثنای سراسری Host می‌رسند تا هرگز به‌عنوان خطای کسب‌وکار پنهان نشوند.
/// </summary>
public static class StoryOperation
{
    /// <summary>
    /// یک عملیات را اجرا و خطاهای معنایی را به <see cref="Result{T}"/> تبدیل می‌کند.
    /// فقط <see cref="SemanticException"/> نگاشت می‌شود؛ سایر استثناها عبور می‌کنند.
    /// </summary>
    /// <typeparam name="T">نوع مقدار موفقیت.</typeparam>
    /// <param name="action">عملیاتی که نتیجهٔ دامنه را برمی‌گرداند.</param>
    /// <returns>نتیجهٔ موفق یا شکست بر پایهٔ کد خطای پایدار.</returns>
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

    /// <summary>
    /// مقدار غایب را به شکست نوع‌دار با کد «یافت‌نشده» تبدیل می‌کند.
    /// کد ارسالی از Contracts می‌آید تا مالکیت کد خطا در جای درست باقی بماند.
    /// </summary>
    /// <typeparam name="T">نوع مقدار مرجع.</typeparam>
    /// <param name="value">مقدار بازگشتی که ممکن است <c>null</c> باشد.</param>
    /// <param name="missingCode">کد خطای پایدار برای حالت یافت‌نشدن.</param>
    /// <returns>نتیجهٔ موفق با مقدار یا شکست با کد ارسالی.</returns>
    public static Result<T> NotFoundIfNull<T>(T? value, string missingCode) where T : class =>
        value is null
            ? Result.Failure<T>(new SemanticError(missingCode))
            : Result.Success(value);
}
