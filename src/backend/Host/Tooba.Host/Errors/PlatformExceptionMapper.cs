using Microsoft.AspNetCore.Mvc;
using Tooba.BuildingBlocks;

namespace Tooba.Host;

/// <summary>
/// نتیجهٔ نگاشت استثنا به وضعیت HTTP امن برای کلاینت.
/// </summary>
/// <param name="StatusCode">کد HTTP.</param>
/// <param name="Title">عنوان عمومی.</param>
/// <param name="ErrorCode">کد پایدار اختیاری.</param>
internal readonly record struct MappedPlatformError(int StatusCode, string Title, string? ErrorCode);

/// <summary>
/// نگاشت استثنا به ProblemDetails بدون افشای مسیر فایل، SQL، یا connection string.
/// </summary>
internal static class PlatformExceptionMapper
{
    /// <summary>
    /// استثنا را به وضعیت و عنوان کنترل‌شده تبدیل می‌کند. ناشناخته = ۵۰۰ عمومی.
    /// </summary>
    public static MappedPlatformError Map(Exception exception)
    {
        return exception switch
        {
            PlatformHttpException platform => new MappedPlatformError(
                platform.StatusCode,
                platform.Title,
                platform.ErrorCode),
            BadHttpRequestException => new MappedPlatformError(
                StatusCodes.Status400BadRequest,
                "Bad Request",
                null),
            _ => new MappedPlatformError(
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                null),
        };
    }

    /// <summary>
    /// ProblemDetails با traceId می‌سازد. <paramref name="developmentDetail"/> فقط در Development برای ۵۰۰ مجاز است.
    /// </summary>
    public static ProblemDetails ToProblemDetails(MappedPlatformError mapped, string traceId, string? developmentDetail)
    {
        var problem = new ProblemDetails
        {
            Status = mapped.StatusCode,
            Title = mapped.Title,
            Type = "about:blank",
        };
        problem.Extensions["traceId"] = traceId;
        if (!string.IsNullOrWhiteSpace(mapped.ErrorCode))
        {
            problem.Extensions["errorCode"] = mapped.ErrorCode;
        }

        if (developmentDetail is not null)
        {
            problem.Detail = developmentDetail;
        }

        return problem;
    }
}
