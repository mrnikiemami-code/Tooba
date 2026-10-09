using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Contracts.Errors;

namespace Tooba.Support.Application.Composition;

/// <summary>
/// درز (seam) canonicalِ نگاشت خطای نوع‌دار Support به شکست <see cref="Result"/> بر پایهٔ کد پایدار.
/// <para>
/// Support از دو مکانیزم خطای نوع‌دار و کد‌حمل‌کننده استفاده می‌کند و هر دو در همین‌جا بر پایهٔ کد
/// اعلام‌شده نگاشت می‌شوند: <see cref="ContractOperationException"/> (با
/// <see cref="ContractOperationException.Code"/>؛ پرتاب‌شده از تجمیع دامنه و دایرکتوری زیرساخت) و
/// <see cref="SemanticException"/> (با <see cref="SemanticError.Code"/>).
/// </para>
/// <para>
/// تشخیص فقط بر پایهٔ کد نوع‌دار انجام می‌شود و هرگز بر پایهٔ متن پیام یا heuristic متن نیست.
/// ناوردای دامنه/دایرکتوری یک هویت <c>Result</c>-محور است، نه یک کد HTTP تازه: برای پاسداشت
/// قرارداد پاسخ موجود، هر ناوردای شناخته‌شده روی کد نتیجهٔ عمومی پایدار همان عملیات نگاشته
/// می‌شود، درست همان‌طور که پیش از این موج با نگاشت‌گر متنی رفتار می‌شد. خطای
/// قراردادی‌ای که کدش یک کد اعلام‌شدهٔ Support نیست، و همچنین هر استثنای نامنتظر، دست‌نخورده عبور
/// می‌کند تا به مرز استثنای سراسری canonical برسد و هرگز به‌عنوان خطای کسب‌وکار پنهان نشود. این
/// درز آینهٔ <c>ReturnsOperation</c> / <c>PromotionOperation</c> است تا Support بتواند بدون نشت
/// نگاشت نتیجه به فراخوان‌ها، به میکروسرویس مستقل استخراج شود.
/// </para>
/// </summary>
public static class SupportOperation
{
    /// <summary>
    /// یک عملیات را اجرا و خطای نوع‌دار Support را به <see cref="Result{T}"/> نگاشت می‌کند.
    /// </summary>
    /// <typeparam name="T">نوع مقدار موفقیت.</typeparam>
    /// <param name="action">عملیاتی که نتیجهٔ دامنه را برمی‌گرداند.</param>
    /// <param name="publicOutcomeCode">
    /// کد نتیجهٔ عمومیِ پایدار برای این مورد استفاده؛ ناورداهای شناخته‌شدهٔ Support به این کد نگاشت
    /// می‌شوند تا شکل پاسخ موجود حفظ شود. اگر <c>null</c> باشد، خودِ کد خطای نوع‌دار بازتاب می‌یابد.
    /// </param>
    /// <returns>نتیجهٔ موفق یا شکست بر پایهٔ کد خطای پایدار.</returns>
    public static async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action, string? publicOutcomeCode = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            return Result.Success(await action());
        }
        catch (ContractOperationException ex) when (SupportErrorCodes.IsKnown(ex.Code))
        {
            return Result.Failure<T>(new SemanticError(ResolveOutcomeCode(ex.Code, publicOutcomeCode)));
        }
        catch (SemanticException ex)
        {
            return Result.Failure<T>(ex.Error);
        }
    }

    /// <summary>
    /// یک عملیات بدون مقدار را اجرا و خطای نوع‌دار Support را به <see cref="Result"/> نگاشت می‌کند.
    /// </summary>
    /// <param name="action">عملیاتی که بدون مقدار موفق می‌شود.</param>
    /// <param name="publicOutcomeCode">کد نتیجهٔ عمومیِ پایدار برای این مورد استفاده (اختیاری).</param>
    /// <returns>نتیجهٔ موفق یا شکست بر پایهٔ کد خطای پایدار.</returns>
    public static async Task<Result> ExecuteAsync(Func<Task> action, string? publicOutcomeCode = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            await action();
            return Result.Success();
        }
        catch (ContractOperationException ex) when (SupportErrorCodes.IsKnown(ex.Code))
        {
            return Result.Failure(new SemanticError(ResolveOutcomeCode(ex.Code, publicOutcomeCode)));
        }
        catch (SemanticException ex)
        {
            return Result.Failure(ex.Error);
        }
    }

    /// <summary>
    /// مقدار غایب را به شکست نوع‌دار با کد «یافت‌نشد» تبدیل می‌کند.
    /// کد ارسالی از Contracts می‌آید تا مالکیت هویت کد خطا در جای درست باقی بماند.
    /// </summary>
    /// <typeparam name="T">نوع مقدار مرجع.</typeparam>
    /// <param name="value">مقدار بازگشتی که ممکن است <c>null</c> باشد.</param>
    /// <param name="missingCode">کد خطای پایدار برای حالت یافت‌نشدن.</param>
    /// <returns>نتیجهٔ موفق با مقدار یا شکست با کد ارسالی.</returns>
    public static Result<T> NotFoundIfNull<T>(T? value, string missingCode) where T : class =>
        value is null
            ? Result.Failure<T>(new SemanticError(missingCode))
            : Result.Success(value);

    /// <summary>
    /// خطای نوع‌دار Support را بر پایهٔ کد پایدار به <see cref="SemanticError"/> تبدیل می‌کند.
    /// کد ناشناخته بازتاب می‌یابد (rethrow) تا به مرز استثنای سراسری canonical برسد و هرگز
    /// به‌عنوان خطای کسب‌وکار پنهان نشود.
    /// </summary>
    /// <param name="exception">خطای قراردادی نوع‌دار.</param>
    /// <param name="publicOutcomeCode">کد نتیجهٔ عمومی پایدار برای این مورد استفاده (اختیاری).</param>
    /// <returns>خطای معنایی با کد پایدار.</returns>
    /// <exception cref="ContractOperationException">وقتی کد یک کد اعلام‌شدهٔ Support نباشد.</exception>
    public static SemanticError ToSemanticError(ContractOperationException exception, string? publicOutcomeCode = null)
    {
        ArgumentNullException.ThrowIfNull(exception);
        if (SupportErrorCodes.IsKnown(exception.Code))
        {
            return new SemanticError(ResolveOutcomeCode(exception.Code, publicOutcomeCode));
        }

        throw exception;
    }

    /// <summary>
    /// کد نتیجهٔ عمومی را برمی‌گزیند: اگر کد نوع‌دار خود یک کد قابل‌مشاهده برای کلاینت باشد، همان
    /// بازتاب می‌یابد؛ در غیر این صورت کد نتیجهٔ عمومی پایدار همان عملیات جایگزین می‌شود تا هیچ کد
    /// HTTP تازه‌ای برای ناوردای دامنه اضافه نشود.
    /// </summary>
    /// <param name="typedCode">کد نوع‌دار پرتاب‌شده.</param>
    /// <param name="publicOutcomeCode">کد نتیجهٔ عمومی اعلام‌شدهٔ مورد استفاده.</param>
    /// <returns>کد نهاییِ قابل‌نمایش در <see cref="SemanticError"/>.</returns>
    private static string ResolveOutcomeCode(string typedCode, string? publicOutcomeCode) =>
        SupportErrorCodes.IsHttpReachable(typedCode)
            ? typedCode
            : publicOutcomeCode ?? typedCode;
}
