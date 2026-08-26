using Tooba.BuildingBlocks;
using Tooba.BulkInquiry.Application;
using Tooba.ProductQnA.Application;

namespace Tooba.Host.ProductQnA;

/// <summary>مرزهای HTTP عمومی و مشتری برای پرسش محصول و درخواست خرید عمده.</summary>
public static class ProductQnAEndpoints
{
    internal const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";

    /// <summary>مسیرهای ProductQnA و BulkInquiry را ثبت می‌کند.</summary>
    public static void MapProductQnAEndpoints(this WebApplication app)
    {
        app.MapGet("/v1/storefront/products/{slug}/questions", GetPublishedQuestionsAsync);
        app.MapPost("/v1/customer/product-questions", SubmitQuestionAsync);
        app.MapPost("/v1/storefront/products/{slug}/bulk-inquiries", SubmitBulkInquiryAsync);
    }

    private static async Task<IResult> GetPublishedQuestionsAsync(
        string slug,
        IProductQaDirectory qna,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await qna.GetPublishedAsync(slug, page, pageSize, cancellationToken);
        if (result is null) return Results.NotFound();
        return Results.Json(new PublicQuestionsResponse(
            result.Items.Select(x => new PublicQuestionItem(
                x.QuestionId, x.AuthorDisplayName, x.Body, x.CreatedAt,
                x.AnswerBody, x.AnswerAuthorDisplayName, x.AnswerCreatedAt)).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount));
    }

    private static async Task<IResult> SubmitQuestionAsync(
        SubmitProductQuestion body,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        IProductQaDirectory qna,
        CancellationToken cancellationToken)
    {
        var actor = ResolveActor(request, session, environment);
        if (actor is null)
            return Results.Json(new { title = "Unauthorized", errorCode = "customer.session.required" }, statusCode: 401);
        try
        {
            var id = await qna.SubmitQuestionAsync(actor.Value, body, cancellationToken);
            return Results.Json(new { questionId = id, status = "Pending" }, statusCode: 201);
        }
        catch (InvalidOperationException)
        {
            return Results.Json(new { title = "Bad Request", errorCode = "product_qna.rejected" }, statusCode: 400);
        }
    }

    private static async Task<IResult> SubmitBulkInquiryAsync(
        string slug,
        BulkInquiryBody body,
        IBulkInquiryDirectory inquiries,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new SubmitBulkInquiryRequest(
                slug, body.FullName, body.Phone, body.Email, body.CompanyName,
                body.Address, body.Quantity, body.Notes);
            var id = await inquiries.SubmitAsync(request, cancellationToken);
            return Results.Json(new { inquiryId = id, status = "Submitted" }, statusCode: 201);
        }
        catch (InvalidOperationException)
        {
            return Results.Json(new { title = "Bad Request", errorCode = "bulk_inquiry.rejected" }, statusCode: 400);
        }
    }

    private static Guid? ResolveActor(HttpRequest request, CurrentAuthenticatedSession session, IHostEnvironment environment)
    {
        if (session.IsAuthenticated && session.UserId is { } userId) return userId;
        if (environment.IsDevelopment() && request.Headers.TryGetValue(DevActorHeader, out var raw)
            && Guid.TryParse(raw.ToString(), out var actor) && actor != Guid.Empty) return actor;
        return null;
    }
}

/// <summary>بدنهٔ HTTP درخواست خرید عمده؛ slug از مسیر تأمین می‌شود.</summary>
public sealed record BulkInquiryBody(
    string FullName,
    string Phone,
    string? Email,
    string? CompanyName,
    string Address,
    int Quantity,
    string? Notes);

/// <summary>پاسخ عمومی صفحهٔ پرسش‌های Published.</summary>
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
