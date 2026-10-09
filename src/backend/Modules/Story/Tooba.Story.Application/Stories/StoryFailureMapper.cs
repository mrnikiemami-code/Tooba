using Tooba.BuildingBlocks;
using Tooba.Story.Contracts.Errors;
using Tooba.Story.Domain.Enums;

namespace Tooba.Story.Application.Stories;

/// <summary>
/// کمکی‌های Application برای تبدیل شکل ورودی transport؛ هیچ‌گاه بر اساس متن پیام خطا تصمیم نمی‌گیرد.
/// تنها مبنای تشخیص، کد خطای پایدار است تا قرارداد API مستقل از زبان و پیام باقی بماند.
/// </summary>
public static class StoryFailureMapper
{
    /// <summary>
    /// متن وضعیت بازبینی transport را بدون تماس Endpoints با Domain به enum دامنه تبدیل می‌کند.
    /// مقدار خالی معتبر و به معنای «بدون فیلتر» است؛ مقدار غیرقابل‌پارس <c>false</c> برمی‌گرداند.
    /// </summary>
    /// <param name="raw">متن خام وضعیت بازبینی که از query/body آمده است.</param>
    /// <param name="value">مقدار enum متناظر یا <c>null</c> در حالت خالی.</param>
    /// <returns><c>true</c> اگر ورودی قابل‌پارس یا خالی باشد؛ در غیر این صورت <c>false</c>.</returns>
    public static bool TryParseReviewStatus(string? raw, out StoryReviewStatus? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(raw))
            return true;

        if (!Enum.TryParse<StoryReviewStatus>(raw, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
            return false;

        value = parsed;
        return true;
    }

    /// <summary>
    /// مقدار وضعیت بازبینی را الزامی می‌کند و در صورت نامعتبر بودن، خطای معنایی پایدار پرتاب می‌کند.
    /// این خطا در لایهٔ handler به <c>Result</c> نگاشت می‌شود و هرگز به استثنای کنترل‌نشده تبدیل نمی‌شود.
    /// </summary>
    /// <param name="raw">متن خام وضعیت بازبینی.</param>
    /// <returns>مقدار enum یا <c>null</c> وقتی ورودی خالی است.</returns>
    /// <exception cref="SemanticException">وقتی مقدار ورودی به هیچ عضو معتبر enum نگاشت نشود.</exception>
    public static StoryReviewStatus? RequireReviewStatus(string? raw)
    {
        if (!TryParseReviewStatus(raw, out var value))
            throw new SemanticException(new SemanticError(StoryErrorCodes.ReviewStatusInvalid));
        return value;
    }
}
