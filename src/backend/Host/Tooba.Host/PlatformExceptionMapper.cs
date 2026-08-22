using Microsoft.AspNetCore.Mvc;
using Tooba.BuildingBlocks;

namespace Tooba.Host;

internal readonly record struct MappedPlatformError(int StatusCode, string Title, string? ErrorCode);

internal static class PlatformExceptionMapper
{
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
