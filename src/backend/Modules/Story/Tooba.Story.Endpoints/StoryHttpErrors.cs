using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks;
using Tooba.Story.Application.Presentation;
using Tooba.Story.Contracts.Errors;

namespace Tooba.Story.Endpoints;

/// <summary>کمک‌های مشترک نگاشت خطای HTTP Story.</summary>
internal static class StoryHttpErrors
{
    public static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);

    public static IResult ToMutationError(InvalidOperationException ex)
    {
        var missing = ex.Message.Contains("یافت نشد", StringComparison.Ordinal);
        var unsafeCta = ex.Message.Contains("ناامن", StringComparison.Ordinal);
        var errorCode = missing
            ? StoryErrorCodes.Missing
            : unsafeCta
                ? StoryErrorCodes.CtaRejected
                : StoryErrorCodes.MutationRejected;
        var statusCode = missing ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;
        return Results.Json(
            new { title = missing ? "Not Found" : "Bad Request", errorCode, detail = ex.Message },
            statusCode: statusCode);
    }

    public static IResult TenantMissing(InvalidOperationException ex) =>
        Results.Json(
            new { title = "Bad Request", errorCode = StoryErrorCodes.TenantMissing, detail = ex.Message },
            statusCode: StatusCodes.Status400BadRequest);

    public static Guid RequireTenantId(ICurrentTenant tenant) =>
        StoryPresentationComposer.RequireTenantId(tenant);
}
