using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.ProductQnA.Application.Storefront.Queries;

namespace Tooba.ProductQnA.Endpoints.Storefront;

/// <summary>مرز HTTP عمومی ProductQnA.</summary>
public static class ProductQnAStorefrontEndpoints
{
    /// <summary>مسیر عمومی پرسش‌های Published را ثبت می‌کند.</summary>
    public static void Map(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapGet("/v1/storefront/products/{slug}/questions", GetPublishedAsync);
    }

    private static async Task<IResult> GetPublishedAsync(
        string slug,
        ISender sender,
        ApiResponseFactory api,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await sender.Send(new GetPublishedQuestionsQuery(slug, page, pageSize), cancellationToken);
            if (result is null)
                return Results.NotFound();

            return Results.Json(new PublicQuestionsResponse(
                result.Items.Select(x => new PublicQuestionItem(
                    x.QuestionId, x.AuthorDisplayName, x.Body, x.CreatedAt,
                    x.AnswerBody, x.AnswerAuthorDisplayName, x.AnswerCreatedAt)).ToList(),
                result.Page,
                result.PageSize,
                result.TotalCount));
        }
        catch (SemanticException ex)
        {
            return api.FromSemanticException(ex);
        }
        catch (PlatformHttpException ex)
        {
            return api.FromPlatformException(ex);
        }
    }
}

/// <summary>پاسخ عمومی صفحهٔ پرسش‌های Published (نام فیلد questions حفظ می‌شود).</summary>
public sealed record PublicQuestionsResponse(
    IReadOnlyList<PublicQuestionItem> Questions,
    int Page,
    int PageSize,
    long TotalCount);

/// <summary>ردیف عمومی پرسش با پاسخ Published اختیاری.</summary>
public sealed record PublicQuestionItem(
    Guid QuestionId,
    string AuthorDisplayName,
    string Body,
    DateTimeOffset CreatedAt,
    string? AnswerBody,
    string? AnswerAuthorDisplayName,
    DateTimeOffset? AnswerCreatedAt);
